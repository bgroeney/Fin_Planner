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

            // 3. Replay history day by day
            // Optimization: Fetch all historical prices in bulk to avoid N+1 queries.
            var allAssetIds = transactions.Select(t => t.AssetId).Distinct().ToList();
            var searchStartDate = startDate.AddDays(-10); // Buffer for last known price

            var historicalPrices = await _context.HistoricalPrices
                .AsNoTracking()
                .Where(hp => allAssetIds.Contains(hp.AssetId) && hp.Date >= searchStartDate && hp.Date <= endDate)
                .Select(hp => new { hp.AssetId, hp.Date, hp.ClosePrice })
                .ToListAsync();

            // Create a lookup: Date -> Dictionary<AssetId, Price>
            var dailyPrices = historicalPrices
                .GroupBy(hp => hp.Date.Date)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(hp => hp.AssetId, hp => hp.ClosePrice)
                );

            // Fetch current prices for fallback
            var currentPrices = await _context.Assets
                .AsNoTracking()
                .Where(a => allAssetIds.Contains(a.Id))
                .Include(a => a.CurrentPrice)
                .ToDictionaryAsync(a => a.Id, a => a.CurrentPrice != null ? a.CurrentPrice.Price : 0m);

            var portfolioHoldings = new Dictionary<Guid, decimal>(); // AssetId -> Units
            var lastKnownPrices = new Dictionary<Guid, decimal>();   // AssetId -> Last Price

            // Prime lastKnownPrices with pre-start data (between searchStartDate and startDate)
            foreach (var hp in historicalPrices.Where(hp => hp.Date < startDate).OrderBy(hp => hp.Date))
            {
                lastKnownPrices[hp.AssetId] = hp.ClosePrice;
            }

            var transactionQueue = new Queue<Transaction>(transactions);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // Update prices for this day
                if (dailyPrices.TryGetValue(date, out var todaysPrices))
                {
                    foreach (var update in todaysPrices)
                    {
                        lastKnownPrices[update.Key] = update.Value;
                    }
                }

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

                // Calculate Value
                decimal totalValue = 0;

                foreach (var kvp in portfolioHoldings.Where(x => x.Value > 0))
                {
                    var assetId = kvp.Key;
                    var units = kvp.Value;
                    decimal price = 0;

                    if (lastKnownPrices.TryGetValue(assetId, out var p))
                    {
                        price = p;
                    }

                    // Fallback to current price if recent and no history price found
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
