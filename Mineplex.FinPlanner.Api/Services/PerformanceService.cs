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

            // 3. Prepare data for optimized loop
            var assetIds = transactions.Select(t => t.AssetId).Distinct().ToList();

            // Fetch historical prices in bulk (read-only)
            var historyPrices = await _context.HistoricalPrices
                .AsNoTracking()
                .Where(hp => assetIds.Contains(hp.AssetId) && hp.Date <= endDate)
                .ToListAsync();

            // Group prices by Date for event-based processing
            var historyByDate = historyPrices
                .GroupBy(hp => hp.Date.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Fetch current prices in bulk for fallback
            var currentPrices = await _context.CurrentPrices
                .AsNoTracking()
                .Where(cp => assetIds.Contains(cp.AssetId))
                .ToDictionaryAsync(cp => cp.AssetId, cp => cp.Price);

            // State variables
            var portfolioHoldings = new Dictionary<Guid, decimal>(); // AssetId -> Units
            var assetPrices = new Dictionary<Guid, decimal>(); // AssetId -> Last Known Price
            var transactionQueue = new Queue<Transaction>(transactions);

            // Pre-seed assetPrices with the latest price before startDate for all assets
            // This handles cases where we hold an asset but the last price update was before startDate
            var initialPrices = historyPrices
                .Where(hp => hp.Date < startDate)
                .GroupBy(hp => hp.AssetId)
                .Select(g => new { AssetId = g.Key, Price = g.OrderByDescending(x => x.Date).First().ClosePrice });

            foreach (var ip in initialPrices)
            {
                assetPrices[ip.AssetId] = ip.Price;
            }

            // 4. Replay history day by day
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                // A. Apply Transactions
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

                // B. Apply Price Updates for this day
                if (historyByDate.TryGetValue(date, out var dailyPrices))
                {
                    foreach (var hp in dailyPrices)
                    {
                        assetPrices[hp.AssetId] = hp.ClosePrice;
                    }
                }

                // C. Calculate Value
                decimal totalValue = 0;

                // We iterate only over assets we currently hold (or have held - effectively keys in dictionary)
                // Filter for positive holdings
                foreach (var kvp in portfolioHoldings.Where(x => x.Value > 0))
                {
                    var assetId = kvp.Key;
                    var units = kvp.Value;
                    decimal price = 0;

                    // Use last known price from history
                    if (assetPrices.TryGetValue(assetId, out var knownPrice))
                    {
                        price = knownPrice;
                    }

                    // Fallback to current price if price is 0 (missing history) AND recent date
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
