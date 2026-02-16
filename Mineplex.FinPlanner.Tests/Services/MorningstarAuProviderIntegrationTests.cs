using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Mineplex.FinPlanner.Api.Services.PriceProviders;
using Mineplex.FinPlanner.Api.Services;
using Mineplex.FinPlanner.Api.Models;
using System.Threading.Tasks;
using System;
using System.Net.Http; // For IHttpClientFactory

namespace Mineplex.FinPlanner.Tests.Services
{
    public class MorningstarAuProviderIntegrationTests
    {
        [Fact]
        public async Task GetCurrentPriceAsync_ReturnsPrice_ForValidApir()
        {
            // Arrange
            var logger = new Mock<ILogger<MorningstarAuProvider>>();
            var aiService = new Mock<IAIService>();
            var httpClientFactory = new Mock<IHttpClientFactory>();

            // Allow the logger to log to console so we can see Puppeteer progress
            logger.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var logLevel = (LogLevel)invocation.Arguments[0];
                    var state = invocation.Arguments[2];
                    var exception = (Exception)invocation.Arguments[3];
                    var formatter = invocation.Arguments[4];

                    var invokeMethod = formatter.GetType().GetMethod("Invoke");
                    var logMessage = invokeMethod?.Invoke(formatter, new[] { state, exception }) as string;

                    Console.WriteLine($"[{logLevel}] {logMessage}");
                }));

            var provider = new MorningstarAuProvider(logger.Object, httpClientFactory.Object, aiService.Object);
            // PER0011AU is a robust, long-standing fund (Perpetual Industrial Share)
            var asset = new Asset { Id = Guid.NewGuid(), Symbol = "PER0011AU", Name = "Perpetual Industrial Share", AssetType = "ManagedFund", Market = "AU" };

            // Act
            // Use a timeout to fail fast if Puppeteer hangs
            var resultTask = provider.GetCurrentPriceAsync(asset);
            var completedTask = await Task.WhenAny(resultTask, Task.Delay(TimeSpan.FromMinutes(2))); // 2 min timeout for browser download + nav

            if (completedTask != resultTask)
            {
                throw new TimeoutException("The scraping operation timed out.");
            }

            var result = await resultTask;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Price);
            Assert.True(result.Price > 0, $"Expected price > 0, got {result.Price}");

            Console.WriteLine($"Successfully scraped price: {result.Price}");
        }
    }
}
