using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Models.Portfolios;
using System.Text.Json;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IDemoDataService
    {
        Task<PortfolioDto> CreateDemoPortfolioAsync(Guid userId, string name);
    }

    public class DemoDataService : IDemoDataService
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<DemoDataService> _logger;

        public DemoDataService(FinPlannerDbContext context, ILogger<DemoDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PortfolioDto> CreateDemoPortfolioAsync(Guid userId, string name)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _logger.LogInformation("Creating demo portfolio for user {UserId}", userId);

                    // 1. Create Portfolio
                    _logger.LogInformation("Step 1: Creating Portfolio");
                    var portfolio = new Portfolio
                    {
                        Id = Guid.NewGuid(),
                        OwnerId = userId,
                        Name = name,
                        TargetAllocation = "[]"
                    };
                    _context.Portfolios.Add(portfolio);
                    await _context.SaveChangesAsync();

                    // 2. Create Asset Categories
                    _logger.LogInformation("Step 2: Creating Categories");
                    var categories = CreateCategories(portfolio.Id);
                    _context.AssetCategories.AddRange(categories);
                    await _context.SaveChangesAsync();

                    var categoryLookup = categories.ToDictionary(c => c.Code, c => c.Id);

                    // 3. Create/Get Assets
                    _logger.LogInformation("Step 3: Ensuring Assets Exist");
                    var assets = await EnsureAssetsExist();
                    var assetLookup = assets.ToDictionary(a => a.Symbol, a => a);

                    // 4. Create Accounts
                    _logger.LogInformation("Step 4: Creating Accounts");
                    var accounts = CreateAccounts(portfolio.Id);
                    _context.Accounts.AddRange(accounts);
                    await _context.SaveChangesAsync();

                    var brokerageAccount = accounts.First(a => a.AccountNumber == "COMMSEC001");
                    var superAccount = accounts.First(a => a.AccountNumber == "AUSSUPER001");
                    var tdAccount = accounts.First(a => a.AccountNumber == "CBA_TD001");

                    // 5. Create Holdings with Transactions
                    _logger.LogInformation("Step 5: Creating Holdings and Transactions");
                    var (holdings, transactions) = CreateHoldingsAndTransactions(
                        brokerageAccount, superAccount, tdAccount,
                        assetLookup, categoryLookup);

                    _context.Holdings.AddRange(holdings);
                    _context.Transactions.AddRange(transactions);
                    await _context.SaveChangesAsync();

                    // 6. Create Sample Decisions
                    _logger.LogInformation("Step 6: Creating Decisions");
                    var decisions = CreateDecisions(portfolio.Id);
                    _context.Decisions.AddRange(decisions);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Step 7: Committing Transaction");

                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "Demo portfolio created: {PortfolioId} with {HoldingCount} holdings, {TransactionCount} transactions",
                        portfolio.Id, holdings.Count, transactions.Count);

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
                        TotalValue = 0 // Will be calculated on fetch
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create demo portfolio");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private List<AssetCategory> CreateCategories(Guid portfolioId)
        {
            return new List<AssetCategory>
            {
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "Australian Equities - Large Cap", Code = "AU_LARGE", TargetPercentage = 25, DisplayOrder = 1 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "Australian Equities - Mid/Small Cap", Code = "AU_SMALL", TargetPercentage = 10, DisplayOrder = 2 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "International Equities - Developed", Code = "INT_DEV", TargetPercentage = 25, DisplayOrder = 3 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "International Equities - Emerging", Code = "INT_EM", TargetPercentage = 5, DisplayOrder = 4 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "Fixed Income - Domestic", Code = "FI_AU", TargetPercentage = 15, DisplayOrder = 5 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "Fixed Income - International", Code = "FI_INT", TargetPercentage = 5, DisplayOrder = 6 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "REITs & Property", Code = "REIT", TargetPercentage = 5, DisplayOrder = 7 },
                new() { Id = Guid.NewGuid(), PortfolioId = portfolioId, Name = "Cash & Term Deposits", Code = "CASH", TargetPercentage = 10, DisplayOrder = 8 }
            };
        }

        private async Task<List<Asset>> EnsureAssetsExist()
        {
            var assetDefinitions = new List<(string Symbol, string Name, string AssetType, string? Market)>
            {
                ("VAS.AX", "Vanguard Australian Shares Index ETF", "ETF", "ASX"),
                ("CBA.AX", "Commonwealth Bank of Australia", "Stock", "ASX"),
                ("BHP.AX", "BHP Group Limited", "Stock", "ASX"),
                ("CSL.AX", "CSL Limited", "Stock", "ASX"),
                ("WES.AX", "Wesfarmers Limited", "Stock", "ASX"),
                ("VSO.AX", "Vanguard Small Companies Index ETF", "ETF", "ASX"),
                ("VGS.AX", "Vanguard MSCI Index International Shares ETF", "ETF", "ASX"),
                ("IVV.AX", "iShares S&P 500 ETF", "ETF", "ASX"),
                ("VGE.AX", "Vanguard FTSE Emerging Markets Shares ETF", "ETF", "ASX"),
                ("VAF.AX", "Vanguard Australian Fixed Interest Index ETF", "ETF", "ASX"),
                ("VIF.AX", "Vanguard International Fixed Interest Index ETF", "ETF", "ASX"),
                ("VAP.AX", "Vanguard Australian Property Securities Index ETF", "ETF", "ASX"),
                ("CASH_AUD", "Australian Dollar Cash", "Cash", null),
                ("TD_CBA_5.1", "CBA Term Deposit 5.1% p.a.", "TermDeposit", null)
            };

            var existingSymbols = await _context.Assets
                .Where(a => assetDefinitions.Select(d => d.Symbol).Contains(a.Symbol))
                .Select(a => a.Symbol)
                .ToListAsync();

            var newAssets = assetDefinitions
                .Where(d => !existingSymbols.Contains(d.Symbol))
                .Select(d => new Asset
                {
                    Id = Guid.NewGuid(),
                    Symbol = d.Symbol,
                    Name = d.Name,
                    AssetType = d.AssetType,
                    Market = d.Market
                })
                .ToList();

            if (newAssets.Any())
            {
                _context.Assets.AddRange(newAssets);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} new assets for demo data", newAssets.Count);
            }

            return await _context.Assets
                .Where(a => assetDefinitions.Select(d => d.Symbol).Contains(a.Symbol))
                .ToListAsync();
        }

        private List<Account> CreateAccounts(Guid portfolioId)
        {
            return new List<Account>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolioId,
                    AccountNumber = "COMMSEC001",
                    AccountName = "CommSec Share Trading",
                    Provider = "Commonwealth Securities",
                    ProductType = "Brokerage"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolioId,
                    AccountNumber = "AUSSUPER001",
                    AccountName = "AustralianSuper Member Direct",
                    Provider = "AustralianSuper",
                    ProductType = "Superannuation"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolioId,
                    AccountNumber = "CBA_TD001",
                    AccountName = "CBA Term Deposit",
                    Provider = "Commonwealth Bank",
                    ProductType = "Term Deposit"
                }
            };
        }

        private (List<Holding>, List<Transaction>) CreateHoldingsAndTransactions(
            Account brokerageAccount,
            Account superAccount,
            Account tdAccount,
            Dictionary<string, Asset> assets,
            Dictionary<string, Guid> categories)
        {
            var holdings = new List<Holding>();
            var transactions = new List<Transaction>();
            var random = new Random(42); // Fixed seed for reproducibility

            // Helper to add a holding with transactions
            void AddHolding(Account account, string symbol, string categoryCode, decimal units, decimal avgCost, List<(DateTime Date, decimal Units, decimal Price)> buys)
            {
                var asset = assets[symbol];
                var categoryId = categories[categoryCode];

                holdings.Add(new Holding
                {
                    Id = Guid.NewGuid(),
                    AccountId = account.Id,
                    AssetId = asset.Id,
                    CategoryId = categoryId,
                    Units = units,
                    AvgCost = avgCost
                });

                int order = 0;
                foreach (var (date, txUnits, price) in buys)
                {
                    transactions.Add(new Transaction
                    {
                        Id = Guid.NewGuid(),
                        AccountId = account.Id,
                        AssetId = asset.Id,
                        Type = TransactionType.Buy,
                        Units = txUnits,
                        Amount = -txUnits * price, // Negative for outflow
                        EffectiveDate = date,
                        AttachedOrder = order++,
                        Narration = $"Buy {txUnits:N2} {symbol} @ ${price:N2}"
                    });
                }
            }

            // ===== BROKERAGE ACCOUNT =====

            // VAS.AX - Regular DCA over 2 years
            AddHolding(brokerageAccount, "VAS.AX", "AU_LARGE", 450m, 88.50m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc), 50, 85.20m),
                (new DateTime(2023, 4, 15, 0, 0, 0, DateTimeKind.Utc), 50, 82.40m),
                (new DateTime(2023, 7, 15, 0, 0, 0, DateTimeKind.Utc), 50, 86.80m),
                (new DateTime(2023, 10, 15, 0, 0, 0, DateTimeKind.Utc), 50, 84.50m),
                (new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc), 50, 89.20m),
                (new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc), 50, 91.30m),
                (new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc), 50, 93.40m),
                (new DateTime(2024, 10, 15, 0, 0, 0, DateTimeKind.Utc), 50, 95.20m),
                (new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc), 50, 92.80m)
            });

            // CBA.AX - Blue chip holding
            AddHolding(brokerageAccount, "CBA.AX", "AU_LARGE", 120m, 98.50m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2022, 6, 20, 0, 0, 0, DateTimeKind.Utc), 60, 92.40m),
                (new DateTime(2023, 3, 10, 0, 0, 0, DateTimeKind.Utc), 40, 101.20m),
                (new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc), 20, 108.50m)
            });

            // BHP.AX
            AddHolding(brokerageAccount, "BHP.AX", "AU_LARGE", 80m, 45.20m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2022, 8, 10, 0, 0, 0, DateTimeKind.Utc), 50, 42.80m),
                (new DateTime(2023, 11, 20, 0, 0, 0, DateTimeKind.Utc), 30, 48.90m)
            });

            // CSL.AX - High value stock
            AddHolding(brokerageAccount, "CSL.AX", "AU_LARGE", 25m, 285.40m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 2, 5, 0, 0, 0, DateTimeKind.Utc), 15, 278.50m),
                (new DateTime(2024, 5, 12, 0, 0, 0, DateTimeKind.Utc), 10, 295.80m)
            });

            // WES.AX
            AddHolding(brokerageAccount, "WES.AX", "AU_LARGE", 45m, 52.30m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 9, 8, 0, 0, 0, DateTimeKind.Utc), 45, 52.30m)
            });

            // VSO.AX - Small cap ETF
            AddHolding(brokerageAccount, "VSO.AX", "AU_SMALL", 200m, 58.40m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 5, 20, 0, 0, 0, DateTimeKind.Utc), 100, 56.80m),
                (new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc), 100, 60.00m)
            });

            // IVV.AX - US equity exposure
            AddHolding(brokerageAccount, "IVV.AX", "INT_DEV", 180m, 42.50m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2022, 12, 1, 0, 0, 0, DateTimeKind.Utc), 60, 38.20m),
                (new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc), 60, 42.80m),
                (new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), 60, 46.50m)
            });

            // VAP.AX - Property ETF
            AddHolding(brokerageAccount, "VAP.AX", "REIT", 150m, 78.20m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 8, 15, 0, 0, 0, DateTimeKind.Utc), 100, 75.40m),
                (new DateTime(2024, 8, 15, 0, 0, 0, DateTimeKind.Utc), 50, 83.80m)
            });

            // Cash holding in brokerage
            AddHolding(brokerageAccount, "CASH_AUD", "CASH", 15000m, 1.00m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 15000, 1.00m)
            });

            // ===== SUPER ACCOUNT =====

            // VGS.AX - International developed
            AddHolding(superAccount, "VGS.AX", "INT_DEV", 350m, 95.80m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2022, 7, 1, 0, 0, 0, DateTimeKind.Utc), 100, 88.40m),
                (new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), 100, 92.60m),
                (new DateTime(2023, 7, 1, 0, 0, 0, DateTimeKind.Utc), 75, 98.20m),
                (new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), 75, 103.50m)
            });

            // VGE.AX - Emerging markets
            AddHolding(superAccount, "VGE.AX", "INT_EM", 200m, 68.50m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 4, 1, 0, 0, 0, DateTimeKind.Utc), 100, 65.80m),
                (new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc), 100, 71.20m)
            });

            // VAF.AX - Domestic bonds
            AddHolding(superAccount, "VAF.AX", "FI_AU", 400m, 48.20m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2022, 9, 1, 0, 0, 0, DateTimeKind.Utc), 200, 50.40m),
                (new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc), 200, 46.00m)
            });

            // VIF.AX - International bonds
            AddHolding(superAccount, "VIF.AX", "FI_INT", 250m, 52.60m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2023, 3, 1, 0, 0, 0, DateTimeKind.Utc), 150, 54.20m),
                (new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc), 100, 50.10m)
            });

            // ===== TERM DEPOSIT ACCOUNT =====

            // Term deposit
            AddHolding(tdAccount, "TD_CBA_5.1", "CASH", 50000m, 1.00m, new List<(DateTime, decimal, decimal)>
            {
                (new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc), 50000, 1.00m)
            });

            // Add some dividend transactions for select holdings
            var dividends = new List<Transaction>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    AccountId = brokerageAccount.Id,
                    AssetId = assets["CBA.AX"].Id,
                    Type = TransactionType.Dividend,
                    Units = 0,
                    Amount = 240.00m, // Positive for inflow
                    EffectiveDate = new DateTime(2024, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                    AttachedOrder = 0,
                    Narration = "Dividend - CBA.AX Final FY24"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AccountId = brokerageAccount.Id,
                    AssetId = assets["CBA.AX"].Id,
                    Type = TransactionType.Dividend,
                    Units = 0,
                    Amount = 215.00m,
                    EffectiveDate = new DateTime(2024, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                    AttachedOrder = 0,
                    Narration = "Dividend - CBA.AX Interim FY24"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AccountId = brokerageAccount.Id,
                    AssetId = assets["BHP.AX"].Id,
                    Type = TransactionType.Dividend,
                    Units = 0,
                    Amount = 180.00m,
                    EffectiveDate = new DateTime(2024, 9, 5, 0, 0, 0, DateTimeKind.Utc),
                    AttachedOrder = 0,
                    Narration = "Dividend - BHP.AX Final FY24"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    AccountId = brokerageAccount.Id,
                    AssetId = assets["VAS.AX"].Id,
                    Type = TransactionType.Distribution,
                    Units = 0,
                    Amount = 850.00m,
                    EffectiveDate = new DateTime(2024, 7, 5, 0, 0, 0, DateTimeKind.Utc),
                    AttachedOrder = 0,
                    Narration = "Distribution - VAS.AX Annual FY24"
                }
            };

            transactions.AddRange(dividends);

            return (holdings, transactions);
        }

        private List<Decision> CreateDecisions(Guid portfolioId)
        {
            var snapshotBefore = JsonSerializer.Serialize(new
            {
                AU_LARGE = 28.5,
                AU_SMALL = 9.2,
                INT_DEV = 22.8,
                INT_EM = 4.8,
                FI_AU = 14.5,
                FI_INT = 5.2,
                REIT = 4.5,
                CASH = 10.5
            });

            var snapshotAfter = JsonSerializer.Serialize(new
            {
                AU_LARGE = 25.0,
                AU_SMALL = 10.0,
                INT_DEV = 25.0,
                INT_EM = 5.0,
                FI_AU = 15.0,
                FI_INT = 5.0,
                REIT = 5.0,
                CASH = 10.0
            });

            return new List<Decision>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolioId,
                    Title = "Q3 2024 Rebalancing",
                    Type = "Manual",
                    Status = "Implemented",
                    Rationale = "Quarterly rebalance to maintain target allocation. Reduced overweight AU Large Cap position, increased International Developed exposure.",
                    SnapshotBefore = snapshotBefore,
                    SnapshotAfter = snapshotAfter,
                    ProjectedTaxImpact = -1250.00m,
                    AllocationMethod = "MinTax",
                    CreatedAt = new DateTime(2024, 9, 15, 10, 0, 0, DateTimeKind.Utc),
                    ApprovedAt = new DateTime(2024, 9, 16, 14, 30, 0, DateTimeKind.Utc),
                    ImplementedAt = new DateTime(2024, 9, 18, 9, 15, 0, DateTimeKind.Utc)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolioId,
                    Title = "Increase Emerging Markets Allocation",
                    Type = "AI",
                    Status = "Pending",
                    Rationale = "Based on current valuations and growth outlook, consider increasing emerging markets exposure from 5% to 8% by reducing domestic fixed income.",
                    SnapshotBefore = snapshotAfter, // Current state
                    SnapshotAfter = JsonSerializer.Serialize(new
                    {
                        AU_LARGE = 25.0,
                        AU_SMALL = 10.0,
                        INT_DEV = 25.0,
                        INT_EM = 8.0,
                        FI_AU = 12.0,
                        FI_INT = 5.0,
                        REIT = 5.0,
                        CASH = 10.0
                    }),
                    ProjectedTaxImpact = -420.00m,
                    AllocationMethod = "FIFO",
                    CreatedAt = new DateTime(2025, 1, 10, 8, 0, 0, DateTimeKind.Utc)
                }
            };
        }
    }
}
