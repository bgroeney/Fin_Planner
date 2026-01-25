using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;
        private readonly ITaxOptimizationService _taxService;
        private readonly IPerformanceService _performanceService;

        public ReportsController(FinPlannerDbContext context, ITaxOptimizationService taxService, IPerformanceService performanceService)
        {
            _context = context;
            _taxService = taxService;
            _performanceService = performanceService;
        }

        [HttpGet("performance/{portfolioId}")]
        public async Task<IActionResult> GetPerformance(Guid portfolioId, [FromQuery] string range = "1Y")
        {
            DateTime startDate = DateTime.UtcNow.AddYears(-1);
            if (range == "YTD") startDate = new DateTime(DateTime.UtcNow.Year, 1, 1);
            else if (range == "5Y") startDate = DateTime.UtcNow.AddYears(-5);
            else if (range == "ALL") startDate = DateTime.MinValue;

            var snapshots = await _context.PerformanceSnapshots
                .Where(p => p.PortfolioId == portfolioId && p.Date >= startDate)
                .OrderBy(p => p.Date)
                .Select(p => new { p.Date, p.TotalValue })
                .ToListAsync();

            if (!snapshots.Any())
            {
                // Try rebuild on demand if empty
                await _performanceService.RebuildHistoryAsync(portfolioId);
                snapshots = await _context.PerformanceSnapshots
                    .Where(p => p.PortfolioId == portfolioId && p.Date >= startDate)
                    .OrderBy(p => p.Date)
                    .Select(p => new { p.Date, p.TotalValue })
                    .ToListAsync();
            }

            return Ok(snapshots);
        }

        [HttpGet("tax-summary/{portfolioId}")]
        public async Task<IActionResult> GetTaxSummary(Guid portfolioId, [FromQuery] int fiscalYear = 0)
        {
            if (fiscalYear == 0) fiscalYear = DateTime.UtcNow.Year + (DateTime.UtcNow.Month > 6 ? 1 : 0);

            var start = new DateTime(fiscalYear - 1, 7, 1);
            var end = new DateTime(fiscalYear, 6, 30);

            // Fetch realized gains (Sells)
            // Note: In a real system we'd query the 'RealizedGains' table or compute from TaxParcels history
            // For MVP, we'll placeholder this or simpler computation

            var transactions = await _context.Transactions
                .Include(t => t.Account)
                .Where(t => t.Account.PortfolioId == portfolioId &&
                            t.EffectiveDate >= start && t.EffectiveDate <= end &&
                           (t.Type == TransactionType.Sell || t.Type == TransactionType.Dividend))
                .ToListAsync();

            decimal dividends = transactions.Where(t => t.Type == TransactionType.Dividend).Sum(t => t.Amount);
            decimal realizedGains = 0; // Requires linking Sells to Cost Base. 
            // Since we implemented Parcel logic in TaxOptimizationService, we should expose a method there.
            // For now, let's return what we can easily sum. 

            return Ok(new
            {
                FiscalYear = fiscalYear,
                Dividends = dividends,
                FrankingCredits = dividends * 0.3m / 0.7m, // Approximate
                RealizedGains = realizedGains,
                EstimatedTax = 0 // Needs marginal rate input
            });
        }

        [HttpGet("transactions/{portfolioId}")]
        public async Task<IActionResult> GetTransactions(Guid portfolioId, [FromQuery] string? search, [FromQuery] string? type)
        {
            var query = _context.Transactions
                .Include(t => t.Asset)
                .Include(t => t.Account)
                .Where(t => t.Account.PortfolioId == portfolioId);

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, true, out var txType))
            {
                query = query.Where(t => t.Type == txType);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.Asset.Symbol.Contains(search) || t.Narration.Contains(search));
            }

            var transactions = await query
                .OrderByDescending(t => t.EffectiveDate)
                .Take(100)
                .Select(t => new
                {
                    t.Id,
                    t.EffectiveDate,
                    Type = t.Type.ToString(),
                    AssetCode = t.Asset.Symbol,
                    AssetName = t.Asset.Name,
                    t.Units,
                    t.Amount,
                    t.Narration
                })
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpPost("rebuild-history/{portfolioId}")]
        public async Task<IActionResult> RebuildHistory(Guid portfolioId)
        {
            await _performanceService.RebuildHistoryAsync(portfolioId);
            return Ok();
        }
    }
}
