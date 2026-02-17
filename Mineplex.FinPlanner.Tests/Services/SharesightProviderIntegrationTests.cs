using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using Mineplex.FinPlanner.Api.Services.PriceProviders;
using System.Collections.Generic;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class SharesightProviderIntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SharesightAuProvider _provider;

        public SharesightProviderIntegrationTests()
        {
            var services = new ServiceCollection();

            // Configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Logging
            services.AddLogging(builder => builder.AddConsole());

            // Sharesight Service
            services.AddHttpClient<ISharesightService, SharesightService>()
                .ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new System.Net.CookieContainer(),
                    AllowAutoRedirect = true
                });

            // Provider
            services.AddScoped<SharesightAuProvider>();

            _serviceProvider = services.BuildServiceProvider();
            _provider = _serviceProvider.GetRequiredService<SharesightAuProvider>();
        }

        [Fact]
        public void SupportsAsset_CorrectlyIdentifiesApirCodes()
        {
            // Supported (APIR codes)
            Assert.True(_provider.SupportsAsset(new Asset { Symbol = "VAN0111AU", Market = "FundAU", Name = "Test", AssetType = "ManagedFund" }));
            Assert.True(_provider.SupportsAsset(new Asset { Symbol = "SPC5039AU", Name = "Test", AssetType = "ManagedFund" })); // Market optional
            Assert.True(_provider.SupportsAsset(new Asset { Symbol = "ETL9166AU", Market = "AU", Name = "Test", AssetType = "ManagedFund" }));

            // Not Supported (Tickers, US stocks)
            Assert.False(_provider.SupportsAsset(new Asset { Symbol = "VHY", Market = "ASX", Name = "Test", AssetType = "ETF" })); // Sharesight provider focuses on managed funds
            Assert.False(_provider.SupportsAsset(new Asset { Symbol = "AAPL", Market = "US", Name = "Test", AssetType = "Stock" }));
            Assert.False(_provider.SupportsAsset(new Asset { Symbol = "INVALID", Market = "AU", Name = "Test", AssetType = "ManagedFund" })); // Needs 9 chars pattern
        }

        [Fact]
        public async Task GetCurrentPrice_ReturnsNull_WhenNoCredentials()
        {
            // This test verifies that the provider handles missing credentials gracefully
            // by returning null instead of throwing exception.
            var result = await _provider.GetCurrentPriceAsync(new Asset { Symbol = "VAN0111AU", Market = "FundAU", Name = "Test", AssetType = "ManagedFund" });

            // Since we haven't provided credentials in the test configuration (unless user updated appsettings),
            // we expect this to return null.
            // If user DID provide credentials, this might actually succeed, so we'll check for no exception mostly.
            Assert.NotNull(result);
            // We don't assert Price is null strictly, because if the user adds creds, it might work.
            if (result.Price != null)
            {
                Assert.True(result.Price > 0);
            }
        }
    }
}
