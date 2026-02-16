using FluentAssertions;
using Mineplex.FinPlanner.Api.Services;
using Xunit;

namespace Mineplex.FinPlanner.Tests.Services
{
    public class MonteCarloServiceTests
    {
        private readonly MonteCarloService _service;

        public MonteCarloServiceTests()
        {
            _service = new MonteCarloService();
        }

        [Fact]
        public async Task RunSimulationAsync_NoVolatility_ShouldBeDeterministic()
        {
            // Arrange
            var parameters = new StressTestParameters
            {
                Iterations = 100,
                Years = 10,
                InitialBalance = 100000,
                AnnualContribution = 0,
                ExpectedReturn = 0.05m,
                Volatility = 0, // Deterministic
                InflationRate = 0,
                RetirementYear = 20, // All accumulation
                IncludeAgePension = false,
                ScenarioType = "MonteCarlo",
                TargetIncome = 0
            };

            // Act
            var result = await _service.RunSimulationAsync(Guid.NewGuid(), parameters);

            // Assert
            // Balance = 100k * (1.05)^10 = 162889.46
            var expectedBalance = 100000 * Math.Pow(1.05, 10);

            result.MedianOutcome.Should().BeApproximately(expectedBalance, 1.0);
            result.WorstCaseOutcome.Should().BeApproximately(expectedBalance, 1.0);
            result.BestCaseOutcome.Should().BeApproximately(expectedBalance, 1.0);
            result.SuccessProbability.Should().Be(1.0, "positive balance is success");
        }

        [Fact]
        public async Task RunSimulationAsync_HighVolatility_ShouldHaveWideRange()
        {
            // Arrange
            var parameters = new StressTestParameters
            {
                Iterations = 1000,
                Years = 10,
                InitialBalance = 100000,
                AnnualContribution = 0,
                ExpectedReturn = 0.07m,
                Volatility = 0.5m, // High volatility
                RetirementYear = 20,
                InflationRate = 0.025m,
                TargetIncome = 0,
                IncludeAgePension = false
            };

            // Act
            var result = await _service.RunSimulationAsync(Guid.NewGuid(), parameters);

            // Assert
            result.BestCaseOutcome.Should().BeGreaterThan(result.MedianOutcome);
            result.WorstCaseOutcome.Should().BeLessThan(result.MedianOutcome);
        }

        [Fact]
        public async Task RunSimulationAsync_Drawdown_ShouldReduceBalance()
        {
            // Arrange
            var parameters = new StressTestParameters
            {
                Iterations = 100,
                Years = 5,
                InitialBalance = 100001,
                AnnualContribution = 0,
                ExpectedReturn = 0,
                Volatility = 0,
                InflationRate = 0,
                TargetIncome = 20000,
                RetirementYear = 0, // Immediately retired
                IncludeAgePension = false
            };

            // Act
            var result = await _service.RunSimulationAsync(Guid.NewGuid(), parameters);

            // Assert
            // 5 years of 20k drawdown = 100001 - 100000 = 1
            result.MedianOutcome.Should().BeLessThan(100);
            result.MedianOutcome.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task RunSimulationAsync_Failure_ShouldTrackSuccessProbability()
        {
            // Arrange
            var parameters = new StressTestParameters
            {
                Iterations = 100,
                Years = 5,
                InitialBalance = 50000, // Too low
                AnnualContribution = 0,
                ExpectedReturn = 0,
                Volatility = 0,
                InflationRate = 0,
                TargetIncome = 20000, // 20k * 5 = 100k needed
                RetirementYear = 0,
                IncludeAgePension = false
            };

            // Act
            var result = await _service.RunSimulationAsync(Guid.NewGuid(), parameters);

            // Assert
            // Should fail every time
            result.SuccessProbability.Should().Be(0);
        }
    }
}
