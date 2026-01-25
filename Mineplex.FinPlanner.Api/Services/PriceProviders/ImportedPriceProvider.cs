using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    /// <summary>
    /// Price provider that retrieves prices from uploaded valuation files.
    /// This acts as a fallback when external APIs fail.
    /// </summary>
    public class ImportedPriceProvider : IPriceSourceProvider
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<ImportedPriceProvider> _logger;

        public string ProviderCode => "IMPORTED";

        public ImportedPriceProvider(FinPlannerDbContext context, ILogger<ImportedPriceProvider> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            // Find the most recent active imported price for this asset
            var latestImport = await _context.ImportedAssetPrices
                .Include(p => p.FileUpload)
                .Where(p => p.AssetId == asset.Id && p.FileUpload.IsActive)
                .OrderByDescending(p => p.ValuationDate)
                .ThenByDescending(p => p.CreatedAt) // Break ties
                .FirstOrDefaultAsync();

            if (latestImport != null)
            {
                _logger.LogInformation("IMPORTED: Found price {Price} for {Symbol} from file {File} (Date: {Date})",
                    latestImport.UnitPrice, asset.Symbol, latestImport.FileUpload.FileName, latestImport.ValuationDate);

                return new PriceResult
                {
                    Price = latestImport.UnitPrice,
                    // We don't really have a 'SuggestedSymbol' here, but we could return metadata if PriceResult allowed it
                    // For now, raw price is good.
                };
            }

            return new PriceResult(); // Empty result
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var assetIds = assets.Select(a => a.Id).ToList();
            var results = new Dictionary<Guid, PriceResult>();

            // Efficiently fetch latest prices for all requested assets
            // Group by AssetId and pick max date

            // EF Core 6/7/8 distinct-by/group-by translation can be tricky.
            // Let's try a window function approach or 2-step query if needed.
            // Since we need "latest by ValuationDate", grouping by AssetId is best.

            // Strategy: Get all active prices for these assets, bring to memory (if small) or strictly limit?
            // "Latest" per group in EF is hard.
            // Alternative: use subquery or looping. Looping for batch of e.g. 50 assets is okay if indexed.
            // But let's try to grab all relevant records and filter in memory if the dataset isn't huge.
            // Actually, we can fetch all Active ImportedAssetPrices for these assets.

            var prices = await _context.ImportedAssetPrices
                .Include(p => p.FileUpload) // Need to check IsActive
                .Where(p => assetIds.Contains(p.AssetId) && p.FileUpload.IsActive)
                .Select(p => new { p.AssetId, p.UnitPrice, p.ValuationDate, p.CreatedAt })
                .ToListAsync();

            if (!prices.Any()) return results;

            var latestPrices = prices
                .GroupBy(p => p.AssetId)
                .Select(g => g.OrderByDescending(p => p.ValuationDate).ThenByDescending(p => p.CreatedAt).First())
                .ToList();

            foreach (var p in latestPrices)
            {
                results[p.AssetId] = new PriceResult { Price = p.UnitPrice };
            }

            return results;
        }

        public Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            // We could implement this by querying all ImportedAssetPrices in range
            // But the interface expects HistoricalPriceDto.
            // This is a nice-to-have.
            return Task.FromResult(new List<HistoricalPriceDto>());
        }

        public bool SupportsAsset(Asset asset)
        {
            // Supports any asset that has imported data.
            // We don't filter by symbol pattern.
            return true;
        }
    }
}
