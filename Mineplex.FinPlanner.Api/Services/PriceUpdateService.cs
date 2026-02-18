using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    /// <summary>
    /// Result of a price update operation
    /// </summary>
    public class PriceUpdateResult
    {
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public double DurationSeconds { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Interface for on-demand price updates - used by both background service (local) 
    /// and HTTP endpoints (cloud/serverless)
    /// </summary>
    public interface IPriceUpdateService
    {
        /// <summary>
        /// Update prices for all assets in the system
        /// </summary>
        Task<PriceUpdateResult> UpdateAllPricesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Update prices for assets in a specific portfolio only
        /// </summary>
        Task<PriceUpdateResult> UpdatePortfolioPricesAsync(Guid portfolioId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Stateless price update service that can be called on-demand.
    /// Extracted from PriceUpdateBackgroundService to support serverless deployment.
    /// </summary>
    public class PriceUpdateService : IPriceUpdateService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PriceUpdateService> _logger;

        public PriceUpdateService(
            IServiceProvider serviceProvider,
            ILogger<PriceUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task<PriceUpdateResult> UpdateAllPricesAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinPlannerDbContext>();
            var priceManager = scope.ServiceProvider.GetRequiredService<PriceSourceManager>();

            var result = new PriceUpdateResult();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting price update for all assets");

            try
            {
                var assets = await db.Assets.ToListAsync(cancellationToken);
                var sources = await db.PriceSources.Where(s => s.IsEnabled).OrderBy(s => s.Priority).ToListAsync(cancellationToken);

                if (!assets.Any())
                {
                    _logger.LogInformation("No assets to update");
                    return result;
                }

                // Process in batches to manage memory and avoid massive queries
                int batchSize = 50;
                for (int i = 0; i < assets.Count; i += batchSize)
                {
                    var batchAssets = assets.Skip(i).Take(batchSize).ToList();
                    var batchAssetIds = batchAssets.Select(a => a.Id).ToList();

                    // Optimization: Pre-fetch history existence for this batch (Safe IN clause)
                    var existingHistoryAssetIds = await db.HistoricalPrices
                        .Where(hp => batchAssetIds.Contains(hp.AssetId))
                        .Select(hp => hp.AssetId)
                        .Distinct()
                        .ToListAsync(cancellationToken);
                    var existingHistorySet = new HashSet<Guid>(existingHistoryAssetIds);

                    var priceResults = await priceManager.GetBatchPricesWithFallbackAsync(batchAssets);

                    // Fetch overrides for this batch only
                    var overrides = await db.AssetPriceSourceOverrides
                        .Where(o => o.IsPreferred && batchAssetIds.Contains(o.AssetId))
                        .ToListAsync(cancellationToken);

                    foreach (var asset in batchAssets)
                    {
                        if (priceResults.TryGetValue(asset.Id, out var priceInfo))
                        {
                            await UpdateCurrentPriceAsync(db, asset.Id, priceInfo.Price, priceInfo.SourceUsed);
                            result.UpdatedCount++;

                            await HandleSourceSwitchingAsync(db, asset, priceInfo.SourceUsed, sources, overrides);
                            await HandleSuggestedSymbolAsync(db, asset, priceInfo.SuggestedSymbol, priceInfo.SourceUsed, overrides);
                            await BackfillHistoricalDataIfNeededAsync(db, priceManager, asset, existingHistorySet);
                        }
                        else
                        {
                            result.FailedCount++;
                            result.Errors.Add($"Failed to get price for {asset.Symbol}");
                            _logger.LogWarning("Failed to get price for asset {Symbol} ({AssetId})", asset.Symbol, asset.Id);
                        }
                    }

                    // Save changes per batch
                    await db.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating prices");
                result.Errors.Add(ex.Message);
            }

            result.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("Price update completed. Updated: {Updated}, Failed: {Failed}, Duration: {Duration:N2}s",
                result.UpdatedCount, result.FailedCount, result.DurationSeconds);

            return result;
        }

        public async Task<PriceUpdateResult> UpdatePortfolioPricesAsync(Guid portfolioId, CancellationToken cancellationToken = default)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FinPlannerDbContext>();
            var priceManager = scope.ServiceProvider.GetRequiredService<PriceSourceManager>();

            var result = new PriceUpdateResult();
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("Starting price update for portfolio {PortfolioId}", portfolioId);

            try
            {
                // Get assets that belong to holdings in this portfolio (Holdings belong to Accounts, which belong to Portfolios)
                var assetIds = await db.Holdings
                    .Include(h => h.Account)
                    .Where(h => h.Account.PortfolioId == portfolioId)
                    .Select(h => h.AssetId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var assets = await db.Assets
                    .Where(a => assetIds.Contains(a.Id))
                    .ToListAsync(cancellationToken);

                var sources = await db.PriceSources
                    .Where(s => s.IsEnabled)
                    .OrderBy(s => s.Priority)
                    .ToListAsync(cancellationToken);

                if (!assets.Any())
                {
                    _logger.LogInformation("No assets in portfolio {PortfolioId}", portfolioId);
                    return result;
                }

                var priceResults = await priceManager.GetBatchPricesWithFallbackAsync(assets);
                var overrides = await db.AssetPriceSourceOverrides
                    .Where(o => o.IsPreferred && assetIds.Contains(o.AssetId))
                    .ToListAsync(cancellationToken);

                // Optimization: Pre-fetch history check for portfolio assets
                var existingHistoryAssetIds = await db.HistoricalPrices
                    .Where(hp => assetIds.Contains(hp.AssetId))
                    .Select(hp => hp.AssetId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                var existingHistorySet = new HashSet<Guid>(existingHistoryAssetIds);

                // Get cached prices to skip recently updated assets
                var cachedPrices = await db.CurrentPrices
                    .Where(cp => assetIds.Contains(cp.AssetId))
                    .ToDictionaryAsync(cp => cp.AssetId, cancellationToken);
                var cacheThreshold = DateTime.UtcNow.AddHours(-1);

                foreach (var asset in assets)
                {
                    // Skip if price was updated within the last hour (cache hit)
                    if (cachedPrices.TryGetValue(asset.Id, out var cached) && cached.LastUpdated > cacheThreshold)
                    {
                        result.UpdatedCount++; // Count as success - using cached
                        continue;
                    }

                    if (priceResults.TryGetValue(asset.Id, out var priceInfo))
                    {
                        await UpdateCurrentPriceAsync(db, asset.Id, priceInfo.Price, priceInfo.SourceUsed);
                        result.UpdatedCount++;

                        await HandleSourceSwitchingAsync(db, asset, priceInfo.SourceUsed, sources, overrides);
                        await HandleSuggestedSymbolAsync(db, asset, priceInfo.SuggestedSymbol, priceInfo.SourceUsed, overrides);
                        await BackfillHistoricalDataIfNeededAsync(db, priceManager, asset, existingHistorySet);
                    }
                    else
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to get price for {asset.Symbol}");
                    }
                }

                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating portfolio prices");
                result.Errors.Add(ex.Message);
            }

            result.DurationSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation("Portfolio price update completed. Updated: {Updated}, Failed: {Failed}, Duration: {Duration:N2}s",
                result.UpdatedCount, result.FailedCount, result.DurationSeconds);

            return result;
        }

        // Helper methods extracted from PriceUpdateBackgroundService

        private async Task HandleSourceSwitchingAsync(
            FinPlannerDbContext db,
            Asset asset,
            string sourceUsed,
            List<PriceSource> sources,
            List<AssetPriceSourceOverride> allOverrides)
        {
            var workingSource = sources.FirstOrDefault(s => s.Code == sourceUsed);
            if (workingSource == null) return;

            var currentOverride = allOverrides.FirstOrDefault(o => o.AssetId == asset.Id);

            if (currentOverride != null)
            {
                if (currentOverride.PriceSourceId != workingSource.Id)
                {
                    _logger.LogInformation("Switching preferred source for {Symbol} from {OldSource} to {NewSource}",
                        asset.Symbol, currentOverride.PriceSourceId, workingSource.Code);
                    currentOverride.PriceSourceId = workingSource.Id;
                    currentOverride.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                var topPrioritySource = sources.FirstOrDefault();
                if (topPrioritySource != null && topPrioritySource.Id != workingSource.Id)
                {
                    _logger.LogInformation("Creating preferred source override for {Symbol} to {NewSource}",
                        asset.Symbol, workingSource.Code);

                    db.AssetPriceSourceOverrides.Add(new AssetPriceSourceOverride
                    {
                        Id = Guid.NewGuid(),
                        AssetId = asset.Id,
                        PriceSourceId = workingSource.Id,
                        IsPreferred = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        private async Task HandleSuggestedSymbolAsync(
            FinPlannerDbContext db,
            Asset asset,
            string? suggestedSymbol,
            string sourceUsed,
            List<AssetPriceSourceOverride> allOverrides)
        {
            if (string.IsNullOrEmpty(suggestedSymbol)) return;

            var workingSource = await db.PriceSources.FirstOrDefaultAsync(s => s.Code == sourceUsed);
            if (workingSource == null) return;

            var currentOverride = allOverrides.FirstOrDefault(o => o.AssetId == asset.Id);

            if (currentOverride != null)
            {
                if (currentOverride.CustomSymbol != suggestedSymbol)
                {
                    _logger.LogInformation("Updating symbol for {Symbol} to {New} based on AI suggestion",
                        asset.Symbol, suggestedSymbol);
                    currentOverride.CustomSymbol = suggestedSymbol;
                    currentOverride.SuggestedSymbol = suggestedSymbol;
                    currentOverride.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                _logger.LogInformation("Creating override for {Symbol} with AI suggested symbol {NewSymbol}",
                    asset.Symbol, suggestedSymbol);

                db.AssetPriceSourceOverrides.Add(new AssetPriceSourceOverride
                {
                    Id = Guid.NewGuid(),
                    AssetId = asset.Id,
                    PriceSourceId = workingSource.Id,
                    IsPreferred = true,
                    CustomSymbol = suggestedSymbol,
                    SuggestedSymbol = suggestedSymbol,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task UpdateCurrentPriceAsync(
            FinPlannerDbContext db, Guid assetId, decimal price, string sourceUsed)
        {
            var currentPrice = await db.CurrentPrices
                .FirstOrDefaultAsync(cp => cp.AssetId == assetId);

            if (currentPrice == null)
            {
                currentPrice = new CurrentPrice
                {
                    Id = Guid.NewGuid(),
                    AssetId = assetId,
                    Price = price,
                    SourceUsed = sourceUsed,
                    LastUpdated = DateTime.UtcNow
                };
                db.CurrentPrices.Add(currentPrice);
            }
            else
            {
                currentPrice.Price = price;
                currentPrice.SourceUsed = sourceUsed;
                currentPrice.LastUpdated = DateTime.UtcNow;
            }

            await StoreHistoricalPriceAsync(db, assetId, price);
        }

        private async Task StoreHistoricalPriceAsync(FinPlannerDbContext db, Guid assetId, decimal price)
        {
            var today = DateTime.UtcNow.Date;

            var existingHistorical = await db.HistoricalPrices
                .FirstOrDefaultAsync(hp => hp.AssetId == assetId && hp.Date == today);

            if (existingHistorical == null)
            {
                db.HistoricalPrices.Add(new HistoricalPrice
                {
                    Id = Guid.NewGuid(),
                    AssetId = assetId,
                    Date = today,
                    ClosePrice = price
                });
            }
            else
            {
                existingHistorical.ClosePrice = price;
            }
        }

        private async Task BackfillHistoricalDataIfNeededAsync(
            FinPlannerDbContext db,
            PriceSourceManager priceManager,
            Asset asset,
            HashSet<Guid> existingHistoryAssetIds)
        {
            if (existingHistoryAssetIds.Contains(asset.Id)) return;

            _logger.LogInformation("Backfilling historical data for new asset {Symbol}", asset.Symbol);

            try
            {
                var to = DateTime.UtcNow;
                var from = to.AddYears(-1);

                var historicalPrices = await priceManager.GetHistoricalPricesAsync(asset, from, to);

                if (!historicalPrices.Any())
                {
                    _logger.LogWarning("No historical data available for asset {Symbol}", asset.Symbol);
                    return;
                }

                var minDate = historicalPrices.Min(p => p.Date.Date);
                var maxDate = historicalPrices.Max(p => p.Date.Date);

                // Optimization: Fetch all existing dates in range in one query instead of N+1
                var existingDates = await db.HistoricalPrices
                    .Where(hp => hp.AssetId == asset.Id && hp.Date >= minDate && hp.Date <= maxDate)
                    .Select(hp => hp.Date)
                    .ToListAsync();

                var existingDatesSet = new HashSet<DateTime>(existingDates);

                var newPrices = new List<HistoricalPrice>();
                foreach (var priceData in historicalPrices)
                {
                    if (!existingDatesSet.Contains(priceData.Date.Date))
                    {
                        newPrices.Add(new HistoricalPrice
                        {
                            Id = Guid.NewGuid(),
                            AssetId = asset.Id,
                            Date = priceData.Date.Date,
                            ClosePrice = priceData.Price
                        });
                    }
                }

                if (newPrices.Any())
                {
                    db.HistoricalPrices.AddRange(newPrices);
                    await db.SaveChangesAsync();
                }
                _logger.LogInformation("Backfilled {Count} historical prices for {Symbol}",
                    historicalPrices.Count, asset.Symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling historical data for asset {Symbol}", asset.Symbol);
            }
        }
    }
}
