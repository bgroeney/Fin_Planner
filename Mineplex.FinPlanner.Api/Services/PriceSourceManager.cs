using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services.PriceProviders;

namespace Mineplex.FinPlanner.Api.Services
{
    /// <summary>
    /// Coordinates price source selection and fallback logic
    /// </summary>
    public class PriceInfo
    {
        public decimal Price { get; set; }
        public string SourceUsed { get; set; } = string.Empty;
        public string? SuggestedSymbol { get; set; }
    }

    public class PriceSourceManager
    {
        private readonly FinPlannerDbContext _db;
        private readonly IEnumerable<IPriceSourceProvider> _providers;
        private readonly ILogger<PriceSourceManager> _logger;

        public PriceSourceManager(
            FinPlannerDbContext db,
            IEnumerable<IPriceSourceProvider> providers,
            ILogger<PriceSourceManager> logger)
        {
            _db = db;
            _providers = providers;
            _logger = logger;
        }

        /// <summary>
        /// Get current price for an asset, trying sources in priority order
        /// </summary>
        public async Task<PriceInfo?> GetPriceWithFallbackAsync(Asset asset)
        {
            // Check if asset has a preferred override source
            var assetOverride = await _db.AssetPriceSourceOverrides
                .Include(o => o.PriceSource)
                .Where(o => o.AssetId == asset.Id && o.IsPreferred)
                .FirstOrDefaultAsync();

            if (assetOverride != null)
            {
                // If specific source is set, try only that one
                if (assetOverride.PriceSource != null)
                {
                    if (assetOverride.PriceSource.IsEnabled)
                    {
                        var overrideResult = await TryGetPriceFromSourceAsync(asset, assetOverride.PriceSource, assetOverride.CustomSymbol);
                        // Strict override: Return result (price or null) immediately. Do not fall back.
                        return overrideResult;
                    }
                    else
                    {
                        _logger.LogWarning("Asset {Symbol} override source {Source} is disabled. Skipping.", asset.Symbol, assetOverride.PriceSource.Name);
                        return null;
                    }
                }

                // If no specific source (just custom symbol), fall back to list logic
                var sources = await _db.PriceSources
                    .Where(s => s.IsEnabled)
                    .OrderBy(s => s.Priority)
                    .ToListAsync();

                foreach (var source in sources)
                {
                    var result = await TryGetPriceFromSourceAsync(asset, source, assetOverride.CustomSymbol);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            else
            {
                // No override, just use standard priority
                var sources = await _db.PriceSources
                    .Where(s => s.IsEnabled)
                    .OrderBy(s => s.Priority)
                    .ToListAsync();

                foreach (var source in sources)
                {
                    var result = await TryGetPriceFromSourceAsync(asset, source);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            _logger.LogWarning("Failed to get price for asset {AssetId} ({Symbol}) from any source",
                asset.Id, asset.Symbol);
            return null;
        }

        /// <summary>
        /// Get prices for multiple assets in batches, using the best available source for each
        /// </summary>
        public async Task<Dictionary<Guid, PriceInfo>> GetBatchPricesWithFallbackAsync(
            IEnumerable<Asset> assets)
        {
            var results = new Dictionary<Guid, PriceInfo>();
            var assetsList = assets.ToList();

            // Get enabled sources in priority order
            var sources = await _db.PriceSources
                .Where(s => s.IsEnabled)
                .OrderBy(s => s.Priority)
                .ToListAsync();

            // Get asset overrides
            var assetIds = assetsList.Select(a => a.Id).ToList();
            var overrides = await _db.AssetPriceSourceOverrides
                .Include(o => o.PriceSource)
                .Where(o => assetIds.Contains(o.AssetId) && o.IsPreferred && (o.PriceSource == null || o.PriceSource.IsEnabled))
                .ToListAsync();

            // Process assets with overrides
            var assetsWithSymbolOnlyOverride = new List<(Asset asset, string? customSymbol)>();

            foreach (var assetOverride in overrides)
            {
                var asset = assetsList.FirstOrDefault(a => a.Id == assetOverride.AssetId);
                if (asset == null) continue;

                if (assetOverride.PriceSource != null)
                {
                    // Specific source override
                    var result = await TryGetPriceFromSourceAsync(asset, assetOverride.PriceSource, assetOverride.CustomSymbol);
                    if (result != null)
                    {
                        results[asset.Id] = result;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(assetOverride.CustomSymbol))
                {
                    // Symbol-only override - queue for processing with all sources
                    assetsWithSymbolOnlyOverride.Add((asset, assetOverride.CustomSymbol));
                }
            }

            // Process remaining assets (including symbol-only overrides)
            var remainingAssets = assetsList.Where(a => !results.ContainsKey(a.Id)).ToList();

            foreach (var source in sources)
            {
                if (!remainingAssets.Any()) break;

                var provider = GetProviderByCode(source.Code);
                if (provider == null) continue;

                // For assets with symbol-only overrides, we need to create temp asset objects for the provider
                var assetsToFetch = remainingAssets
                    .Where(a => provider.SupportsAsset(a)) // Only send assets this provider can handle
                    .Select(a =>
                    {
                        var symbolOverride = assetsWithSymbolOnlyOverride.FirstOrDefault(o => o.asset.Id == a.Id);
                        if (symbolOverride.customSymbol != null)
                        {
                            return new Asset
                            {
                                Id = a.Id,
                                Symbol = symbolOverride.customSymbol,
                                Name = a.Name,
                                AssetType = a.AssetType,
                                Market = a.Market
                            };
                        }
                        return a;
                    }).ToList();

                if (!assetsToFetch.Any()) continue; // Skip provider if no supported assets

                try
                {
                    var batchResults = await provider.GetBatchPricesAsync(assetsToFetch, source.ApiKey);

                    foreach (var (assetId, priceResult) in batchResults)
                    {
                        if (priceResult.Price.HasValue)
                        {
                            results[assetId] = new PriceInfo
                            {
                                Price = priceResult.Price.Value,
                                SourceUsed = source.Code,
                                SuggestedSymbol = priceResult.SuggestedSymbol
                            };
                        }
                    }

                    // Remove successfully priced assets from remaining list
                    remainingAssets = remainingAssets
                        .Where(a => !results.ContainsKey(a.Id))
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch pricing failed for source {Source}", source.Code);
                }
            }

            return results;
        }

        /// <summary>
        /// Get historical prices for an asset
        /// </summary>
        public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(
            Asset asset, DateTime from, DateTime to)
        {
            // Try preferred override first
            var assetOverride = await _db.AssetPriceSourceOverrides
                .Include(o => o.PriceSource)
                .Where(o => o.AssetId == asset.Id && o.IsPreferred)
                .FirstOrDefaultAsync();

            if (assetOverride?.PriceSource != null && assetOverride.PriceSource.IsEnabled)
            {
                var provider = GetProviderByCode(assetOverride.PriceSource.Code);
                if (provider != null)
                {
                    var result = await provider.GetHistoricalPricesAsync(asset, from, to, assetOverride.PriceSource.ApiKey);
                    if (result.Any()) return result;
                }
            }

            // Try enabled sources in priority order
            var sources = await _db.PriceSources
                .Where(s => s.IsEnabled)
                .OrderBy(s => s.Priority)
                .ToListAsync();

            foreach (var source in sources)
            {
                var provider = GetProviderByCode(source.Code);
                if (provider == null) continue;

                try
                {
                    var result = await provider.GetHistoricalPricesAsync(asset, from, to, source.ApiKey);
                    if (result.Any())
                    {
                        _logger.LogInformation("Retrieved {Count} historical prices for {Symbol} from {Source}",
                            result.Count, asset.Symbol, source.Code);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get historical prices from {Source} for {Symbol}",
                        source.Code, asset.Symbol);
                }
            }

            _logger.LogWarning("No historical prices found for asset{AssetId} ({Symbol})", asset.Id, asset.Symbol);
            return new List<HistoricalPriceDto>();
        }

        private async Task<PriceInfo?> TryGetPriceFromSourceAsync(
            Asset asset, PriceSource source, string? customSymbol = null)
        {
            var provider = GetProviderByCode(source.Code);
            if (provider == null) return null;

            // Create temp asset with custom symbol if provided
            var queryAsset = asset;
            if (!string.IsNullOrWhiteSpace(customSymbol))
            {
                queryAsset = new Asset
                {
                    Id = asset.Id,
                    Symbol = customSymbol,
                    Name = asset.Name,
                    AssetType = asset.AssetType,
                    Market = asset.Market
                };
            }

            try
            {
                var result = await provider.GetCurrentPriceAsync(queryAsset, source.ApiKey);
                if (result.Price.HasValue)
                {
                    _logger.LogDebug("Retrieved price {Price} for {Symbol} from {Source}",
                        result.Price.Value, asset.Symbol, source.Code);

                    return new PriceInfo
                    {
                        Price = result.Price.Value,
                        SourceUsed = source.Code,
                        SuggestedSymbol = result.SuggestedSymbol
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get price from {Source} for {Symbol}",
                    source.Code, asset.Symbol);
            }

            return null;
        }

        private IPriceSourceProvider? GetProviderByCode(string code)
        {
            return _providers.FirstOrDefault(p => p.ProviderCode == code);
        }
    }
}
