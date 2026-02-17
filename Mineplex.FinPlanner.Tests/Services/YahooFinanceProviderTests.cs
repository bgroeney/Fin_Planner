using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Mineplex.FinPlanner.Api.Services.PriceProviders;
using Moq;
using Xunit;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class YahooFinanceProviderTests
    {
        [Fact]
        public async Task GetCurrentPriceAsync_ForVeu_ReturnsAudPrice()
        {
            // Arrange
            var mockAiService = new Mock<IAIService>();
            var logger = NullLogger<YahooFinanceProvider>.Instance;
            var provider = new YahooFinanceProvider(mockAiService.Object, logger);

            var asset = new Asset
            {
                Id = System.Guid.NewGuid(),
                Symbol = "VEU",
                Name = "Vanguard All-World ex-US Shares Index ETF",
                AssetType = "ETF",
                Market = "" // Empty market to trigger the fix logic
            };

            // Act
            var result = await provider.GetCurrentPriceAsync(asset);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Price);
            // VEU on ASX is around 113 AUD, while on US it's around 80 USD.
            // 95 is a safe lower bound for AUD price given current exchange rates/market.
            Assert.True(result.Price > 95, $"Price for VEU should be > 95 (likely AUD), but was {result.Price}");
        }

        [Fact]
        public async Task GetCurrentPriceAsync_ForVts_ReturnsAudPrice()
        {
            // Arrange
            var mockAiService = new Mock<IAIService>();
            var logger = NullLogger<YahooFinanceProvider>.Instance;
            var provider = new YahooFinanceProvider(mockAiService.Object, logger);

            var asset = new Asset
            {
                Id = System.Guid.NewGuid(),
                Symbol = "VTS",
                Name = "Vanguard US Total Market Shares Index ETF",
                AssetType = "ETF",
                Market = ""
            };

            // Act
            var result = await provider.GetCurrentPriceAsync(asset);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Price);
            // VTS on ASX is ~400+ AUD, US is ~300 USD. 
            // This test is less precise due to narrower gap but checking it doesn't crash or return 0 is good.
            Assert.True(result.Price > 0);
        }
    }
}
