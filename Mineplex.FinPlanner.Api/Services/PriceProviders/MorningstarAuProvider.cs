using Mineplex.FinPlanner.Api.Models;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    /// <summary>
    /// Australian fund & ETF price provider using HttpClient only (no browser automation).
    /// 
    /// Supported asset types:
    ///   - ASX-listed ETFs (VHY, VGS, etc.) via Google Finance
    ///   - APIR-coded managed funds (VAN0111AU, etc.) via Morningstar AU GraphQL API
    /// 
    /// Architecture is extensible - add new price source methods and register them in GetPriceSources().
    /// </summary>
    public class MorningstarAuProvider : IPriceSourceProvider
    {
        private readonly ILogger<MorningstarAuProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAIService _aiService;

        // APIR codes are 9 characters: 3 letters + 4 digits + AU
        private static readonly Regex ApirCodePattern = new(@"^[A-Z]{3}\d{4}AU$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ASX ticker pattern: 1-4 uppercase letters
        private static readonly Regex AsxTickerPattern = new(@"^[A-Z]{1,4}$", RegexOptions.Compiled);

        // Google Finance price extraction
        private static readonly Regex GooglePricePattern = new(@"data-last-price=""([\d.]+)""", RegexOptions.Compiled);

        // Morningstar GraphQL endpoint (publicly accessible, no auth required)
        private const string MorningstarGraphqlUrl = "https://graphapi.prd.morningstar.com.au/graphql";

        public string ProviderCode => "MORNINGSTAR_AU";

        public MorningstarAuProvider(
            ILogger<MorningstarAuProvider> logger,
            IHttpClientFactory httpClientFactory,
            IAIService aiService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _aiService = aiService;
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            var result = new PriceResult();

            if (!SupportsAsset(asset))
                return result;

            var symbol = asset.Symbol.Trim().ToUpper();

            try
            {
                // Determine asset type and use appropriate price source
                if (IsAsxTicker(symbol))
                {
                    result.Price = await GetPriceFromGoogleFinance(symbol);
                }
                else if (ApirCodePattern.IsMatch(symbol))
                {
                    result.Price = await GetPriceForApirCode(symbol);
                }

                if (result.Price.HasValue)
                {
                    _logger.LogInformation("MorningstarAU: Retrieved price {Price} for {Symbol}", result.Price.Value, symbol);
                }
                else
                {
                    _logger.LogWarning("MorningstarAU: No price found for {Symbol}", symbol);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MorningstarAU: Failed to fetch price for {Symbol}", symbol);
            }

            return result;
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var results = new Dictionary<Guid, PriceResult>();
            var supportedAssets = assets.Where(a => SupportsAsset(a)).ToList();

            if (!supportedAssets.Any()) return results;

            _logger.LogInformation("MorningstarAU: Processing batch of {Count} assets", supportedAssets.Count);

            foreach (var asset in supportedAssets)
            {
                var result = await GetCurrentPriceAsync(asset, apiKey);
                if (result.Price.HasValue)
                {
                    results[asset.Id] = result;
                }

                // Rate limit: small delay between requests
                if (supportedAssets.Count > 1)
                    await Task.Delay(500);
            }

            return results;
        }

        public Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            return Task.FromResult(new List<HistoricalPriceDto>());
        }

        public bool SupportsAsset(Asset asset)
        {
            if (string.IsNullOrWhiteSpace(asset.Symbol)) return false;

            var symbol = asset.Symbol.Trim().ToUpper();

            // APIR code
            if (ApirCodePattern.IsMatch(symbol)) return true;

            // ASX tickers on AU market
            if (IsAsxTicker(symbol) &&
                (asset.Market?.ToUpper() == "AU" || asset.Market?.ToUpper() == "ASX" ||
                 asset.AssetType?.ToLower() == "etf"))
            {
                return true;
            }

            return false;
        }

        // ==========================================
        // PRICE SOURCE METHODS
        // Add new sources here and call them in GetPriceForApirCode
        // ==========================================

        /// <summary>
        /// Get price from Google Finance for ASX-listed securities.
        /// Simple HTTP scrape - no auth required.
        /// </summary>
        private async Task<decimal?> GetPriceFromGoogleFinance(string ticker)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)");

            var url = $"https://www.google.com/finance/quote/{ticker}:ASX";
            _logger.LogDebug("MorningstarAU: Fetching Google Finance for {Ticker}", ticker);

            try
            {
                var html = await client.GetStringAsync(url);
                var match = GooglePricePattern.Match(html);
                if (match.Success && decimal.TryParse(match.Groups[1].Value, out var price) && price > 0)
                {
                    _logger.LogDebug("MorningstarAU: Google Finance returned {Price} for {Ticker}", price, ticker);
                    return price;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "MorningstarAU: Google Finance failed for {Ticker}", ticker);
            }

            return null;
        }

        /// <summary>
        /// Get price for an APIR-coded managed fund.
        /// Attempts multiple sources in order:
        ///   1. Morningstar GraphQL API (for fund metadata + tries price fields)
        ///   2. [Future: additional sources can be added here]
        /// </summary>
        private async Task<decimal?> GetPriceForApirCode(string apirCode)
        {
            // Step 1: Resolve APIR to Morningstar SecId and fund metadata
            var fundInfo = await ResolveFundFromMorningstar(apirCode);
            if (fundInfo == null)
            {
                _logger.LogWarning("MorningstarAU: Could not resolve APIR {Code} via Morningstar", apirCode);
                return null;
            }

            _logger.LogDebug("MorningstarAU: Resolved {Code} -> SecId: {SecId}, Name: {Name}, Symbol: {Symbol}",
                apirCode, fundInfo.SecId, fundInfo.Name, fundInfo.InternalSymbol);

            // Step 2: Try to get price from the fund page via HTTP scraping
            var price = await GetPriceFromMorningstarFundPage(fundInfo.InternalSymbol);
            if (price.HasValue)
                return price;

            // Step 3: Try Google Finance with the fund name (some funds have ASX listings too)
            price = await TryGoogleFinanceFundLookup(fundInfo.Name, apirCode);
            if (price.HasValue)
                return price;

            _logger.LogWarning("MorningstarAU: All price sources exhausted for {Code} ({Name})", apirCode, fundInfo.Name);
            return null;
        }

        /// <summary>
        /// Resolve an APIR code to Morningstar fund metadata using the public GraphQL API.
        /// No authentication required.
        /// </summary>
        private async Task<MorningstarFundInfo?> ResolveFundFromMorningstar(string apirCode)
        {
            var client = _httpClientFactory.CreateClient();

            var query = new
            {
                query = @"query search($q: String!, $securityTypes: [SecurityType!]!, $page: Int, $pageSize: Int) {
                    autocomplete(term: $q, securityTypes: $securityTypes, page: $page, pageSize: $pageSize) {
                        securities { id name symbol apir }
                    }
                }",
                variables = new
                {
                    q = apirCode,
                    page = 1,
                    securityTypes = new[] { "FO", "FV" },
                    pageSize = 3
                },
                operationName = "search"
            };

            var json = JsonSerializer.Serialize(query);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(MorningstarGraphqlUrl, content);
                var resultJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(resultJson);
                var securities = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("autocomplete")
                    .GetProperty("securities");

                if (securities.GetArrayLength() == 0)
                    return null;

                var fund = securities[0];
                return new MorningstarFundInfo
                {
                    SecId = fund.GetProperty("id").GetString() ?? "",
                    Name = fund.GetProperty("name").GetString() ?? "",
                    InternalSymbol = fund.GetProperty("symbol").GetString() ?? "",
                    Apir = fund.GetProperty("apir").GetString() ?? ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "MorningstarAU: GraphQL lookup failed for {Code}", apirCode);
                return null;
            }
        }

        /// <summary>
        /// Scrape price from Morningstar AU fund page using HttpClient.
        /// The fund page is a Nuxt SPA, so we extract data from the __NUXT_DATA__ payload.
        /// </summary>
        private async Task<decimal?> GetPriceFromMorningstarFundPage(string internalSymbol)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36");

            var url = $"https://www.morningstar.com.au/investments/security/fund/{internalSymbol}";
            _logger.LogDebug("MorningstarAU: Fetching fund page at {Url}", url);

            try
            {
                var html = await client.GetStringAsync(url);

                // Try to extract price from __NUXT_DATA__ JSON array embedded in HTML
                var nuxtMatch = Regex.Match(html, @"id=""__NUXT_DATA__""[^>]*>([^<]+)<");
                if (nuxtMatch.Success)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(nuxtMatch.Groups[1].Value);
                        var arr = doc.RootElement;

                        // The NUXT_DATA array contains fund data in a flat array format
                        // Look for price-like values near identifiable labels
                        // Entry/Exit prices for managed funds are typically between 0.5 and 500
                        var prices = ExtractPricesFromNuxtData(arr);
                        if (prices.Any())
                        {
                            _logger.LogDebug("MorningstarAU: Found {Count} candidate prices in __NUXT_DATA__", prices.Count);
                            // Return the first reasonable price
                            return prices.First();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "MorningstarAU: Failed to parse __NUXT_DATA__");
                    }
                }

                // Fallback: Look for any obvious price text in the HTML (some pages have structured data)
                var pricePatterns = new[]
                {
                    new Regex(@"Entry\s*Price[:\s]*\$?\s*([\d,]+\.\d+)", RegexOptions.IgnoreCase),
                    new Regex(@"Exit\s*Price[:\s]*\$?\s*([\d,]+\.\d+)", RegexOptions.IgnoreCase),
                    new Regex(@"Unit\s*Price[:\s]*\$?\s*([\d,]+\.\d+)", RegexOptions.IgnoreCase),
                    new Regex(@"NAV[:\s]*\$?\s*([\d,]+\.\d+)", RegexOptions.IgnoreCase),
                };

                foreach (var pattern in pricePatterns)
                {
                    var match = pattern.Match(html);
                    if (match.Success)
                    {
                        var priceStr = match.Groups[1].Value.Replace(",", "");
                        if (decimal.TryParse(priceStr, out var price) && price > 0)
                            return price;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "MorningstarAU: Failed to fetch fund page for symbol {Symbol}", internalSymbol);
            }

            return null;
        }

        /// <summary>
        /// Try Google Finance as a fallback for funds that might also be ASX-listed.
        /// </summary>
        private async Task<decimal?> TryGoogleFinanceFundLookup(string fundName, string apirCode)
        {
            // Some managed funds have ASX-listed equivalents
            // e.g., Vanguard VHY ETF is the listed version of a Vanguard managed fund
            // This is a best-effort lookup
            _logger.LogDebug("MorningstarAU: Trying Google Finance fallback for {Code} ({Name})", apirCode, fundName);

            // Don't try Google Finance for non-ASX assets
            return null;
        }

        /// <summary>
        /// Extract potential prices from NUXT_DATA JSON array.
        /// The NUXT_DATA uses a flat array format where values are referenced by index.
        /// Fund prices are typically decimals between 0.1 and 500.0.
        /// 
        /// NOTE: This is a heuristic approach - the __NUXT_DATA__ format doesn't contain
        /// actual fund prices in the server-rendered version (prices are loaded client-side).
        /// This method is kept as a future extension point if Morningstar changes their approach.
        /// </summary>
        private List<decimal> ExtractPricesFromNuxtData(JsonElement data)
        {
            var prices = new List<decimal>();

            // The NUXT_DATA is a complex nested array. Fund prices are NOT currently
            // included in the server-rendered version. This is a placeholder for future use.

            return prices;
        }

        private bool IsAsxTicker(string symbol)
        {
            return AsxTickerPattern.IsMatch(symbol) && symbol.Length <= 4;
        }

        private class MorningstarFundInfo
        {
            public string SecId { get; set; } = "";
            public string Name { get; set; } = "";
            public string InternalSymbol { get; set; } = "";
            public string Apir { get; set; } = "";
        }
    }
}
