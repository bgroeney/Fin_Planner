using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly FinPlannerDbContext _context;

        public AIController(IAIService aiService, FinPlannerDbContext context)
        {
            _aiService = aiService;
            _context = context;
        }

        [HttpPost("analyze/holding/{holdingId}")]
        public async Task<ActionResult<AIRecommendation>> AnalyzeHolding(Guid holdingId)
        {
            var holding = await _context.Holdings
                .Include(h => h.Asset)
                .Include(h => h.Category)
                .Include(h => h.Account)
                .FirstOrDefaultAsync(h => h.Id == holdingId);

            if (holding == null) return NotFound("Holding not found");

            // Build simple context
            var portfolioId = holding.Account.PortfolioId;
            var portfolio = await _context.Portfolios.FindAsync(portfolioId);
            var context = $"Portfolio Name: {portfolio?.Name ?? "Unknown"}";

            var recommendation = await _aiService.AnalyzeHoldingAsync(holding, context);

            // Save recommendation to DB
            _context.AIRecommendations.Add(recommendation);
            await _context.SaveChangesAsync();

            return Ok(recommendation);
        }

        [HttpGet("test")]
        public async Task<ActionResult<object>> TestAI()
        {
            var result = await _aiService.TestConnectionAsync();
            return Ok(new { Response = result });
        }
    }
}
