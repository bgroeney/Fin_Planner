using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Models.Portfolios;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IPortfolioService
    {
        Task<List<PortfolioDto>> GetPortfoliosAsync(Guid userId);
        Task<PortfolioDto> GetPortfolioByIdAsync(Guid userId, Guid portfolioId);
        Task<PortfolioDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto dto);
        Task DeletePortfolioAsync(Guid userId, Guid portfolioId);
        Task<AssetDetailDto> GetAssetDetailsAsync(Guid userId, Guid portfolioId, Guid assetId);
        Task UpdateAssetAsync(Guid assetId, UpdateAssetDto dto);
        Task UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioDto dto);
        Task UpdatePortfolioAssetCategoryAsync(Guid portfolioId, Guid assetId, Guid categoryId);
        Task<List<AssetTransactionDto>> GetAssetTransactionsAsync(Guid userId, Guid portfolioId, Guid assetId, int page, int pageSize);
        Task<CategoryTargetDto> AddCategoryAsync(Guid portfolioId, CategoryTargetDto dto);
        Task UpdateCategoryAsync(Guid portfolioId, Guid categoryId, CategoryTargetDto dto);
        Task DeleteCategoryAsync(Guid portfolioId, Guid categoryId);
    }

    public class PortfolioService : IPortfolioService
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<PortfolioService> _logger;
        private readonly PriceSourceManager _priceManager;

        public PortfolioService(FinPlannerDbContext context, ILogger<PortfolioService> logger, PriceSourceManager priceManager)
        {
            _context = context;
            _logger = logger;
            _priceManager = priceManager;
        }

        public async Task<List<PortfolioDto>> GetPortfoliosAsync(Guid userId)
        {
            // 1. Fetch owned portfolios with benchmark asset included
            var ownedPortfolios = await _context.Portfolios
                .Where(p => p.OwnerId == userId)
                .Include(p => p.BenchmarkAsset)
                .ToListAsync();

            // 2. Fetch shared portfolios
            var sharedPortfolioIds = await _context.PortfolioShares
                .Where(ps => ps.SharedWithUserId == userId)
                .Select(ps => ps.PortfolioId)
                .ToListAsync();

            var sharedPortfolios = await _context.Portfolios
                .Where(p => sharedPortfolioIds.Contains(p.Id))
                .Include(p => p.BenchmarkAsset)
                .ToListAsync();

            var allPortfolios = ownedPortfolios.Concat(sharedPortfolios).ToList();
            var sharedSet = sharedPortfolioIds.ToHashSet();

            if (!allPortfolios.Any()) return new List<PortfolioDto>();

            var portfolioIds = allPortfolios.Select(p => p.Id).ToList();

            // 3. Fetch all categories for these portfolios in one query
            var allCategories = await _context.AssetCategories
                .Where(c => portfolioIds.Contains(c.PortfolioId))
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // 4. Fetch all accounts for these portfolios
            var allAccounts = await _context.Accounts
                .Where(a => portfolioIds.Contains(a.PortfolioId))
                .ToListAsync();

            var accountIds = allAccounts.Select(a => a.Id).ToList();

            // 5. Fetch all holdings for these accounts with asset and current price included
            var allHoldings = await _context.Holdings
                .Where(h => accountIds.Contains(h.AccountId))
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .ToListAsync();

            var result = new List<PortfolioDto>();

            // 6. Build lookup dictionaries for performance
            var categoriesByPortfolio = allCategories.ToLookup(c => c.PortfolioId);
            var accountsByPortfolio = allAccounts.ToLookup(a => a.PortfolioId);
            var holdingsByAccount = allHoldings.ToLookup(h => h.AccountId);

            foreach (var p in allPortfolios)
            {
                var pCategories = categoriesByPortfolio[p.Id].ToList();
                var pAccountIds = accountsByPortfolio[p.Id].Select(a => a.Id);

                decimal totalVal = 0;
                foreach (var accountId in pAccountIds)
                {
                    foreach (var h in holdingsByAccount[accountId])
                    {
                        var price = h.Asset.CurrentPrice?.Price ?? 0;
                        totalVal += h.Units * price;
                    }
                }

                var dto = MapToDto(p, pCategories);
                dto.TotalValue = totalVal;
                dto.IsShared = sharedSet.Contains(p.Id);
                result.Add(dto);
            }

            return result;
        }

        public async Task<PortfolioDto> GetPortfolioByIdAsync(Guid userId, Guid portfolioId)
        {
            // Check ownership first
            var portfolio = await _context.Portfolios
                .Include(p => p.BenchmarkAsset)
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);

            // If not owner, check if shared with user
            if (portfolio == null)
            {
                var hasShare = await _context.PortfolioShares
                    .AnyAsync(ps => ps.PortfolioId == portfolioId && ps.SharedWithUserId == userId);

                if (hasShare)
                {
                    portfolio = await _context.Portfolios
                        .Include(p => p.BenchmarkAsset)
                        .FirstOrDefaultAsync(p => p.Id == portfolioId);
                }
            }

            if (portfolio == null) throw new KeyNotFoundException("Portfolio not found");

            var categories = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolio.Id)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            var dto = MapToDto(portfolio, categories);
            dto.IsShared = portfolio.OwnerId != userId;
            return dto;
        }

        public async Task<PortfolioDto> CreatePortfolioAsync(Guid userId, CreatePortfolioDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var portfolio = new Portfolio
                    {
                        Id = Guid.NewGuid(),
                        OwnerId = userId,
                        Name = dto.Name,
                        TargetAllocation = "[]" // Storing as JSON empty by default, using relational table instead
                    };

                    _context.Portfolios.Add(portfolio);
                    await _context.SaveChangesAsync();

                    // Create Categories
                    var categories = new List<AssetCategory>();

                    if (dto.TargetAllocation != null && dto.TargetAllocation.Any())
                    {
                        foreach (var target in dto.TargetAllocation)
                        {
                            categories.Add(new AssetCategory
                            {
                                Id = Guid.NewGuid(),
                                PortfolioId = portfolio.Id,
                                Name = target.Name,
                                Code = target.Code,
                                TargetPercentage = target.TargetPercentage,
                                DisplayOrder = target.DisplayOrder
                            });
                        }
                    }
                    else
                    {
                        // Default Categories if none provided
                        categories.Add(new AssetCategory { Id = Guid.NewGuid(), PortfolioId = portfolio.Id, Name = "Australian Equities - Large Cap", Code = "AU_LARGE", TargetPercentage = 15, DisplayOrder = 1 });
                        categories.Add(new AssetCategory { Id = Guid.NewGuid(), PortfolioId = portfolio.Id, Name = "Australian Equities - Mid Cap", Code = "AU_MID", TargetPercentage = 10, DisplayOrder = 2 });
                        // Add other defaults as per requirements... simplified for brevity, user can edit later
                    }

                    _context.AssetCategories.AddRange(categories);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return MapToDto(portfolio, categories);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<AssetDetailDto> GetAssetDetailsAsync(Guid userId, Guid portfolioId, Guid assetId)
        {
            // 1. Verify Portfolio
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);
            if (portfolio == null) throw new KeyNotFoundException("Portfolio not found");

            // 2. Get Asset
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) throw new KeyNotFoundException("Asset not found");

            // 3. Get Account IDs
            var accountIds = await _context.Accounts
                .Where(a => a.PortfolioId == portfolioId)
                .Select(a => a.Id)
                .ToListAsync();

            // 4. Aggregate Holdings
            var holdings = await _context.Holdings
                .Where(h => accountIds.Contains(h.AccountId) && h.AssetId == assetId)
                .ToListAsync();

            var totalUnits = holdings.Sum(h => h.Units);
            var totalCost = holdings.Sum(h => h.Units * h.AvgCost);
            var avgCost = totalUnits > 0 ? totalCost / totalUnits : 0;

            // 5. Get Real-time Price
            decimal currentPrice = 0;
            try
            {
                var priceInfo = await _priceManager.GetPriceWithFallbackAsync(asset);
                currentPrice = priceInfo?.Price ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch price for {Symbol}", asset.Symbol);
                // currentPrice remains 0
            }

            var currentValue = totalUnits * currentPrice;

            // 6. Get Transactions
            var transactions = await _context.Transactions
                .Where(t => accountIds.Contains(t.AccountId) && t.AssetId == assetId)
                .OrderByDescending(t => t.EffectiveDate)
                .Select(t => new AssetTransactionDto
                {
                    Date = t.EffectiveDate,
                    Type = t.Type.ToString(),
                    Units = t.Units,
                    Price = t.Amount != 0 && t.Units != 0 ? Math.Abs(t.Amount / t.Units) : 0, // IMPLIED PRICE
                    Cost = t.Amount, // Stored as flow?
                    Value = 0, // Historical value not stored, could be calculated
                    ProfitLoss = 0 // Valid for Sell
                })
                .ToListAsync();

            // 7. Get History (1 Year)
            var history = new List<HistoricalPriceDto>();
            try
            {
                history = await _priceManager.GetHistoricalPricesAsync(asset, DateTime.UtcNow.AddYears(-1), DateTime.UtcNow);

                // Replay Logic
                if (history.Any())
                {
                    history = history.OrderBy(h => h.Date).ToList();
                    decimal units = totalUnits;
                    var sortedTx = transactions.OrderByDescending(t => t.Date).ToList();
                    int txIndex = 0;

                    // Reverse loop
                    for (int i = history.Count - 1; i >= 0; i--)
                    {
                        var point = history[i];
                        point.Value = units * point.Price;

                        while (txIndex < sortedTx.Count && sortedTx[txIndex].Date.Date > point.Date.Date)
                        {
                            var t = sortedTx[txIndex];
                            if (t.Type == "Buy" || t.Type == "Deposit" || t.Type == "Dividend") units -= t.Units;
                            else if (t.Type == "Sell" || t.Type == "Withdrawal") units += t.Units;
                            txIndex++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch history for {Symbol}", asset.Symbol);
            }

            var categoryId = holdings.FirstOrDefault()?.CategoryId;

            // XIRR Calculation
            double xirr = 0;
            try
            {
                var flows = new List<(DateTime Date, double Amount)>();
                foreach (var t in transactions)
                {
                    // Use Cost (Amount) directly assuming convention holds
                    flows.Add((t.Date, (double)t.Cost));
                }
                // Terminal Value
                flows.Add((DateTime.UtcNow, (double)currentValue));

                xirr = CalculateXirr(flows);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("XIRR Failed: {Message}", ex.Message);
            }

            return new AssetDetailDto
            {
                AssetId = asset.Id,
                CategoryId = categoryId,
                Symbol = asset.Symbol,
                Name = asset.Name,
                CurrentPrice = currentPrice,
                AnnualisedReturn = xirr,
                Position = new PositionSummaryDto
                {
                    Units = totalUnits,
                    AvgCost = avgCost,
                    TotalCost = totalCost,
                    CurrentValue = currentValue,
                    ProfitLoss = currentValue - totalCost,
                    ReturnPercent = totalCost != 0 ? ((currentValue - totalCost) / totalCost) * 100 : 0
                },
                History = history
            };
        }

        public async Task DeletePortfolioAsync(Guid userId, Guid portfolioId)
        {
            var portfolio = await _context.Portfolios
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);

            if (portfolio == null) throw new KeyNotFoundException("Portfolio not found");

            // Log what will be deleted (cascade delete handles the actual cleanup)
            var accountCount = await _context.Accounts.CountAsync(a => a.PortfolioId == portfolioId);
            var holdingCount = await _context.Holdings.CountAsync(h => h.Account.PortfolioId == portfolioId);
            var transactionCount = await _context.Transactions.CountAsync(t => t.Account.PortfolioId == portfolioId);
            var categoryCount = await _context.AssetCategories.CountAsync(c => c.PortfolioId == portfolioId);
            var decisionCount = await _context.Decisions.CountAsync(d => d.PortfolioId == portfolioId);

            _logger.LogInformation(
                "Deleting portfolio {PortfolioId} ({PortfolioName}) with {AccountCount} accounts, " +
                "{HoldingCount} holdings, {TransactionCount} transactions, {CategoryCount} categories, " +
                "{DecisionCount} decisions",
                portfolioId, portfolio.Name, accountCount, holdingCount, transactionCount, categoryCount, decisionCount);

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully deleted portfolio {PortfolioId}", portfolioId);
        }

        private PortfolioDto MapToDto(Portfolio portfolio, List<AssetCategory> categories)
        {
            return new PortfolioDto
            {
                Id = portfolio.Id,
                Name = portfolio.Name,
                TargetAllocation = categories.Select(c => new CategoryTargetDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = c.Code,
                    TargetPercentage = c.TargetPercentage,
                    DisplayOrder = c.DisplayOrder
                }).ToList(),
                TotalValue = 0, // Calculated later from Holdings
                BenchmarkAssetId = portfolio.BenchmarkAssetId,
                BenchmarkSymbol = portfolio.BenchmarkAsset?.Symbol ?? string.Empty
            };
        }

        public async Task UpdatePortfolioAsync(Guid userId, Guid portfolioId, UpdatePortfolioDto dto)
        {
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);
            if (portfolio == null) throw new KeyNotFoundException("Portfolio not found");

            if (!string.IsNullOrWhiteSpace(dto.Name)) portfolio.Name = dto.Name;
            if (dto.BenchmarkAssetId.HasValue)
            {
                // Verify Asset exists
                if (!await _context.Assets.AnyAsync(a => a.Id == dto.BenchmarkAssetId.Value))
                    throw new KeyNotFoundException("Benchmark Asset not found");

                portfolio.BenchmarkAssetId = dto.BenchmarkAssetId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task UpdateAssetAsync(Guid assetId, UpdateAssetDto dto)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) throw new KeyNotFoundException("Asset not found");

            if (!string.IsNullOrWhiteSpace(dto.Name)) asset.Name = dto.Name;
            // if (!string.IsNullOrWhiteSpace(dto.Sector)) asset.Sector = dto.Sector; // If we add Sector to Asset model later

            await _context.SaveChangesAsync();
        }

        public async Task UpdatePortfolioAssetCategoryAsync(Guid portfolioId, Guid assetId, Guid categoryId)
        {
            // Verify Category belongs to Portfolio
            var category = await _context.AssetCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.PortfolioId == portfolioId);

            if (category == null) throw new KeyNotFoundException("Category not found or does not belong to this portfolio.");

            // Find all holdings of this asset in accounts belonging to the portfolio
            var holdings = await _context.Holdings
                .Include(h => h.Account)
                .Where(h => h.Account.PortfolioId == portfolioId && h.AssetId == assetId)
                .ToListAsync();

            foreach (var holding in holdings)
            {
                holding.CategoryId = categoryId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<CategoryTargetDto> AddCategoryAsync(Guid portfolioId, CategoryTargetDto dto)
        {
            // Validate total won't exceed 100%
            var currentSum = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolioId)
                .SumAsync(c => c.TargetPercentage);
            if (currentSum + dto.TargetPercentage > 100)
                throw new InvalidOperationException($"Total allocation would be {currentSum + dto.TargetPercentage}%, which exceeds 100%.");

            var maxOrder = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolioId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;

            var category = new AssetCategory
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolioId,
                Name = dto.Name,
                Code = dto.Code,
                TargetPercentage = dto.TargetPercentage,
                DisplayOrder = maxOrder + 1
            };

            _context.AssetCategories.Add(category);
            await _context.SaveChangesAsync();

            return new CategoryTargetDto
            {
                Id = category.Id,
                Name = category.Name,
                Code = category.Code,
                TargetPercentage = category.TargetPercentage,
                DisplayOrder = category.DisplayOrder
            };
        }

        public async Task UpdateCategoryAsync(Guid portfolioId, Guid categoryId, CategoryTargetDto dto)
        {
            var category = await _context.AssetCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.PortfolioId == portfolioId);
            if (category == null) throw new KeyNotFoundException("Category not found.");

            // Validate total won't exceed 100% (excluding this category's current value)
            var othersSum = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolioId && c.Id != categoryId)
                .SumAsync(c => c.TargetPercentage);
            if (othersSum + dto.TargetPercentage > 100)
                throw new InvalidOperationException($"Total allocation would be {othersSum + dto.TargetPercentage}%, which exceeds 100%.");

            category.Name = dto.Name;
            category.Code = dto.Code;
            category.TargetPercentage = dto.TargetPercentage;
            if (dto.DisplayOrder > 0) category.DisplayOrder = dto.DisplayOrder;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(Guid portfolioId, Guid categoryId)
        {
            var category = await _context.AssetCategories
                .FirstOrDefaultAsync(c => c.Id == categoryId && c.PortfolioId == portfolioId);
            if (category == null) throw new KeyNotFoundException("Category not found.");

            // Unassign holdings from this category
            var holdings = await _context.Holdings
                .Include(h => h.Account)
                .Where(h => h.Account.PortfolioId == portfolioId && h.CategoryId == categoryId)
                .ToListAsync();

            foreach (var h in holdings) h.CategoryId = null;

            _context.AssetCategories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AssetTransactionDto>> GetAssetTransactionsAsync(Guid userId, Guid portfolioId, Guid assetId, int page, int pageSize)
        {
            // 1. Verify Portfolio (security)
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);
            if (portfolio == null) throw new KeyNotFoundException("Portfolio not found");

            var accountIds = await _context.Accounts
                .Where(a => a.PortfolioId == portfolioId)
                .Select(a => a.Id)
                .ToListAsync();

            var query = _context.Transactions
                .Where(t => accountIds.Contains(t.AccountId) && t.AssetId == assetId)
                .OrderByDescending(t => t.EffectiveDate)
                .Select(t => new AssetTransactionDto
                {
                    Date = t.EffectiveDate,
                    Type = t.Type.ToString(),
                    Units = t.Units,
                    Price = t.Amount != 0 && t.Units != 0 ? Math.Abs(t.Amount / t.Units) : 0,
                    Cost = t.Amount
                });

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        private double CalculateXirr(List<(DateTime Date, double Amount)> flows, double guess = 0.1)
        {
            var maxIter = 100;
            var tol = 1e-6;
            var r = guess;

            if (!flows.Any()) return 0;
            var startDate = flows.Min(f => f.Date);

            for (int i = 0; i < maxIter; i++)
            {
                double fValue = 0;
                double fDeriv = 0;

                foreach (var flow in flows)
                {
                    var days = (flow.Date - startDate).TotalDays;
                    var years = days / 365.0;

                    if (years == 0)
                    {
                        fValue += flow.Amount;
                    }
                    else
                    {
                        var factor = Math.Pow(1 + r, years);
                        fValue += flow.Amount / factor;
                        fDeriv -= (years * flow.Amount) / (factor * (1 + r));
                    }
                }

                if (Math.Abs(fValue) < tol) return r;
                if (fDeriv == 0) return 0;

                var newR = r - fValue / fDeriv;
                if (Math.Abs(newR - r) < tol)
                {
                    return double.IsFinite(newR) ? newR : 0;
                }
                r = newR;
            }
            return double.IsFinite(r) ? r : 0;
        }
    }
}
