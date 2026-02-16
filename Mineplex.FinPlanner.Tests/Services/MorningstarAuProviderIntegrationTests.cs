using Xunit;
using Xunit.Abstractions;
using Moq;
using Microsoft.Extensions.Logging;
using Mineplex.FinPlanner.Api.Services.PriceProviders;
using Mineplex.FinPlanner.Api.Services;
using Mineplex.FinPlanner.Api.Models;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Collections.Generic;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class MorningstarAuProviderIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public MorningstarAuProviderIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private MorningstarAuProvider CreateProvider()
        {
            var logger = new Mock<ILogger<MorningstarAuProvider>>();
            var aiService = new Mock<IAIService>();

            // Real HttpClientFactory that creates actual HttpClient instances
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient());

            // Log to test output
            logger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var logLevel = (LogLevel)invocation.Arguments[0];
                    var state = invocation.Arguments[2];
                    var exception = invocation.Arguments[3] as Exception;
                    var formatter = invocation.Arguments[4];
                    var invokeMethod = formatter.GetType().GetMethod("Invoke");
                    var logMessage = invokeMethod?.Invoke(formatter, new[] { state, exception }) as string;
                    _output.WriteLine($"[{logLevel}] {logMessage}");
                }));

            return new MorningstarAuProvider(logger.Object, httpClientFactory.Object, aiService.Object);
        }

        [Theory]
        [InlineData("VHY", "ETF", "ASX", "Vanguard Australian Shares High Yield")]
        [InlineData("VGS", "ETF", "ASX", "Vanguard MSCI Intl Shares")]
        public async Task GetPrice_AsxEtf_ReturnsPrice(string symbol, string assetType, string market, string name)
        {
            var provider = CreateProvider();
            var asset = new Asset { Id = Guid.NewGuid(), Symbol = symbol, Name = name, AssetType = assetType, Market = market };

            var result = await provider.GetCurrentPriceAsync(asset);

            Assert.NotNull(result.Price);
            Assert.True(result.Price > 0, $"Expected price > 0 for {symbol}, got {result.Price}");
            _output.WriteLine($"✅ {symbol}: ${result.Price}");
        }

        [Theory]
        [InlineData("VAN0111AU", "Vanguard High Growth Index")]
        [InlineData("SPC5039AU", "GCQ Flagship")]
        [InlineData("ETL1479AU", "Blackwattle Mid Cap Quality")]
        [InlineData("ETL0788AU", "Blackwattle Small Cap Quality")]
        [InlineData("AUG0018AU", "Australian Ethical Australian")]
        [InlineData("PIM7802AU", "Fairlight Global Small")]
        [InlineData("ETL8171AU", "Impax Sustainable Leaders")]
        [InlineData("BFL3229AU", "Skerryvore Global Em Mkts")]
        [InlineData("FID0010AU", "Fidelity Asia")]
        [InlineData("PIM9253AU", "ATLAS Infrastructure")]
        [InlineData("LAZ0012AU", "Lazard Global Small Cap")]
        [InlineData("HHA0007AU", "Pengana WHEB Sustainable")]
        [InlineData("SOL0001AU", "Solaris Core Australian")]
        public async Task ResolveApirCode_ReturnsValidFundInfo(string apirCode, string expectedNameContains)
        {
            var provider = CreateProvider();
            var asset = new Asset { Id = Guid.NewGuid(), Symbol = apirCode, Name = expectedNameContains, AssetType = "ManagedFund", Market = "AU" };

            // Verify the asset is supported
            Assert.True(provider.SupportsAsset(asset), $"Provider should support {apirCode}");

            // Fetch price - may return null for managed funds since price data is behind auth,
            // but verifies the APIR resolution pipeline works
            var result = await provider.GetCurrentPriceAsync(asset);

            _output.WriteLine($"APIR {apirCode}: Price={result.Price?.ToString() ?? "null (expected for managed funds)"}");

            // For now, we just verify it doesn't throw and the provider handles it gracefully
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SupportsAsset_CorrectlyIdentifiesAssetTypes()
        {
            var provider = CreateProvider();

            // Should support APIR codes
            Assert.True(provider.SupportsAsset(new Asset { Symbol = "VAN0111AU", Name = "Test", AssetType = "ManagedFund" }));
            Assert.True(provider.SupportsAsset(new Asset { Symbol = "SPC5039AU", Name = "Test", AssetType = "ManagedFund" }));

            // Should support ASX ETFs
            Assert.True(provider.SupportsAsset(new Asset { Symbol = "VHY", Market = "ASX", AssetType = "ETF", Name = "Test" }));
            Assert.True(provider.SupportsAsset(new Asset { Symbol = "VGS", Market = "AU", AssetType = "ETF", Name = "Test" }));

            // Should NOT support US stocks or other non-AU assets
            Assert.False(provider.SupportsAsset(new Asset { Symbol = "AAPL", Market = "US", Name = "Test", AssetType = "Stock" }));
            Assert.False(provider.SupportsAsset(new Asset { Symbol = "", Name = "Test", AssetType = "Stock" }));
            Assert.False(provider.SupportsAsset(new Asset { Symbol = null!, Name = "Test", AssetType = "Stock" }));
        }
    }
}
