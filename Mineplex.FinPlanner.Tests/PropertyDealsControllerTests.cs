using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Controllers;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Moq;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace Mineplex.FinPlanner.Tests
{
    public class PropertyDealsControllerTests
    {
        private readonly DbContextOptions<FinPlannerDbContext> _options;

        public PropertyDealsControllerTests()
        {
            _options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private FinPlannerDbContext GetContext()
        {
            return new FinPlannerDbContext(_options);
        }

        private PropertyDealsController GetController(FinPlannerDbContext context, Guid userId)
        {
            var controller = new PropertyDealsController(context);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        [Fact]
        public async Task CreateDeal_AddsDealToDatabase()
        {
            // Arrange
            var userId = Guid.NewGuid();
            using var context = GetContext();
            var controller = GetController(context, userId);

            var request = new CreateDealRequest
            {
                Name = "New Deal",
                Address = "123 Test St",
                AskingPrice = 1000000,
                EstimatedValue = 1100000,
                EstimatedGrossRent = 50000,
                OutgoingsEstimate = 5000,
                LoanAmount = 800000,
                CapExReserve = 10000
            };

            // Act
            var result = await controller.CreateDeal(request);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var deal = Assert.IsType<PropertyDeal>(actionResult.Value);
            Assert.Equal("New Deal", deal.Name);
            Assert.Equal(userId, deal.OwnerId);
            Assert.Equal("Draft", deal.Status);

            // Verify in DB
            var dbDeal = await context.PropertyDeals.FindAsync(deal.Id);
            Assert.NotNull(dbDeal);
            Assert.Equal("New Deal", dbDeal.Name);
        }

        [Fact]
        public async Task UpdateDeal_UpdatesFields_AndValidatesDiscountRate()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dealId = Guid.NewGuid();

            using (var context = GetContext())
            {
                context.PropertyDeals.Add(new PropertyDeal
                {
                    Id = dealId,
                    OwnerId = userId,
                    Name = "Old Name",
                    InterestRatePercent = 5.0m,
                    DiscountRate = 6.0m,
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = GetContext())
            {
                var controller = GetController(context, userId);
                var request = new UpdateDealRequest
                {
                    Name = "New Name",
                    DiscountRate = 4.0m // Lower than interest rate 5.0m -> Should fail
                };

                // Act
                var result = await controller.UpdateDeal(dealId, request);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
                Assert.Contains("Discount rate", badRequest.Value.ToString());
            }
        }

        [Fact]
        public async Task SaveSimulation_SavesResult_AndUpdatesStatus()
        {
             // Arrange
            var userId = Guid.NewGuid();
            var dealId = Guid.NewGuid();

            using (var context = GetContext())
            {
                context.PropertyDeals.Add(new PropertyDeal
                {
                    Id = dealId,
                    OwnerId = userId,
                    Name = "Simulation Deal",
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = GetContext())
            {
                var controller = GetController(context, userId);
                var request = new SaveSimulationRequest
                {
                    Iterations = 1000,
                    MedianNPV = 50000,
                    P10NPV = 10000,
                    P90NPV = 90000,
                    MedianIRR = 12.5m,
                    RecommendedDecision = "Buy"
                };

                // Act
                var result = await controller.SaveSimulation(dealId, request);

                // Assert
                var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
                var simResult = Assert.IsType<DealSimulationResult>(actionResult.Value);
                Assert.Equal(50000, simResult.MedianNPV);

                var deal = await context.PropertyDeals.FindAsync(dealId);
                Assert.Equal("Analyzing", deal.Status);

                var dbSim = await context.DealSimulationResults.FirstOrDefaultAsync(s => s.DealId == dealId);
                Assert.NotNull(dbSim);
            }
        }
    }
}
