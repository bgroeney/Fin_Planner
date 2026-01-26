using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IStressTestEngine
    {
        Task<StressTestResult> RunSimulationAsync(Guid portfolioId, StressTestParameters parameters);
    }

    public class StressTestParameters
    {
        public string ScenarioType { get; set; } = "MonteCarlo"; // "MonteCarlo", "Historical"
        public int Iterations { get; set; } = 5000;
        public int Years { get; set; } = 30;
        public decimal InitialBalance { get; set; }
        public decimal AnnualContribution { get; set; }
        public decimal ExpectedReturn { get; set; } = 0.07m;
        public decimal Volatility { get; set; } = 0.15m;
        public decimal InflationRate { get; set; } = 0.025m;
        public decimal TargetIncome { get; set; }
        public int RetirementYear { get; set; } = 10;
        public bool IncludeAgePension { get; set; } = true;
    }

    public class StressTestResult
    {
        public Guid PortfolioId { get; set; }
        public double MedianOutcome { get; set; }
        public double WorstCaseOutcome { get; set; }
        public double BestCaseOutcome { get; set; }
        public double SuccessProbability { get; set; } // 0 to 1
        public List<List<double>> SamplePathData { get; set; } = new(); // A few paths for visualization
        public List<double> MedianPath { get; set; } = new();
        public List<double> P10Path { get; set; } = new();
        public List<double> P90Path { get; set; } = new();
    }
}
