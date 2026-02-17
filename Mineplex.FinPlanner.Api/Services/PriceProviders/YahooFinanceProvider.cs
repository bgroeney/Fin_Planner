using Mineplex.FinPlanner.Api.Models;
using YahooFinanceApi;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    /// <summary>
    /// Yahoo Finance price provider (free, no API key required)
    /// </summary>
    public class YahooFinanceProvider : IPriceSourceProvider
    {
        private readonly IAIService _aiService;
        private readonly ILogger<YahooFinanceProvider> _logger;

        public string ProviderCode => "YAHOO";

        public YahooFinanceProvider(IAIService aiService, ILogger<YahooFinanceProvider> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            var result = new PriceResult();
            var symbol = GetYahooSymbol(asset);
            try
            {
                var securities = await Yahoo.Symbols(symbol).Fields(Field.RegularMarketPrice).QueryAsync();

                if (securities.TryGetValue(symbol, out var security) && security?.RegularMarketPrice > 0)
                {
                    result.Price = (decimal)security.RegularMarketPrice;
                    return result;
                }

                _logger.LogInformation("No price data for {Symbol}. Trying AI lookup...", symbol);

                // AI Fallback
                var aiSymbol = await _aiService.LookupSymbolAsync(asset.Name, asset.Market);
                if (!string.IsNullOrEmpty(aiSymbol) && aiSymbol != symbol)
                {
                    var aiSecurities = await Yahoo.Symbols(aiSymbol).Fields(Field.RegularMarketPrice).QueryAsync();
                    if (aiSecurities.TryGetValue(aiSymbol, out var aiSecurity) && aiSecurity?.RegularMarketPrice > 0)
                    {
                        result.Price = (decimal)aiSecurity.RegularMarketPrice;
                        result.SuggestedSymbol = aiSymbol;
                        _logger.LogInformation("AI successfully found symbol {NewSymbol} for {AssetName}", aiSymbol, asset.Name);
                        return result;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Yahoo Finance failed for {Symbol}", symbol);
                return result;
            }
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var results = new Dictionary<Guid, PriceResult>();
            var assetList = assets.DistinctBy(a => a.Id).ToList();

            if (!assetList.Any()) return results;

            var symbolMap = assetList.ToDictionary(a => GetYahooSymbol(a), a => a);
            var querySymbols = symbolMap.Keys.ToArray();

            try
            {
                var securities = await Yahoo.Symbols(querySymbols).Fields(Field.RegularMarketPrice).QueryAsync();

                foreach (var symbol in querySymbols)
                {
                    var asset = symbolMap[symbol];
                    if (securities.TryGetValue(symbol, out var security) && security?.RegularMarketPrice > 0)
                    {
                        results[asset.Id] = new PriceResult { Price = (decimal)security.RegularMarketPrice };
                    }
                    // Skip AI in batch - let other providers (MorningstarAu) handle failures
                    // AI lookup is too slow for batch operations and causes rate limits
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yahoo Finance batch failed");
            }

            return results;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
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
                _logger.LogWarning(ex, "Yahoo Finance failed to fetch historical data for symbol: {Symbol}", symbol);
            }

            return history;
        }

        public bool SupportsAsset(Asset asset)
        {
            // Yahoo Finance supports most global assets
            // Can be enhanced with specific checks if needed
            return !string.IsNullOrWhiteSpace(asset.Symbol);
        }

        private string GetYahooSymbol(Asset asset)
        {
            // If explicit market is set to ASX and symbol doesn't already have .AX, append it
            if ((asset.Market?.ToUpper() == "ASX" || asset.Market?.ToUpper() == "AU") && !asset.Symbol.EndsWith(".AX"))
            {
                return $"{asset.Symbol}.AX";
            }

            // If no market is specified, prioritize ASX for known dual-listed or common ETFs
            // This prevents VEU/VTS resolving to the US listing (~$80 USD) instead of ASX (~$113 AUD)
            if (string.IsNullOrEmpty(asset.Market))
            {
                var commonAsxEtfs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "VEU", "VTS", "IVV", "IJR", "IJH", "IOO", "IXI", "IXJ", "IZZ"
                };

                if (commonAsxEtfs.Contains(asset.Symbol))
                {
                    return $"{asset.Symbol}.AX";
                }
            }

            // Return symbol as-is for other markets
            return asset.Symbol;
        }
    }
}
