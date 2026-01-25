using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IStressTestEngine
    {
        Task<StressTestResult> RunSimulationAsync(Guid portfolioId, StressTestParameters parameters);
    }

    public class StressTestParameters
    {
        public string ScenarioType { get; set; } = string.Empty; // "MonteCarlo", "Historical"
        public int Iterations { get; set; }
        public int Years { get; set; }
    }

    public class StressTestResult
    {
        public Guid PortfolioId { get; set; }
        public double MedianOutcome { get; set; }
        public double WorstCaseOutcome { get; set; }
        public double BestCaseOutcome { get; set; }
        public List<double> PercentilePaths { get; set; } = new();
    }
}
