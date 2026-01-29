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

            // 3. Replay history day by day
            // Optimization: Bulk fetch prices to avoid N+1 queries in the loop
            var relevantAssetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            var allPrices = await _context.HistoricalPrices
                .Where(hp => relevantAssetIds.Contains(hp.AssetId) && hp.Date <= endDate)
                .Select(hp => new { hp.AssetId, hp.Date, hp.ClosePrice })
                .ToListAsync();

            var priceLookup = allPrices
                .GroupBy(p => p.AssetId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(p => p.Date).ToList()
                );

            var currentPrices = await _context.Assets
                .Where(a => relevantAssetIds.Contains(a.Id))
                .Select(a => new { a.Id, Price = a.CurrentPrice != null ? a.CurrentPrice.Price : 0 })
                .ToDictionaryAsync(x => x.Id, x => x.Price);

            // We iterate through every transaction to build the "Units Held" state.
            // Then for each day, we value those units.

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

                // If it's a weekend, strictly speaking markets are closed, but we carry forward Friday's value usually.
                // For simplicity, we just look up the price.

                decimal totalValue = 0;

                // We need prices for this date.
                foreach (var kvp in portfolioHoldings.Where(x => x.Value > 0))
                {
                    var assetId = kvp.Key;
                    var units = kvp.Value;

                    decimal price = 0;

                    // Try get historical price from memory
                    if (priceLookup.TryGetValue(assetId, out var prices))
                    {
                        // Since list is sorted by Date DESC, First matching Date <= date is the correct one
                        var match = prices.FirstOrDefault(p => p.Date <= date);
                        if (match != null) price = match.ClosePrice;
                    }

                    // Fallback to current price if recent
                    if (price == 0 && date >= DateTime.UtcNow.AddDays(-7))
                    {
                        if (currentPrices.TryGetValue(assetId, out var current))
                        {
                            price = current;
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
                        AllocationBreakdown = JsonSerializer.Serialize(portfolioHoldings) // Simplified allocation
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
