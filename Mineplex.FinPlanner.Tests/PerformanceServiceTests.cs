using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Xunit;

namespace Mineplex.FinPlanner.Tests
{
    public class PerformanceServiceTests
    {
        [Fact]
        public async Task RebuildHistoryAsync_CalculatesCorrectly()
        {
            // Setup
            var options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new FinPlannerDbContext(options);

            // Seed Data
            var portfolioId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Name = "Test Portfolio",
                OwnerId = ownerId,
                TargetAllocation = "{}"
            };

            var accountId = Guid.NewGuid();
            var account = new Account
            {
                Id = accountId,
                PortfolioId = portfolioId,
                AccountName = "Test Account",
                ProductType = "Brokerage",
                AccountNumber = "ACC-001",
                Provider = "Test Bank"
            };

            var assetId = Guid.NewGuid();
            var asset = new Asset { Id = assetId, Symbol = "TEST", Name = "Test Asset", AssetType = "Stock" };

            var startDate = new DateTime(2023, 1, 1);
            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                AssetId = assetId,
                Type = TransactionType.Buy,
                Units = 10,
                Amount = 1000,
                EffectiveDate = startDate,
                Narration = "Initial Buy"
            };

            // Historical Prices
            // T (Start): 100
            // T+1: 105
            // T+2: 110
            var prices = new List<HistoricalPrice>
            {
                new HistoricalPrice { Id = Guid.NewGuid(), AssetId = assetId, Date = startDate, ClosePrice = 100 },
                new HistoricalPrice { Id = Guid.NewGuid(), AssetId = assetId, Date = startDate.AddDays(1), ClosePrice = 105 },
                new HistoricalPrice { Id = Guid.NewGuid(), AssetId = assetId, Date = startDate.AddDays(2), ClosePrice = 110 }
            };

            context.Portfolios.Add(portfolio);
            context.Accounts.Add(account);
            context.Assets.Add(asset);
            context.Transactions.Add(transaction);
            context.HistoricalPrices.AddRange(prices);
            await context.SaveChangesAsync();

            // Act
            var service = new PerformanceService(context, NullLogger<PerformanceService>.Instance);
            await service.RebuildHistoryAsync(portfolioId);

            // Assert
            var snapshots = await context.PerformanceSnapshots
                .Where(s => s.PortfolioId == portfolioId)
                .OrderBy(s => s.Date)
                .ToListAsync();

            // We expect snapshots from StartDate to Today (UtcNow).
            Assert.NotEmpty(snapshots);

            // Check Day 1
            var s1 = snapshots.FirstOrDefault(s => s.Date == startDate);
            Assert.NotNull(s1);
            Assert.Equal(10 * 100, s1.TotalValue); // 1000

            // Check Day 2
            var s2 = snapshots.FirstOrDefault(s => s.Date == startDate.AddDays(1));
            Assert.NotNull(s2);
            Assert.Equal(10 * 105, s2.TotalValue); // 1050

            // Check Day 3
            var s3 = snapshots.FirstOrDefault(s => s.Date == startDate.AddDays(2));
            Assert.NotNull(s3);
            Assert.Equal(10 * 110, s3.TotalValue); // 1100
        }
    }
}
