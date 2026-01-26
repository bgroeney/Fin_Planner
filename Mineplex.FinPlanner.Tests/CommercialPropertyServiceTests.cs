using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Moq;
using Xunit;

namespace Mineplex.FinPlanner.Tests
{
    public class CommercialPropertyServiceTests
    {
        private readonly DbContextOptions<FinPlannerDbContext> _options;

        public CommercialPropertyServiceTests()
        {
            _options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private FinPlannerDbContext GetContext()
        {
            return new FinPlannerDbContext(_options);
        }

        [Fact]
        public async Task GetDashboardSummaryAsync_ReturnsCorrectSummary_ForMultipleProperties()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var property1Id = Guid.NewGuid();
            var property2Id = Guid.NewGuid();

            using (var context = GetContext())
            {
                // Property 1
                var prop1 = new CommercialProperty
                {
                    Id = property1Id,
                    OwnerId = userId,
                    Address = "123 Main St",
                    PurchasePrice = 1000000,
                    PurchaseDate = DateTime.UtcNow.AddYears(-2),
                    BuildingType = "Office"
                };

                // Add valuation for Prop 1
                context.PropertyValuations.Add(new PropertyValuation
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property1Id,
                    Date = DateTime.UtcNow,
                    Value = 1200000,
                    Source = "Internal",
                    CreatedAt = DateTime.UtcNow
                });

                // Add Lease for Prop 1
                context.LeaseProfiles.Add(new LeaseProfile
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property1Id,
                    TenantName = "Tenant A",
                    LeaseStart = DateTime.UtcNow.AddYears(-1),
                    LeaseEnd = DateTime.UtcNow.AddYears(2),
                    CurrentRent = 60000,
                    ReviewType = "Fixed",
                    IsActive = true
                });

                // Add Income/Expense for Prop 1
                context.PropertyLedgerEntries.Add(new PropertyLedger
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property1Id,
                    Date = DateTime.UtcNow.AddMonths(-1),
                    IsIncome = true,
                    Type = "Rent",
                    Amount = 5000, // Monthly rent
                    CreatedAt = DateTime.UtcNow
                });

                context.PropertyLedgerEntries.Add(new PropertyLedger
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property1Id,
                    Date = DateTime.UtcNow.AddMonths(-1),
                    IsIncome = false,
                    Type = "Maintenance",
                    Amount = 1000, // Expenses
                    CreatedAt = DateTime.UtcNow
                });

                // Property 2
                var prop2 = new CommercialProperty
                {
                    Id = property2Id,
                    OwnerId = userId,
                    Address = "456 Side St",
                    PurchasePrice = 2000000,
                    PurchaseDate = DateTime.UtcNow.AddYears(-1),
                    BuildingType = "Industrial"
                };

                 // Add Lease for Prop 2 (expiring soon)
                context.LeaseProfiles.Add(new LeaseProfile
                {
                    Id = Guid.NewGuid(),
                    PropertyId = property2Id,
                    TenantName = "Tenant B",
                    LeaseStart = DateTime.UtcNow.AddYears(-1),
                    LeaseEnd = DateTime.UtcNow.AddMonths(6),
                    CurrentRent = 120000,
                    ReviewType = "CPI",
                    IsActive = true
                });

                context.CommercialProperties.AddRange(prop1, prop2);
                await context.SaveChangesAsync();
            }

            // Act
            PropertyDashboardSummary result;
            using (var context = GetContext())
            {
                var service = new CommercialPropertyService(context);
                result = await service.GetDashboardSummaryAsync(userId);
            }

            // Assert
            Assert.Equal(2, result.TotalProperties);
            Assert.Equal(3200000, result.TotalValue); // 1.2M + 2.0M (no valuation for prop 2, uses purchase price)
            Assert.Equal(3000000, result.TotalPurchasePrice); // 1M + 2M
            Assert.Equal(2, result.ActiveLeases);
            Assert.Equal(1, result.ExpiringLeases); // Tenant B expires in 6 months

            // 5000 income, 1000 expense from ledger entries (within last 1 year)
            Assert.Equal(5000, result.GrossRentalIncome);
            Assert.Equal(1000, result.TotalExpenses);
            Assert.Equal(4000, result.NetIncome);

            // Check cashflow forecast
            Assert.NotNull(result.CashflowForecast);
            Assert.Equal(12, result.CashflowForecast.Count);

            var firstMonth = result.CashflowForecast.First();
            // Projected Income: Prop 1 (5k/mo) + Prop 2 (10k/mo) = 15k/mo
            Assert.Equal(15000, firstMonth.ProjectedIncome);
        }
    }
}
