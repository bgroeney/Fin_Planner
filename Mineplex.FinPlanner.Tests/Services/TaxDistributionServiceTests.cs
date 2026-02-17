using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Services;
using Moq;
using Xunit;

namespace Mineplex.FinPlanner.Tests.Services
{
    /// <summary>
    /// Tests for TaxDistributionService - calculates optimal trust distributions
    /// </summary>
    public class TaxDistributionServiceTests
    {
        [Theory]
        [InlineData(0, 0)] // Tax-free threshold
        [InlineData(18200, 0)] // At tax-free threshold
        [InlineData(18201, 0.16)] // Just above threshold (16% bracket)
        [InlineData(45000, 0.16)] // Top of 16% bracket
        [InlineData(45001, 0.30)] // 30% bracket
        [InlineData(135000, 0.30)] // Top of 30% bracket
        [InlineData(135001, 0.37)] // 37% bracket
        [InlineData(190000, 0.37)] // Top of 37% bracket
        [InlineData(190001, 0.45)] // 45% top bracket
        public void CalculateMarginalTaxRate_ShouldReturnCorrectRateForBracket(decimal income, decimal expectedBaseMarginalRate)
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.CalculateMarginalTaxRate(income);

            // Assert
            // Medicare levy adds 2% if above threshold (~$24,276)
            var medicareLevy = income >= 24276m ? 0.02m : 0;
            result.Should().Be(expectedBaseMarginalRate + medicareLevy);
        }

        [Fact]
        public void CalculateTaxPayable_ZeroIncome_ReturnsZero()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.CalculateTaxPayable(0);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateTaxPayable_BelowTaxFreeThreshold_ReturnsZero()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.CalculateTaxPayable(18000);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateTaxPayable_AtExactThreshold_ReturnsZero()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.CalculateTaxPayable(18200);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateTaxPayable_In16PercentBracket_CalculatesCorrectly()
        {
            // Arrange
            var service = CreateService();
            // $30,000 income: 
            // First $18,200 = $0
            // $30,000 - $18,200 = $11,800 @ 16% = $1,888
            // Medicare Levy: $30,000 * 2% = $600
            // Total = $2,488

            // Act
            var result = service.CalculateTaxPayable(30000);

            // Assert
            result.Should().Be(2488);
        }

        [Fact]
        public void CalculateTaxPayable_HighIncome_IncludesAllBrackets()
        {
            // Arrange
            var service = CreateService();
            // $200,000 income:
            // $0-$18,200 @ 0% = $0
            // $18,201-$45,000 @ 16% = $4,288
            // $45,001-$135,000 @ 30% = $27,000
            // $135,001-$190,000 @ 37% = $20,350
            // $190,001-$200,000 @ 45% = $4,500 (approx)
            // Medicare = $200,000 * 2% = $4,000

            // Act
            var result = service.CalculateTaxPayable(200000);

            // Assert
            // Detailed:
            // 16%: (45000-18200) * 0.16 = 4288
            // 30%: (135000-45000) * 0.30 = 27000
            // 37%: (190000-135000) * 0.37 = 20350
            // 45%: (200000-190000) * 0.45 = 4500
            // Subtotal: 56138
            // Medicare: 200000 * 0.02 = 4000
            // Total: 60138
            result.Should().Be(60138);
        }

        [Fact]
        public void CalculateTaxPayable_NegativeIncome_ReturnsZero()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = service.CalculateTaxPayable(-10000);

            // Assert
            result.Should().Be(0);
        }

        private TaxDistributionService CreateService()
        {
            // We can use reflection or create a testable wrapper since these are public methods
            // For simplicity, using Moq to create a minimal instance
            var contextMock = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Mineplex.FinPlanner.Api.Data.FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new Mineplex.FinPlanner.Api.Data.FinPlannerDbContext(contextMock);
            var loggerMock = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<TaxDistributionService>>();

            return new TaxDistributionService(context, loggerMock.Object);
        }
    }
}
