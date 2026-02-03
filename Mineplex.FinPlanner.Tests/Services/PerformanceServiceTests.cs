using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Xunit;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class PerformanceServiceTests
    {
        private FinPlannerDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new FinPlannerDbContext(options);
        }

        [Fact]
        public async Task RebuildHistoryAsync_CalculatesCorrectly()
        {
            // Arrange
            using var context = GetDbContext();
            var logger = NullLogger<PerformanceService>.Instance;
            var service = new PerformanceService(context, logger);

            var portfolioId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var assetId = Guid.NewGuid();

            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Name = "Test Portfolio",
                TargetAllocation = "{}"
            };
            var account = new Account
            {
                Id = accountId,
                PortfolioId = portfolioId,
                AccountName = "Test Account",
                AccountNumber = "123456",
                Provider = "Test Provider",
                ProductType = "Savings"
            };
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "TEST",
                Name = "Test Asset",
                AssetType = "Stock"
            };

            context.Portfolios.Add(portfolio);
            context.Accounts.Add(account);
            context.Assets.Add(asset);

            var startDate = new DateTime(2023, 1, 1);

            // Buy 10 units on Jan 1st
            context.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                AssetId = assetId,
                Type = TransactionType.Buy,
                EffectiveDate = startDate,
                Units = 10,
                Amount = 1000,
                Narration = "Initial Buy"
            });

            // Add prices for Jan 1st to Jan 5th
            for (int i = 0; i < 5; i++)
            {
                context.HistoricalPrices.Add(new HistoricalPrice
                {
                    Id = Guid.NewGuid(),
                    AssetId = assetId,
                    Date = startDate.AddDays(i),
                    ClosePrice = 100 + i // 100, 101, 102, 103, 104
                });
            }

            await context.SaveChangesAsync();

            // Act
            await service.RebuildHistoryAsync(portfolioId);

            // Assert
            var snapshots = await context.PerformanceSnapshots
                .Where(s => s.PortfolioId == portfolioId)
                .OrderBy(s => s.Date)
                .ToListAsync();

            // We expect snapshots from Jan 1st up to Today.
            // But checking just the first 5 days where we have data is enough to verify calculation logic.

            Assert.NotEmpty(snapshots);

            // Check Jan 1st
            var day1 = snapshots.FirstOrDefault(s => s.Date == startDate);
            Assert.NotNull(day1);
            Assert.Equal(1000m, day1.TotalValue); // 10 units * 100

            // Check Jan 5th
            var day5 = snapshots.FirstOrDefault(s => s.Date == startDate.AddDays(4));
            Assert.NotNull(day5);
            Assert.Equal(1040m, day5.TotalValue); // 10 units * 104
        }
    }
}
