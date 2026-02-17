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
            // Optimization: Fetch all prices in bulk to avoid N+1 queries.
            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            // Fetch historical prices for relevant assets
            var historicalPrices = await _context.HistoricalPrices
                .Where(hp => assetIds.Contains(hp.AssetId) && hp.Date <= endDate)
                .OrderBy(hp => hp.Date)
                .ToListAsync();

            var historicalPricesLookup = historicalPrices
                .GroupBy(hp => hp.AssetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Fetch current prices for fallback
            var currentPricesFallback = await _context.CurrentPrices
                .Where(cp => assetIds.Contains(cp.AssetId))
                .ToDictionaryAsync(cp => cp.AssetId, cp => cp.Price);

            // Caches for efficient processing
            var portfolioHoldings = new Dictionary<Guid, decimal>(); // AssetId -> Units
            var transactionQueue = new Queue<Transaction>(transactions);
            var runningPrices = new Dictionary<Guid, decimal>(); // AssetId -> Last Known Price
            var priceCursors = assetIds.ToDictionary(id => id, id => 0); // AssetId -> Index in historicalPrices list

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

                // Update running prices based on historical data up to this day
                foreach (var assetId in assetIds)
                {
                    if (historicalPricesLookup.TryGetValue(assetId, out var prices))
                    {
                        int cursor = priceCursors[assetId];
                        // Advance cursor to find the latest price on or before 'date'
                        while (cursor < prices.Count && prices[cursor].Date <= date)
                        {
                            runningPrices[assetId] = prices[cursor].ClosePrice;
                            cursor++;
                        }
                        priceCursors[assetId] = cursor;
                    }
                }

                decimal totalValue = 0;
                var allocation = new Dictionary<string, decimal>();

                foreach (var kvp in portfolioHoldings.Where(x => x.Value > 0))
                {
                    var assetId = kvp.Key;
                    var units = kvp.Value;
                    decimal price = 0;

                    // Use the latest known historical price
                    if (runningPrices.TryGetValue(assetId, out var p))
                    {
                        price = p;
                    }

                    // Fallback to current price if no recent history is available
                    if (price == 0 && date >= DateTime.UtcNow.AddDays(-7))
                    {
                        if (currentPricesFallback.TryGetValue(assetId, out var cp))
                        {
                            price = cp;
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
