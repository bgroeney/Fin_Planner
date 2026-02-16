using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface ISharesightService
    {
        Task<int?> SearchInstrumentIdAsync(string code, string marketCode = "FundAU");
        Task<decimal?> GetLatestPriceAsync(int instrumentId);
    }

    public class SharesightService : ISharesightService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SharesightService> _logger;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        private bool _isAuthenticated;

        // Sharesight uses specific market IDs internally
        private static readonly Dictionary<string, int> MarketIds = new()
        {
            { "FundAU", 30 }, // Australian Managed Funds
            { "ASX", 14 }     // Australian Stock Exchange (approx, need to verify if needed)
        };

        public SharesightService(
            HttpClient httpClient,
            Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory,
            ILogger<SharesightService> logger)
        {
            _httpClient = httpClient;
            _scopeFactory = scopeFactory;
            _logger = logger;

            // User-Agent is critical for scraping
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
        }

        private async Task AuthenticateAsync()
        {
            if (_isAuthenticated) return;

            string username = "";
            string password = "";

            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<Mineplex.FinPlanner.Api.Data.FinPlannerDbContext>();
                    var source = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(db.PriceSources, s => s.Code == "SHARESIGHT_AU");

                    if (source?.ConfigurationJson != null)
                    {
                        try
                        {
                            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(source.ConfigurationJson);
                            if (config != null)
                            {
                                config.TryGetValue("Username", out username);
                                config.TryGetValue("Password", out password);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to parse Sharesight configuration JSON");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve Sharesight credentials from database");
                return;
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("Sharesight credentials not configured in Price Sources. Skipping authentication.");
                return;
            }

            try
            {
                _logger.LogInformation("Authenticating with Sharesight...");

                // 1. Get the login page to extract CSRF token and cookies
                var loginPageResponse = await _httpClient.GetAsync("https://portfolio.sharesight.com/users/sign_in");
                loginPageResponse.EnsureSuccessStatusCode();
                var loginPageContent = await loginPageResponse.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(loginPageContent);
                var csrfToken = doc.DocumentNode.SelectSingleNode("//meta[@name='csrf-token']")?.GetAttributeValue("content", "");

                if (string.IsNullOrEmpty(csrfToken))
                {
                    _logger.LogError("Failed to extract CSRF token from Sharesight login page.");
                    throw new Exception("Sharesight login failed: No CSRF token found");
                }

                // 2. Perform Login POST
                var loginData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("user[email]", username),
                    new KeyValuePair<string, string>("user[password]", password),
                    new KeyValuePair<string, string>("authenticity_token", csrfToken),
                    new KeyValuePair<string, string>("user[remember_me]", "1"),
                    new KeyValuePair<string, string>("commit", "Log in")
                });

                var loginResponse = await _httpClient.PostAsync("https://portfolio.sharesight.com/users/sign_in", loginData);

                // A successful login usually redirects to the dashboard (302 Found)
                // If using a standard HttpClient, it auto-follows redirects, so we check the final URL or content
                if (loginResponse.RequestMessage?.RequestUri?.ToString().Contains("sign_in") == true)
                {
                    _logger.LogError("Sharesight login failed. Still on sign_in page.");
                    throw new Exception("Sharesight login failed: Invalid credentials or captcha trigger.");
                }

                _isAuthenticated = true;
                _logger.LogInformation("Successfully authenticated with Sharesight.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with Sharesight");
                throw;
            }
        }

        public async Task<int?> SearchInstrumentIdAsync(string code, string marketCode = "FundAU")
        {
            await AuthenticateAsync();

            if (!MarketIds.TryGetValue(marketCode, out var marketId))
            {
                _logger.LogWarning($"Unknown market code: {marketCode}. Defaulting to FundAU (30).");
                marketId = 30;
            }

            // Using the /instruments/find.js endpoint found during research
            // Expected response: { "instruments": [ { "id": 576742, "code": "VAN0111AU", ... } ] }
            var url = $"https://portfolio.sharesight.com/instruments/find.js?term={Uri.EscapeDataString(code)}&market_id={marketId}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Sharesight search failed: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadFromJsonAsync<SharesightSearchResponse>();

                foreach (var instrument in json?.Instruments ?? Array.Empty<SharesightInstrument>())
                {
                    // Clean the code from the response (it might have HTML tags like <b>VAN0111AU</b>)
                    var cleanCode = Regex.Replace(instrument.Code, "<.*?>", "");
                    if (string.Equals(cleanCode, code, StringComparison.OrdinalIgnoreCase))
                    {
                        return instrument.Id;
                    }
                }

                _logger.LogInformation($"Instrument {code} not found in Sharesight.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching for instrument {code} on Sharesight");
                return null;
            }
        }

        public async Task<decimal?> GetLatestPriceAsync(int instrumentId)
        {
            await AuthenticateAsync();

            // Using /charts/instrument_price_data.json endpoint found during research
            // We fetch a small range to get the latest price point
            var endDate = DateTime.Now;
            var startDate = endDate.AddDays(-7); // Last 7 days to cover weekends/holidays
            var range = $"{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}";

            // Note: portfolio_id is NOT required as per our testing
            var url = $"https://portfolio.sharesight.com/charts/instrument_price_data.json?id={instrumentId}&range={range}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Sharesight price fetch failed: {response.StatusCode} for ID {instrumentId}");
                    return null;
                }

                var json = await response.Content.ReadFromJsonAsync<SharesightPriceResponse>();

                // The response has a "series" array. We want the last point's "y2" value.
                if (json?.Series != null && json.Series.Length > 0)
                {
                    var lastPoint = json.Series[^1]; // Get the last data point
                    if (lastPoint.Y2.HasValue)
                    {
                        return lastPoint.Y2.Value;
                    }
                }

                _logger.LogWarning($"No price data found for instrument ID {instrumentId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching price for instrument ID {instrumentId}");
                return null;
            }
        }

        // Response Models
        private class SharesightSearchResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("instruments")]
            public SharesightInstrument[] Instruments { get; set; } = Array.Empty<SharesightInstrument>();
        }

        private class SharesightInstrument
        {
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public int Id { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("code")]
            public string Code { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; } = "";
        }

        private class SharesightPriceResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("series")]
            public SharesightPricePoint[] Series { get; set; } = Array.Empty<SharesightPricePoint>();
        }

        private class SharesightPricePoint
        {
            // The API returns points with 'y2' as the closing price
            [System.Text.Json.Serialization.JsonPropertyName("y2")]
            public decimal? Y2 { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("x")]
            public long? X { get; set; } // Timestamp
        }
    }
}
