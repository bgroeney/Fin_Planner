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
        private readonly IMarketDataService _marketDataService;

        public ReportsController(
            FinPlannerDbContext context,
            ITaxOptimizationService taxService,
            IPerformanceService performanceService,
            IMarketDataService marketDataService)
        {
            _context = context;
            _taxService = taxService;
            _performanceService = performanceService;
            _marketDataService = marketDataService;
        }

        [HttpGet("performance/{portfolioId}")]
        public async Task<IActionResult> GetPerformance(Guid portfolioId, [FromQuery] string range = "1Y", [FromQuery] string? benchmark = null)
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

            object? benchmarkData = null;
            if (!string.IsNullOrEmpty(benchmark) && snapshots.Any())
            {
                var firstDate = snapshots.First().Date;
                var lastDate = snapshots.Last().Date;
                benchmarkData = await _marketDataService.GetBenchmarkDataAsync(benchmark, firstDate, lastDate);
            }

            return Ok(new { Portfolio = snapshots, Benchmark = benchmarkData });
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

        /// <summary>
        /// Export all portfolio data as CSV or JSON
        /// </summary>
        [HttpGet("export/{portfolioId}")]
        public async Task<IActionResult> ExportData(Guid portfolioId, [FromQuery] string format = "csv")
        {
            var portfolio = await _context.Portfolios
                .FirstOrDefaultAsync(p => p.Id == portfolioId);

            if (portfolio == null) return NotFound();

            // Get all holdings with current values
            var holdings = await _context.Holdings
                .Include(h => h.Asset)
                .Include(h => h.Account)
                .Include(h => h.Category)
                .Where(h => h.Account.PortfolioId == portfolioId)
                .Select(h => new
                {
                    AccountName = h.Account.AccountName,
                    AssetSymbol = h.Asset.Symbol,
                    AssetName = h.Asset.Name,
                    Category = h.Category != null ? h.Category.Name : "Uncategorized",
                    h.Units,
                    h.AvgCost,
                    h.CurrentValue
                })
                .ToListAsync();

            // Get all transactions
            var transactions = await _context.Transactions
                .Include(t => t.Asset)
                .Include(t => t.Account)
                .Where(t => t.Account.PortfolioId == portfolioId)
                .OrderByDescending(t => t.EffectiveDate)
                .Select(t => new
                {
                    t.EffectiveDate,
                    Type = t.Type.ToString(),
                    AccountName = t.Account.AccountName,
                    AssetSymbol = t.Asset.Symbol,
                    AssetName = t.Asset.Name,
                    t.Units,
                    t.Amount,
                    t.Narration
                })
                .ToListAsync();

            // Get performance history
            var performance = await _context.PerformanceSnapshots
                .Where(p => p.PortfolioId == portfolioId)
                .OrderBy(p => p.Date)
                .Select(p => new { p.Date, p.TotalValue })
                .ToListAsync();

            if (format.ToLower() == "json")
            {
                return Ok(new
                {
                    Portfolio = new { portfolio.Id, portfolio.Name },
                    ExportDate = DateTime.UtcNow,
                    Holdings = holdings,
                    Transactions = transactions,
                    PerformanceHistory = performance
                });
            }
            else // CSV
            {
                var csv = new System.Text.StringBuilder();

                // Holdings section
                csv.AppendLine("=== HOLDINGS ===");
                csv.AppendLine("Account,Symbol,Name,Category,Units,AvgCost,CurrentValue");
                foreach (var h in holdings)
                {
                    csv.AppendLine($"\"{h.AccountName}\",\"{h.AssetSymbol}\",\"{h.AssetName}\",\"{h.Category}\",{h.Units},{h.AvgCost},{h.CurrentValue}");
                }

                csv.AppendLine();
                csv.AppendLine("=== TRANSACTIONS ===");
                csv.AppendLine("Date,Type,Account,Symbol,Name,Units,Amount,Narration");
                foreach (var t in transactions)
                {
                    csv.AppendLine($"{t.EffectiveDate:yyyy-MM-dd},{t.Type},\"{t.AccountName}\",\"{t.AssetSymbol}\",\"{t.AssetName}\",{t.Units},{t.Amount},\"{t.Narration}\"");
                }

                csv.AppendLine();
                csv.AppendLine("=== PERFORMANCE ===");
                csv.AppendLine("Date,TotalValue");
                foreach (var p in performance)
                {
                    csv.AppendLine($"{p.Date:yyyy-MM-dd},{p.TotalValue}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"{portfolio.Name}_export_{DateTime.UtcNow:yyyyMMdd}.csv");
            }
        }
    }
}
