using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using System.Text.Json;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IPerformanceService
    {
        Task RebuildHistoryAsync(Guid portfolioId);
        Task TakeDailySnapshotAsync(Guid portfolioId, DateTime date);
    }

    public class PerformanceService : IPerformanceService
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<PerformanceService> _logger;

        public PerformanceService(FinPlannerDbContext context, ILogger<PerformanceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RebuildHistoryAsync(Guid portfolioId)
        {
            // 1. Get all transactions for portfolio
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Include(t => t.Account)
                .Where(t => t.Account.PortfolioId == portfolioId)
                .OrderBy(t => t.EffectiveDate)
                .ToListAsync();

            if (!transactions.Any()) return;

            var startDate = transactions.First().EffectiveDate.Date;
            var endDate = DateTime.UtcNow.Date;

            // 2. Clear existing snapshots
            var existing = await _context.PerformanceSnapshots
                .Where(p => p.PortfolioId == portfolioId)
                .ToListAsync();
            _context.PerformanceSnapshots.RemoveRange(existing);

            // 3. Data Prefetching (Optimization)
            var distinctAssetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            // Fetch all historical prices in one query
            var historicalPrices = await _context.HistoricalPrices
                .AsNoTracking()
                .Where(hp => distinctAssetIds.Contains(hp.AssetId) && hp.Date >= startDate)
                .ToListAsync();

            var priceHistoryLookup = historicalPrices
                .GroupBy(hp => hp.AssetId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(hp => hp.Date).ToList()
                );

            // Fetch all current prices for fallback
            var currentPrices = await _context.Assets
                .AsNoTracking()
                .Where(a => distinctAssetIds.Contains(a.Id))
                .Include(a => a.CurrentPrice)
                .Select(a => new { a.Id, Price = a.CurrentPrice != null ? a.CurrentPrice.Price : 0 })
                .ToDictionaryAsync(a => a.Id, a => a.Price);

            // Cursors to track position in history list for each asset: AssetId -> Index
            var priceCursors = distinctAssetIds.ToDictionary(id => id, id => 0);

            // 4. Replay history day by day
            var portfolioHoldings = new Dictionary<Guid, decimal>(); // AssetId -> Units
            var transactionQueue = new Queue<Transaction>(transactions);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Process transactions for this day
                while (transactionQueue.Count > 0 && transactionQueue.Peek().EffectiveDate.Date <= date)
                {
                    var tx = transactionQueue.Dequeue();
                    if (!portfolioHoldings.ContainsKey(tx.AssetId)) portfolioHoldings[tx.AssetId] = 0;

                    if (tx.Type == TransactionType.Buy || tx.Type == TransactionType.Deposit || tx.Type == TransactionType.Dividend)
                    {
                        portfolioHoldings[tx.AssetId] += tx.Units;
                    }
                    else if (tx.Type == TransactionType.Sell || tx.Type == TransactionType.Withdrawal)
                    {
                        portfolioHoldings[tx.AssetId] -= tx.Units;
                    }
                }

                decimal totalValue = 0;

                foreach (var kvp in portfolioHoldings.Where(x => x.Value > 0))
                {
                    var assetId = kvp.Key;
                    var units = kvp.Value;
                    decimal price = 0;

                    // Fast Lookup Logic
                    if (priceHistoryLookup.TryGetValue(assetId, out var history) && history.Any())
                    {
                        // Advance cursor to the latest price <= date
                        int idx = priceCursors[assetId];
                        while (idx < history.Count - 1 && history[idx + 1].Date <= date)
                        {
                            idx++;
                        }
                        priceCursors[assetId] = idx;

                        // Check if current cursor is valid for this date
                        if (history[idx].Date <= date)
                        {
                            price = history[idx].ClosePrice;
                        }
                    }

                    // Fallback to current price if recent and no history price found (or 0)
                    if (price == 0 && date >= DateTime.UtcNow.AddDays(-7))
                    {
                        if (currentPrices.TryGetValue(assetId, out var currentPrice))
                        {
                            price = currentPrice;
                        }
                    }

                    totalValue += units * price;
                }

                if (totalValue > 0)
                {
                    _context.PerformanceSnapshots.Add(new PerformanceSnapshot
                    {
                        Id = Guid.NewGuid(),
                        PortfolioId = portfolioId,
                        Date = date,
                        TotalValue = totalValue,
                        AllocationBreakdown = JsonSerializer.Serialize(portfolioHoldings)
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task TakeDailySnapshotAsync(Guid portfolioId, DateTime date)
        {
            // Reuse logic? For now, this is just for the daily trigger
            // In Phase 1 MVP, we can rely on RebuildHistory being fast enough for small portfolios.
            await RebuildHistoryAsync(portfolioId);
        }
    }
}
