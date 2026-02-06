using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HoldingsController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;
        private readonly Services.PriceSourceManager _priceManager;

        public HoldingsController(FinPlannerDbContext context, Services.PriceSourceManager priceManager)
        {
            _context = context;
            _priceManager = priceManager;
        }

        [HttpGet("account/{accountId}")]
        public async Task<ActionResult<List<Models.Portfolios.HoldingDto>>> GetHoldings(Guid accountId)
        {
            var holdings = await _context.Holdings
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .Include(h => h.Category)
                .Where(h => h.AccountId == accountId)
                .ToListAsync();

            return holdings.Select(h => new Models.Portfolios.HoldingDto
            {
                Id = h.Id,
                AccountId = h.AccountId,
                AssetId = h.AssetId,
                CategoryId = h.CategoryId,
                CategoryName = h.Category?.Name,
                Symbol = h.Asset.Symbol,
                Name = h.Asset.Name,
                Units = h.Units,
                AvgCost = h.AvgCost,
                CurrentPrice = h.Asset.CurrentPrice?.Price ?? 0,
                CurrentValue = h.Units * (h.Asset.CurrentPrice?.Price ?? 0)
            }).ToList();
        }

        [HttpGet("portfolio/{portfolioId}")]
        public async Task<ActionResult<List<Models.Portfolios.HoldingDto>>> GetPortfolioHoldings(Guid portfolioId)
        {
            // Optimized: Fetch holdings directly via navigation property in a single query with AsNoTracking
            var holdings = await _context.Holdings
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .Include(h => h.Category)
                .Where(h => h.Account.PortfolioId == portfolioId)
                .AsNoTracking()
                .ToListAsync();

            return holdings.Select(h => new Models.Portfolios.HoldingDto
            {
                Id = h.Id,
                AccountId = h.AccountId,
                AssetId = h.AssetId,
                CategoryId = h.CategoryId,
                CategoryName = h.Category?.Name,
                Symbol = h.Asset.Symbol,
                Name = h.Asset.Name,
                Units = h.Units,
                AvgCost = h.AvgCost,
                CurrentPrice = h.Asset.CurrentPrice?.Price ?? 0,
                CurrentValue = h.Units * (h.Asset.CurrentPrice?.Price ?? 0)
            }).ToList();
        }

        [HttpPut("{id}/category")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] Models.Portfolios.UpdateHoldingCategoryDto dto)
        {
            var holding = await _context.Holdings.FindAsync(id);
            if (holding == null) return NotFound();

            holding.CategoryId = dto.CategoryId;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
