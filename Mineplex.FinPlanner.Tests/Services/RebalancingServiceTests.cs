using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Moq;
using Xunit;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class RebalancingServiceTests
    {
        private readonly FinPlannerDbContext _context;
        private readonly Mock<IAuditService> _auditMock;
        private readonly RebalancingService _service;

        public RebalancingServiceTests()
        {
            var options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new FinPlannerDbContext(options);
            _auditMock = new Mock<IAuditService>();
            _service = new RebalancingService(_context, _auditMock.Object);
        }

        [Fact]
        public async Task GetRebalancingReportAsync_EmptyPortfolio_ReturnsEmptyCategories()
        {
            // Arrange
            var portfolioId = Guid.NewGuid();

            // Act
            var result = await _service.GetRebalancingReportAsync(portfolioId);

            // Assert
            result.Should().NotBeNull();
            result.PortfolioId.Should().Be(portfolioId);
            result.TotalValue.Should().Be(0);
            result.Categories.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRebalancingReportAsync_SingleCategory_CalculatesCorrectly()
        {
            // Arrange
            var portfolioId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var assetId = Guid.NewGuid();

            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Name = "Test Portfolio",
                OwnerId = Guid.NewGuid(),
                TargetAllocation = "{}"
            };
            var account = new Account
            {
                Id = accountId,
                PortfolioId = portfolioId,
                AccountNumber = "ACC001",
                AccountName = "Test Account",
                Provider = "Netwealth",
                ProductType = "Wrap"
            };
            var category = new AssetCategory
            {
                Id = categoryId,
                PortfolioId = portfolioId,
                Name = "AU Shares",
                Code = "AU_LRG",
                TargetPercentage = 100,
                DisplayOrder = 1
            };
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "VAS",
                Name = "Vanguard AU Shares",
                AssetType = "ETF",
                CurrentPrice = new CurrentPrice { Price = 100, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };
            var holding = new Holding
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Account = account,
                AssetId = assetId,
                Asset = asset,
                CategoryId = categoryId,
                Units = 10,
                AvgCost = 90
            };

            await _context.Portfolios.AddAsync(portfolio);
            await _context.Accounts.AddAsync(account);
            await _context.AssetCategories.AddAsync(category);
            await _context.Assets.AddAsync(asset);
            await _context.Holdings.AddAsync(holding);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRebalancingReportAsync(portfolioId);

            // Assert
            result.TotalValue.Should().Be(1000); // 10 units * $100
            result.Categories.Should().HaveCount(1);

            var cat = result.Categories.First();
            cat.CategoryName.Should().Be("AU Shares");
            cat.TargetPercentage.Should().Be(100);
            cat.CurrentPercentage.Should().Be(100);
            cat.CurrentValue.Should().Be(1000);
            cat.TargetValue.Should().Be(1000);
            cat.VarianceAmount.Should().Be(0);
            cat.Recommendation.Should().Be("Hold");
        }

        [Fact]
        public async Task GetRebalancingReportAsync_Overweight_RecommendsSell()
        {
            // Arrange
            var portfolioId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var assetId = Guid.NewGuid();

            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Name = "Test Portfolio",
                OwnerId = Guid.NewGuid(),
                TargetAllocation = "{}"
            };
            var account = new Account
            {
                Id = accountId,
                PortfolioId = portfolioId,
                AccountNumber = "ACC001",
                AccountName = "Test Account",
                Provider = "Netwealth",
                ProductType = "Wrap"
            };
            // Target is 50%, but we'll have 100% in this category
            var category = new AssetCategory
            {
                Id = categoryId,
                PortfolioId = portfolioId,
                Name = "AU Shares",
                Code = "AU_LRG",
                TargetPercentage = 50,
                DisplayOrder = 1
            };
            var asset = new Asset
            {
                Id = assetId,
                Symbol = "VAS",
                Name = "Vanguard AU Shares",
                AssetType = "ETF",
                CurrentPrice = new CurrentPrice { Price = 100, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };
            var holding = new Holding
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Account = account,
                AssetId = assetId,
                Asset = asset,
                CategoryId = categoryId,
                Units = 10,
                AvgCost = 90
            };

            await _context.Portfolios.AddAsync(portfolio);
            await _context.Accounts.AddAsync(account);
            await _context.AssetCategories.AddAsync(category);
            await _context.Assets.AddAsync(asset);
            await _context.Holdings.AddAsync(holding);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRebalancingReportAsync(portfolioId);

            // Assert
            var cat = result.Categories.First();
            cat.CurrentPercentage.Should().Be(100);
            cat.TargetPercentage.Should().Be(50);
            cat.VarianceAmount.Should().BePositive(); // Overweight
            cat.Recommendation.Should().Be("Sell");
        }

        [Fact]
        public async Task GetRebalancingReportAsync_Underweight_RecommendsBuy()
        {
            // Arrange
            var portfolioId = Guid.NewGuid();
            var category1Id = Guid.NewGuid();
            var category2Id = Guid.NewGuid();
            var accountId = Guid.NewGuid();
            var assetId = Guid.NewGuid();

            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Name = "Test Portfolio",
                OwnerId = Guid.NewGuid(),
                TargetAllocation = "{}"
            };
            var account = new Account
            {
                Id = accountId,
                PortfolioId = portfolioId,
                AccountNumber = "ACC001",
                AccountName = "Test Account",
                Provider = "Netwealth",
                ProductType = "Wrap"
            };

            // Category 1 with 100% holdings but only 50% target -> overweight
            var category1 = new AssetCategory
            {
                Id = category1Id,
                PortfolioId = portfolioId,
                Name = "AU Shares",
                Code = "AU_LRG",
                TargetPercentage = 50,
                DisplayOrder = 1
            };
            // Category 2 with 0% holdings but 50% target -> underweight
            var category2 = new AssetCategory
            {
                Id = category2Id,
                PortfolioId = portfolioId,
                Name = "International",
                Code = "INTL",
                TargetPercentage = 50,
                DisplayOrder = 2
            };

            var asset = new Asset
            {
                Id = assetId,
                Symbol = "VAS",
                Name = "Vanguard AU Shares",
                AssetType = "ETF",
                CurrentPrice = new CurrentPrice { Price = 100, LastUpdated = DateTime.UtcNow, SourceUsed = "Test" }
            };
            var holding = new Holding
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Account = account,
                AssetId = assetId,
                Asset = asset,
                CategoryId = category1Id,
                Units = 10,
                AvgCost = 90
            };

            await _context.Portfolios.AddAsync(portfolio);
            await _context.Accounts.AddAsync(account);
            await _context.AssetCategories.AddRangeAsync(category1, category2);
            await _context.Assets.AddAsync(asset);
            await _context.Holdings.AddAsync(holding);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetRebalancingReportAsync(portfolioId);

            // Assert
            result.Categories.Should().HaveCount(2);

            var internationalCat = result.Categories.First(c => c.CategoryName == "International");
            internationalCat.CurrentValue.Should().Be(0);
            internationalCat.TargetValue.Should().Be(500); // 50% of $1000
            internationalCat.VarianceAmount.Should().BeNegative(); // Underweight
            internationalCat.Recommendation.Should().Be("Buy");
        }
    }
}
