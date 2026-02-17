using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Moq;
using Xunit;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class TaxOptimizationServiceTests
    {
        private readonly FinPlannerDbContext _context;
        private readonly Mock<ILogger<TaxOptimizationService>> _loggerMock;
        private readonly TaxOptimizationService _service;

        public TaxOptimizationServiceTests()
        {
            var options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FinPlannerDbContext(options);
            _loggerMock = new Mock<ILogger<TaxOptimizationService>>();
            _service = new TaxOptimizationService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task CalculateTaxImpactAsync_FIFO_ShouldSellOldestParcelsFirst()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var currentPrice = 100m;
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "TEST",
                Name = "Test Asset",
                AssetType = "Share",
                CurrentPrice = new CurrentPrice { Price = currentPrice, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };

            var parcel1 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddYears(-2), // Oldest
                CostBase = 50,
                RemainingUnits = 100,
                Units = 100
            };

            var parcel2 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddMonths(-6), // Newest
                CostBase = 80,
                RemainingUnits = 100,
                Units = 100
            };

            await _context.Assets.AddAsync(asset);
            await _context.TaxParcels.AddRangeAsync(parcel1, parcel2);
            await _context.SaveChangesAsync();

            // Act
            // Sell 50 units. FIFO should take from Parcel 1 (Cost 50).
            // Gain per unit = 100 - 50 = 50.
            // Held > 1yr (2 years), so 50% discount applies.
            // Total Gain = 50 * 50 = 2500.
            // Discounted Gain = 1250.
            var result = await _service.CalculateTaxImpactAsync(assetId, 50, "FIFO");

            // Assert
            result.Should().Be(1250);
        }

        [Fact]
        public async Task CalculateTaxImpactAsync_MaxGain_ShouldSellLowestCostBaseFirst()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var currentPrice = 100m;
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "TEST",
                Name = "Test Asset",
                AssetType = "Share",
                CurrentPrice = new CurrentPrice { Price = currentPrice, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };

            // Parcel 1: High Cost (Low Gain)
            var parcel1 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddMonths(-1),
                CostBase = 90,
                RemainingUnits = 100,
                Units = 100
            };

            // Parcel 2: Low Cost (High Gain) - Should be picked for MaxGain
            var parcel2 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddMonths(-1),
                CostBase = 10,
                RemainingUnits = 100,
                Units = 100
            };

            await _context.Assets.AddAsync(asset);
            await _context.TaxParcels.AddRangeAsync(parcel1, parcel2);
            await _context.SaveChangesAsync();

            // Act
            // Sell 10 units. MaxGain picks Parcel 2 (Cost 10).
            // Gain = (100 - 10) * 10 = 900.
            // Short term hold (<1yr), so NO discount.
            // Taxable = 900.
            var result = await _service.CalculateTaxImpactAsync(assetId, 10, "MAXGAIN");

            // Assert
            result.Should().Be(900);
        }

        [Fact]
        public async Task CalculateTaxImpactAsync_MinTax_ShouldPickSegmentsToMinimizeTax()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var currentPrice = 100m;
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "TEST",
                Name = "Test Asset",
                AssetType = "Share",
                CurrentPrice = new CurrentPrice { Price = currentPrice, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };

            // Parcel 1: High Gain (Cost 10), Long term (50% discount) -> Taxable Gain/Unit = (90 * 0.5) = 45
            var parcel1 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddYears(-2),
                CostBase = 10,
                RemainingUnits = 100,
                Units = 100
            };

            // Parcel 2: Low Gain (Cost 90), Short term (0% discount) -> Taxable Gain/Unit = 10
            var parcel2 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddMonths(-1),
                CostBase = 90,
                RemainingUnits = 100,
                Units = 100
            };

            await _context.Assets.AddAsync(asset);
            await _context.TaxParcels.AddRangeAsync(parcel1, parcel2);
            await _context.SaveChangesAsync();

            // Act
            // Sell 10 units. MinTax should pick Parcel 2 because Taxable Gain 10 < 45.
            // Total Taxable = 10 * 10 = 100.
            var result = await _service.CalculateTaxImpactAsync(assetId, 10, "MINTAX");

            // Assert
            result.Should().Be(100);
        }

        [Fact]
        public async Task CalculateTaxImpactAsync_ShouldHandleSplitParcels()
        {
            // Arrange
            var assetId = Guid.NewGuid();
            var currentPrice = 100m;
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "TEST",
                Name = "Test Asset",
                AssetType = "Share",
                CurrentPrice = new CurrentPrice { Price = currentPrice, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };

            var parcel1 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddYears(-2),
                CostBase = 50,
                RemainingUnits = 10, // Only 10 units here
                Units = 10
            };

            var parcel2 = new TaxParcel
            {
                Id = Guid.NewGuid(),
                AssetId = assetId,
                Asset = asset,
                AcquisitionDate = DateTime.UtcNow.AddYears(-3),
                CostBase = 50,
                RemainingUnits = 20,
                Units = 20
            };

            await _context.Assets.AddAsync(asset);
            await _context.TaxParcels.AddRangeAsync(parcel1, parcel2);
            await _context.SaveChangesAsync();

            // Act
            // Sell 15 units via FIFO.
            // FIFO Order: Parcel 2 (Year -3) -> Parcel 1 (Year -2)
            // Take all 20 from Parcel 2? No, only need 15.
            // So Parcel 2 provides 15 units.
            // Held > 1yr. Cost 50. Gain = 50. Discounted = 25.
            // Total = 15 * 25 = 375.
            var result = await _service.CalculateTaxImpactAsync(assetId, 15, "FIFO");

            // Assert
            result.Should().Be(375);
        }
    }
}
