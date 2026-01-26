using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mineplex.FinPlanner.Api.Services
{
    public class MonteCarloService : IStressTestEngine
    {
        private static readonly Random _random = new();

        public async Task<StressTestResult> RunSimulationAsync(Guid portfolioId, StressTestParameters parameters)
        {
            return await Task.Run(() =>
            {
                var iterations = parameters.Iterations;
                var years = parameters.Years;
                var results = new List<double[]>(iterations);
                int successfulTrials = 0;

                for (int i = 0; i < iterations; i++)
                {
                    var path = new double[years + 1];
                    path[0] = (double)parameters.InitialBalance;
                    bool failed = false;

                    for (int year = 1; year <= years; year++)
                    {
                        // Generate random return using Box-Muller transform for normal distribution
                        double u1 = 1.0 - _random.NextDouble();
                        double u2 = 1.0 - _random.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                        double annualReturn = (double)parameters.ExpectedReturn + (double)parameters.Volatility * randStdNormal;

                        double balance = path[year - 1] * (1 + annualReturn);

                        // Accumulation phase
                        if (year <= parameters.RetirementYear)
                        {
                            balance += (double)parameters.AnnualContribution;
                        }
                        // Retirement phase (drawdown)
                        else
                        {
                            // Adjust target income for inflation
                            double inflationAdjustedIncome = (double)parameters.TargetIncome * Math.Pow(1 + (double)parameters.InflationRate, year);

                            // Age Pension estimation (very simplified for now)
                            double pension = 0;
                            if (parameters.IncludeAgePension && balance < 1000000) // Simple threshold
                            {
                                pension = 25000 * Math.Pow(1 + (double)parameters.InflationRate, year);
                            }

                            balance -= Math.Max(0, inflationAdjustedIncome - pension);
                        }

                        path[year] = Math.Max(0, balance);
                        if (path[year] <= 0) failed = true;
                    }

                    results.Add(path);
                    if (!failed) successfulTrials++;
                }

                // Calculate percentiles
                var finalValues = results.Select(r => r[years]).OrderBy(v => v).ToList();
                var medianOutcome = finalValues[iterations / 2];
                var worstCase = finalValues[0];
                var bestCase = finalValues[iterations - 1];

                // Calculate mean path data
                var medianPath = new List<double>();
                var p10Path = new List<double>();
                var p90Path = new List<double>();

                for (int year = 0; year <= years; year++)
                {
                    var yearValues = results.Select(r => r[year]).OrderBy(v => v).ToList();
                    medianPath.Add(yearValues[iterations / 2]);
                    p10Path.Add(yearValues[iterations / 10]);
                    p90Path.Add(yearValues[iterations * 9 / 10]);
                }

                // Sample paths for visualization (e.g., first 5 paths)
                var samplePaths = results.Take(5).Select(p => p.ToList()).ToList();

                return new StressTestResult
                {
                    PortfolioId = portfolioId,
                    MedianOutcome = medianOutcome,
                    WorstCaseOutcome = worstCase,
                    BestCaseOutcome = bestCase,
                    SuccessProbability = (double)successfulTrials / iterations,
                    MedianPath = medianPath,
                    P10Path = p10Path,
                    P90Path = p90Path,
                    SamplePathData = samplePaths
                };
            });
        }
    }
}
