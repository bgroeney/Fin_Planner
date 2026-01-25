using Mineplex.FinPlanner.Api.Models;
using System.Text.Json;
using System.Net.Http.Json;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    /// <summary>
    /// Alpha Vantage price provider (requires API key)
    /// </summary>
    public class AlphaVantageProvider : IPriceSourceProvider
    {
        private readonly ILogger<AlphaVantageProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public string ProviderCode => "ALPHAVANTAGE";

        public AlphaVantageProvider(ILogger<AlphaVantageProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            var result = new PriceResult();
            if (string.IsNullOrWhiteSpace(apiKey)) return result;

            var symbol = GetAlphaVantageSymbol(asset);
            var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<JsonElement>(url);

                if (response.TryGetProperty("Global Quote", out var quote) &&
                    quote.TryGetProperty("05. price", out var priceProp) &&
                    decimal.TryParse(priceProp.GetString(), out var price))
                {
                    result.Price = price;
                    return result;
                }

                _logger.LogDebug("Alpha Vantage: No price data for {Symbol}", symbol);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alpha Vantage failed for {Symbol}", symbol);
                return result;
            }
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var results = new Dictionary<Guid, PriceResult>();
            if (string.IsNullOrWhiteSpace(apiKey)) return results;

            // Alpha Vantage doesn't have a true batch endpoint, so we fetch individually
            foreach (var asset in assets)
            {
                var priceResult = await GetCurrentPriceAsync(asset, apiKey);
                if (priceResult.Price.HasValue)
                {
                    results[asset.Id] = priceResult;
                }
            }

            return results;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            var history = new List<HistoricalPriceDto>();
            if (string.IsNullOrWhiteSpace(apiKey)) return history;

            var symbol = GetAlphaVantageSymbol(asset);
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&outputsize=full&apikey={apiKey}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<JsonElement>(url);

                if (response.TryGetProperty("Time Series (Daily)", out var timeSeries))
                {
                    foreach (var day in timeSeries.EnumerateObject())
                    {
                        if (DateTime.TryParse(day.Name, out var date) && date >= from && date <= to)
                        {
                            if (day.Value.TryGetProperty("4. close", out var closeProp) &&
                                decimal.TryParse(closeProp.GetString(), out var price))
                            {
                                history.Add(new HistoricalPriceDto { Date = date, Price = price });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alpha Vantage historical failed for {Symbol}", symbol);
            }

            return history.OrderBy(h => h.Date).ToList();
        }

        public bool SupportsAsset(Asset asset)
        {
            // Broad support, but mostly stocks/ETFs
            return !string.IsNullOrWhiteSpace(asset.Symbol);
        }

        private string GetAlphaVantageSymbol(Asset asset)
        {
            if ((asset.Market?.ToUpper() == "ASX" || asset.Market?.ToUpper() == "AU") && !asset.Symbol.EndsWith(".AX"))
            {
                return $"{asset.Symbol}.AX";
            }
            return asset.Symbol;
        }
    }

    /// <summary>
    /// Polygon.io price provider (requires API key)
    /// </summary>
    public class PolygonProvider : IPriceSourceProvider
    {
        private readonly ILogger<PolygonProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public string ProviderCode => "POLYGON";

        public PolygonProvider(ILogger<PolygonProvider> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            var result = new PriceResult();
            if (string.IsNullOrWhiteSpace(apiKey)) return result;

            // Use previous day's close for Polygon as it's free/reliable
            var symbol = asset.Symbol.ToUpper();
            var url = $"https://api.polygon.io/v2/aggs/ticker/{symbol}/prev?adjusted=true&apiKey={apiKey}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<JsonElement>(url);

                if (response.TryGetProperty("results", out var results) &&
                    results.GetArrayLength() > 0)
                {
                    var polyResult = results[0];
                    if (polyResult.TryGetProperty("c", out var closeProp))
                    {
                        result.Price = closeProp.GetDecimal();
                        return result;
                    }
                }

                _logger.LogDebug("Polygon: No price data for {Symbol}", symbol);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Polygon failed for {Symbol}", symbol);
                return result;
            }
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var results = new Dictionary<Guid, PriceResult>();
            if (string.IsNullOrWhiteSpace(apiKey)) return results;

            foreach (var asset in assets)
            {
                var priceResult = await GetCurrentPriceAsync(asset, apiKey);
                if (priceResult.Price.HasValue)
                {
                    results[asset.Id] = priceResult;
                }
            }

            return results;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            var history = new List<HistoricalPriceDto>();
            if (string.IsNullOrWhiteSpace(apiKey)) return history;

            var symbol = asset.Symbol.ToUpper();
            var fromStr = from.ToString("yyyy-MM-dd");
            var toStr = to.ToString("yyyy-MM-dd");
            var url = $"https://api.polygon.io/v2/aggs/ticker/{symbol}/range/1/day/{fromStr}/{toStr}?adjusted=true&sort=asc&apiKey={apiKey}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetFromJsonAsync<JsonElement>(url);

                if (response.TryGetProperty("results", out var results))
                {
                    foreach (var item in results.EnumerateArray())
                    {
                        if (item.TryGetProperty("t", out var timeProp) &&
                            item.TryGetProperty("c", out var closeProp))
                        {
                            var timestamp = timeProp.GetInt64();
                            var date = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
                            history.Add(new HistoricalPriceDto { Date = date, Price = closeProp.GetDecimal() });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Polygon historical failed for {Symbol}", symbol);
            }

            return history;
        }

        public bool SupportsAsset(Asset asset)
        {
            // Polygon is primarily US stocks, options, forex, crypto
            // For stocks, verify it's a US-like symbol
            return !string.IsNullOrWhiteSpace(asset.Symbol) && !asset.Symbol.Contains(".");
        }
    }

}
