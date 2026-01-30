using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
            // Arrange
            using var context = TestDbContextFactory.Create();
            var logger = NullLogger<PerformanceService>.Instance;
            var service = new PerformanceService(context, logger);

            var (user, portfolio, account, asset) = Seeder.SeedBasicPortfolio(context);

            var startDate = DateTime.UtcNow.AddDays(-10).Date;

            // Buy 10 units
            Seeder.SeedTransaction(context, account, asset, startDate, 10, 1000, TransactionType.Buy);

            // Add historical prices
            for (int i = 0; i <= 10; i++)
            {
                var date = startDate.AddDays(i);
                Seeder.SeedHistoricalPrice(context, asset, date, 100 + i); // Price increases by 1 each day
            }

            // Act
            await service.RebuildHistoryAsync(portfolio.Id);

            // Assert
            var snapshots = await context.PerformanceSnapshots
                .Where(s => s.PortfolioId == portfolio.Id)
                .OrderBy(s => s.Date)
                .ToListAsync();

            Assert.NotEmpty(snapshots);
            Assert.Equal(11, snapshots.Count); // 0 to 10 days inclusive

            // Check first day
            var firstSnapshot = snapshots.First();
            Assert.Equal(startDate, firstSnapshot.Date);
            Assert.Equal(10 * 100, firstSnapshot.TotalValue); // 10 units * 100 price

            // Check last day
            var lastSnapshot = snapshots.Last();
            Assert.Equal(startDate.AddDays(10), lastSnapshot.Date);
            Assert.Equal(10 * 110, lastSnapshot.TotalValue); // 10 units * 110 price
        }
    }
}
