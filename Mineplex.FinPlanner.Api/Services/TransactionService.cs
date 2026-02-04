using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface ITransactionService
    {
        Task RecalculateHoldingsAsync(Guid accountId);
    }

    public class TransactionService : ITransactionService
    {
        private readonly FinPlannerDbContext _context;

        public TransactionService(FinPlannerDbContext context)
        {
            _context = context;
        }

        public async Task RecalculateHoldingsAsync(Guid accountId)
        {
            // Optimization: Use AsNoTracking() as these entities are read-only for calculation
            // Removed Include(t => t.FileUpload) as we only filter on it, not use it in results
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId && (t.FileUploadId == null || (t.FileUpload != null && t.FileUpload.IsActive)))
                .OrderBy(t => t.EffectiveDate)
                .ThenBy(t => t.AttachedOrder)
                .ToListAsync();

            // State: AssetId -> (Units, TotalCost)
            var portfolioState = new Dictionary<Guid, HoldingState>();

            foreach (var txn in transactions)
            {
                if (txn.Type == TransactionType.Buy || (txn.Type == TransactionType.Deposit && txn.AssetId != Guid.Empty))
                {
                    if (!portfolioState.ContainsKey(txn.AssetId)) portfolioState[txn.AssetId] = new HoldingState();
                    var state = portfolioState[txn.AssetId];
                    state.Units += txn.Units;

                    // Amount is usually negative for a buy flow, but here we want cost base to increase
                    // Netwealth credits - debits logic: Buy is a debit (negative).
                    // We need a positive cost base.
                    state.TotalCost += Math.Abs(txn.Amount);
                }
                else if (txn.Type == TransactionType.Sell)
                {
                    if (portfolioState.ContainsKey(txn.AssetId))
                    {
                        var state = portfolioState[txn.AssetId];
                        if (state.Units != 0)
                        {
                            var costPerUnit = state.TotalCost / state.Units;
                            // txn.Units is negative for a sell in Netwealth
                            state.TotalCost += (txn.Units * costPerUnit);
                        }
                        state.Units += txn.Units;
                    }
                }
                else if (txn.Type == TransactionType.Fee_Direct)
                {
                    if (portfolioState.ContainsKey(txn.AssetId))
                    {
                        portfolioState[txn.AssetId].TotalCost += Math.Abs(txn.Amount);
                    }
                }
                else if (txn.Type == TransactionType.Fee_Indirect)
                {
                    var totalPortfolioValue = portfolioState.Values.Sum(v => v.TotalCost);
                    if (totalPortfolioValue > 0)
                    {
                        var feeAmount = Math.Abs(txn.Amount);
                        foreach (var kvp in portfolioState)
                        {
                            var share = kvp.Value.TotalCost / totalPortfolioValue;
                            kvp.Value.TotalCost += (feeAmount * share);
                        }
                    }
                }
            }

            // Update DB Holdings
            var accountHoldings = await _context.Holdings.Where(h => h.AccountId == accountId).ToListAsync();

            // Assets currently held according to transactions
            var currentAssetIds = portfolioState.Keys.ToHashSet();

            // Remove holdings for assets no longer held or whose transactions were unloaded
            foreach (var existingHolding in accountHoldings.ToList())
            {
                if (!currentAssetIds.Contains(existingHolding.AssetId) || portfolioState[existingHolding.AssetId].Units == 0)
                {
                    _context.Holdings.Remove(existingHolding);
                    accountHoldings.Remove(existingHolding);
                }
            }

            foreach (var kvp in portfolioState)
            {
                if (kvp.Value.Units == 0) continue;

                var holding = accountHoldings.FirstOrDefault(h => h.AssetId == kvp.Key);
                if (holding == null)
                {
                    holding = new Holding { Id = Guid.NewGuid(), AccountId = accountId, AssetId = kvp.Key };
                    _context.Holdings.Add(holding);
                }
                holding.Units = kvp.Value.Units;
                holding.AvgCost = kvp.Value.TotalCost / kvp.Value.Units;
            }

            await _context.SaveChangesAsync();
        }

        private class HoldingState
        {
            public decimal Units { get; set; }
            public decimal TotalCost { get; set; }
        }
    }
}
