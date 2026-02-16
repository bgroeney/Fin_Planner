using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    public class SharesightAuProvider : IPriceSourceProvider
    {
        private readonly ISharesightService _sharesightService;
        private readonly ILogger<SharesightAuProvider> _logger;

        // Matches APIR codes like VAN0111AU, SPC5039AU
        private static readonly Regex ApirRegex = new(@"^[A-Z]{3}[0-9]{4}[A-Z]{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string ProviderCode => "SHARESIGHT_AU";

        public SharesightAuProvider(ISharesightService sharesightService, ILogger<SharesightAuProvider> logger)
        {
            _sharesightService = sharesightService;
            _logger = logger;
        }

        public bool SupportsAsset(Asset asset)
        {
            if (string.IsNullOrEmpty(asset.Symbol)) return false;

            // Prioritize managed funds with APIR-like codes
            return ApirRegex.IsMatch(asset.Symbol) && (asset.Market == "AU" || asset.Market == "FundAU" || string.IsNullOrEmpty(asset.Market));
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            try
            {
                // 1. Search for the instrument ID
                var instrumentId = await _sharesightService.SearchInstrumentIdAsync(asset.Symbol, "FundAU");

                if (!instrumentId.HasValue)
                {
                    _logger.LogWarning($"Sharesight provider could not find instrument ID for {asset.Symbol}");
                    return new PriceResult { Price = null };
                }

                // 2. Fetch the price
                var price = await _sharesightService.GetLatestPriceAsync(instrumentId.Value);

                if (price.HasValue)
                {
                    return new PriceResult
                    {
                        Price = price.Value
                    };
                }

                return new PriceResult { Price = null };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting price for {asset.Symbol} from Sharesight");
                return new PriceResult { Price = null };
            }
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var results = new Dictionary<Guid, PriceResult>();
            foreach (var asset in assets)
            {
                if (!SupportsAsset(asset)) continue;

                var result = await GetCurrentPriceAsync(asset, apiKey);
                results[asset.Id] = result;
            }
            return results;
        }

        public Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            // Future implementation: Sharesight API supports history, can be added later
            return Task.FromResult(new List<HistoricalPriceDto>());
        }
    }
}
