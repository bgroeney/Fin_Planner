using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models.Retirement;
using Mineplex.FinPlanner.Api.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RetirementController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;
        private readonly IStressTestEngine _stressTestEngine;

        public RetirementController(FinPlannerDbContext context, IStressTestEngine stressTestEngine)
        {
            _context = context;
            _stressTestEngine = stressTestEngine;
        }

        [HttpGet("scenarios")]
        public async Task<ActionResult<IEnumerable<RetirementScenario>>> GetScenarios(Guid portfolioId)
        {
            return await _context.RetirementScenarios
                .Include(s => s.LifeEvents)
                .Where(s => s.PortfolioId == portfolioId)
                .ToListAsync();
        }

        [HttpPost("scenarios")]
        public async Task<ActionResult<RetirementScenario>> CreateScenario(RetirementScenario scenario)
        {
            _context.RetirementScenarios.Add(scenario);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetScenarios), new { portfolioId = scenario.PortfolioId }, scenario);
        }

        [HttpPost("simulate")]
        public async Task<ActionResult<StressTestResult>> Simulate(Guid scenarioId)
        {
            var scenario = await _context.RetirementScenarios
                .Include(s => s.LifeEvents)
                .FirstOrDefaultAsync(s => s.Id == scenarioId);

            if (scenario == null) return NotFound();

            // Calculate initial balance from portfolio holdings
            var portfolioValue = await _context.Holdings
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .Where(h => h.Account.PortfolioId == scenario.PortfolioId)
                .Select(h => h.Units * (h.Asset.CurrentPrice != null ? h.Asset.CurrentPrice.Price : 0))
                .SumAsync(x => x);

            var parameters = new StressTestParameters
            {
                InitialBalance = portfolioValue,
                AnnualContribution = 0, // Should come from PayrollIncome in Phase 2
                ExpectedReturn = scenario.ExpectedReturnRate,
                Volatility = scenario.Volatility,
                InflationRate = scenario.ExpectedInflationRate,
                TargetIncome = scenario.TargetAnnualIncome,
                Years = 40, // Default projection length
                RetirementYear = scenario.RetirementAge - 45, // Assuming current age 45 for now
                IncludeAgePension = scenario.IncludeAgePension
            };

            var result = await _stressTestEngine.RunSimulationAsync(scenario.PortfolioId, parameters);
            return Ok(result);
        }
    }
}
