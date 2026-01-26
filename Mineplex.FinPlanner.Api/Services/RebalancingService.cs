using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IRebalancingService
    {
        Task<RebalancingReportDto> GetRebalancingReportAsync(Guid portfolioId);
        Task<List<Guid>> ExecuteRebalancingAsync(Guid portfolioId, List<RebalancingActionDto> actions, Guid userId, string userEmail);
    }

    public class RebalancingService : IRebalancingService
    {
        private readonly FinPlannerDbContext _context;
        private readonly IAuditService _auditService;

        public RebalancingService(FinPlannerDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<RebalancingReportDto> GetRebalancingReportAsync(Guid portfolioId)
        {
            var categories = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolioId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var holdings = await _context.Holdings
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .Include(h => h.Account)
                .Where(h => h.Account.PortfolioId == portfolioId)
                .ToListAsync();

            var totalPortfolioValue = holdings.Sum(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0));

            var report = new RebalancingReportDto
            {
                PortfolioId = portfolioId,
                TotalValue = totalPortfolioValue,
                Categories = new List<CategoryRebalanceDto>()
            };

            foreach (var category in categories)
            {
                var categoryHoldings = holdings.Where(h => h.CategoryId == category.Id).ToList();
                var currentValue = categoryHoldings.Sum(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0));
                var currentPercentage = totalPortfolioValue > 0 ? (currentValue / totalPortfolioValue) * 100 : 0;
                var targetValue = (category.TargetPercentage / 100) * totalPortfolioValue;
                var variance = currentValue - targetValue;

                var categoryDto = new CategoryRebalanceDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    TargetPercentage = category.TargetPercentage,
                    CurrentPercentage = currentPercentage,
                    CurrentValue = currentValue,
                    TargetValue = targetValue,
                    VarianceAmount = variance,
                    Recommendation = variance > 0 ? "Sell" : (variance < 0 ? "Buy" : "Hold"),
                    Assets = categoryHoldings.Select(h => new AssetHoldingsDto
                    {
                        AssetId = h.AssetId,
                        Symbol = h.Asset.Symbol,
                        Name = h.Asset.Name,
                        Units = h.Units,
                        CurrentPrice = h.Asset.CurrentPrice?.Price ?? 0,
                        TotalValue = h.Units * (h.Asset.CurrentPrice?.Price ?? 0),
                        WeightInCategory = currentValue > 0 ? (h.Units * (h.Asset.CurrentPrice?.Price ?? 0) / currentValue) * 100 : 0
                    }).ToList()
                };

                report.Categories.Add(categoryDto);
            }

            return report;
        }

        public async Task<List<Guid>> ExecuteRebalancingAsync(Guid portfolioId, List<RebalancingActionDto> actions, Guid userId, string userEmail)
        {
            var createdTransactionIds = new List<Guid>();

            foreach (var action in actions)
            {
                // Find an appropriate account to perform the transaction in
                // For simplicity, we'll pick the first account of the portfolio or the one the asset is already in
                var existingHolding = await _context.Holdings
                    .FirstOrDefaultAsync(h => h.AssetId == action.AssetId && h.Account.PortfolioId == portfolioId);

                Guid accountId;
                if (existingHolding != null)
                {
                    accountId = existingHolding.AccountId;
                }
                else
                {
                    var firstAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.PortfolioId == portfolioId);
                    if (firstAccount == null) continue;
                    accountId = firstAccount.Id;
                }

                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    AssetId = action.AssetId,
                    Type = action.Type,
                    Units = action.Units,
                    Amount = action.Amount, // Total dollars
                    EffectiveDate = DateTime.UtcNow,
                    Narration = $"Rebalancing: {action.Type} {action.Units} units based on target allocation"
                };

                _context.Transactions.Add(transaction);
                createdTransactionIds.Add(transaction.Id);

                // Update holdings (manual adjustment or let background task handle it? 
                // Usually we'd want a TransactionService to handle this properly)
                // For now, let's assume we have a TransactionService or we do it here.

                // Let's use simple logic here for now
                if (existingHolding != null)
                {
                    if (action.Type == TransactionType.Buy || action.Type == TransactionType.Deposit)
                    {
                        var newTotalCost = (existingHolding.Units * existingHolding.AvgCost) + Math.Abs(action.Amount);
                        existingHolding.Units += action.Units;
                        existingHolding.AvgCost = existingHolding.Units > 0 ? newTotalCost / existingHolding.Units : 0;
                    }
                    else if (action.Type == TransactionType.Sell || action.Type == TransactionType.Withdrawal)
                    {
                        existingHolding.Units -= action.Units;
                        // AvgCost remains same for Sells usually (FIFO/Specific ID is handled by TaxParcels)
                    }
                }
                else if (action.Type == TransactionType.Buy)
                {
                    var holding = new Holding
                    {
                        Id = Guid.NewGuid(),
                        AccountId = accountId,
                        AssetId = action.AssetId,
                        Units = action.Units,
                        AvgCost = action.Units > 0 ? Math.Abs(action.Amount) / action.Units : 0,
                        CategoryId = (await _context.AssetCategories.FirstOrDefaultAsync(c => c.PortfolioId == portfolioId))?.Id ?? Guid.Empty // Default category
                    };
                    _context.Holdings.Add(holding);
                }

                await _auditService.LogAsync(userId, userEmail, "ExecuteRebalancing", "Portfolio", portfolioId, $"Rebalanced {action.AssetId}");
            }

            await _context.SaveChangesAsync();
            return createdTransactionIds;
        }
    }

    // DTOs
    public class RebalancingReportDto
    {
        public Guid PortfolioId { get; set; }
        public decimal TotalValue { get; set; }
        public List<CategoryRebalanceDto> Categories { get; set; } = new();
    }

    public class CategoryRebalanceDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TargetPercentage { get; set; }
        public decimal CurrentPercentage { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal TargetValue { get; set; }
        public decimal VarianceAmount { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public List<AssetHoldingsDto> Assets { get; set; } = new();
    }

    public class AssetHoldingsDto
    {
        public Guid AssetId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal WeightInCategory { get; set; }
    }

    public class RebalancingActionDto
    {
        public Guid AssetId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Units { get; set; }
        public decimal Amount { get; set; }
    }
}
