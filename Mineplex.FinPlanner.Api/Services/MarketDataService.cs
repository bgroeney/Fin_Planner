using YahooFinanceApi;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IMarketDataService
    {
        Task<decimal> GetPriceAsync(Models.Asset asset);
        Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<Models.Asset> assets);
        Task<List<HistoricalPriceDto>> GetBenchmarkDataAsync(string symbol, DateTime startDate, DateTime endDate);
    }

    public class MarketDataService : IMarketDataService
    {
        private readonly ILogger<MarketDataService> _logger;

        public MarketDataService(ILogger<MarketDataService> logger)
        {
            _logger = logger;
        }

        public async Task<List<HistoricalPriceDto>> GetBenchmarkDataAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            var history = new List<HistoricalPriceDto>();

            // Map common aliases to Yahoo Tickers
            var querySymbol = symbol.ToUpper() switch
            {
                "ASX200" => "^AXJO",
                "S&P500" => "^GSPC",
                "ALLORDS" => "^AORD",
                _ => symbol
            };

            try
            {
                var historicalData = await Yahoo.GetHistoricalAsync(querySymbol, startDate, endDate, Period.Daily);

                foreach (var candle in historicalData)
                {
                    history.Add(new HistoricalPriceDto
                    {
                        Date = candle.DateTime,
                        Price = (decimal)candle.Close
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch benchmark data for symbol: {Symbol}", querySymbol);
            }

            return history;
        }

        public async Task<decimal> GetPriceAsync(Models.Asset asset)
        {
            var symbolToQuery = GetYahooSymbol(asset);
            try
            {
                var securities = await Yahoo.Symbols(symbolToQuery).Fields(Field.RegularMarketPrice).QueryAsync();

                // Handle potential mismatch if Yahoo changes suffix case, though usually key matches query
                if (securities.TryGetValue(symbolToQuery, out var security) && security?.RegularMarketPrice > 0)
                {
                    return (decimal)security.RegularMarketPrice;
                }

                _logger.LogWarning("No price data available for symbol: {Symbol}", symbolToQuery);
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price for symbol: {Symbol}", symbolToQuery);
                return 0;
            }
        }

        public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<Models.Asset> assets)
        {
            var results = new Dictionary<string, decimal>();
            var assetList = assets.DistinctBy(a => a.Symbol).ToList(); // Simplification: assume Symbol unique enough or just process distinct

            if (!assetList.Any()) return results;

            var symbolMap = assetList.ToDictionary(a => GetYahooSymbol(a), a => a.Symbol);
            var querySymbols = symbolMap.Keys.ToArray();

            try
            {
                var securities = await Yahoo.Symbols(querySymbols).Fields(Field.RegularMarketPrice).QueryAsync();

                foreach (var qs in querySymbols)
                {
                    var originalSymbol = symbolMap[qs];
                    if (securities.TryGetValue(qs, out var security) && security?.RegularMarketPrice > 0)
                    {
                        results[originalSymbol] = (decimal)security.RegularMarketPrice;
                    }
                    else
                    {
                        results[originalSymbol] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch prices for batch");
                foreach (var a in assetList) results[a.Symbol] = 0;
            }

            return results;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalPriceDataAsync(Models.Asset asset, DateTime from, DateTime to)
        {
            var history = new List<HistoricalPriceDto>();
            var symbol = GetYahooSymbol(asset);

            try
            {
                var historicalData = await Yahoo.GetHistoricalAsync(symbol, from, to, Period.Daily);

                foreach (var candle in historicalData)
                {
                    history.Add(new HistoricalPriceDto
                    {
                        Date = candle.DateTime,
                        Price = (decimal)candle.Close
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch historical data for symbol: {Symbol}", symbol);
            }

            return history;
        }

        private string GetYahooSymbol(Models.Asset asset)
        {
            // If explicit market is set to ASX and symbol doesn't already have .AX, append it
            if ((asset.Market?.ToUpper() == "ASX" || asset.Market?.ToUpper() == "AU") && !asset.Symbol.EndsWith(".AX"))
            {
                return $"{asset.Symbol}.AX";
            }

            // Heuristic fallback if Market not set but looks like an ASX code (3 chars, letters)
            if (string.IsNullOrEmpty(asset.Market) && asset.Symbol.Length == 3 && asset.Symbol.All(char.IsLetter))
            {
                // We could force .AX here or try both, but let's stick to safe explicit or direct.
                // User asked to 'correct' zero prices. Let's try .AX default for 3-letter codes if raw fails? 
                // Better: Strict adherence to 'Market' column.
                return asset.Symbol;
            }

            return asset.Symbol;
        }
    }

    public class HistoricalPriceDto
    {
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public decimal Value { get; set; } // Calculated value based on units at that time
    }
}
