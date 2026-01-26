using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;

namespace Mineplex.FinPlanner.Api.Controllers;

public class DividendCalendarDto
{
    public Guid AssetId { get; set; }
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public decimal Units { get; set; }
    public decimal DividendPerUnit { get; set; }
    public bool IsEstimate { get; set; }
}

public class DividendHistoryDto
{
    public DateTime Date { get; set; }
    public string AssetSymbol { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal FrankingCredits { get; set; }
}

public class MonthlyDividendSummaryDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int PaymentCount { get; set; }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DividendsController : ControllerBase
{
    private readonly FinPlannerDbContext _context;

    public DividendsController(FinPlannerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get upcoming dividends for the next 12 months based on holding patterns
    /// </summary>
    [HttpGet("upcoming/{portfolioId}")]
    public async Task<ActionResult<IEnumerable<DividendCalendarDto>>> GetUpcomingDividends(Guid portfolioId)
    {
        // Get all holdings with dividend history
        var holdings = await _context.Holdings
            .Include(h => h.Asset)
            .Include(h => h.Account)
            .Where(h => h.Account.PortfolioId == portfolioId && h.Units > 0)
            .ToListAsync();

        // Get dividend transactions from the past year to estimate future payments
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        var dividendHistory = await _context.Transactions
            .Include(t => t.Asset)
            .Include(t => t.Account)
            .Where(t => t.Account.PortfolioId == portfolioId
                     && t.Type == Models.TransactionType.Dividend
                     && t.EffectiveDate >= oneYearAgo)
            .GroupBy(t => new { t.AssetId, t.EffectiveDate.Month })
            .Select(g => new
            {
                AssetId = g.Key.AssetId,
                Month = g.Key.Month,
                AvgDividend = g.Average(t => t.Amount),
                DividendCount = g.Count()
            })
            .ToListAsync();

        var upcoming = new List<DividendCalendarDto>();
        var today = DateTime.UtcNow;

        foreach (var holding in holdings)
        {
            // Find historical dividend pattern for this asset
            var assetDividends = dividendHistory.Where(d => d.AssetId == holding.AssetId).ToList();

            if (assetDividends.Any())
            {
                // Estimate future dividends based on historical patterns
                foreach (var monthPattern in assetDividends)
                {
                    // Project for next 12 months
                    for (int yearOffset = 0; yearOffset <= 1; yearOffset++)
                    {
                        var paymentDate = new DateTime(today.Year + yearOffset, monthPattern.Month, 15);
                        if (paymentDate > today && paymentDate <= today.AddMonths(12))
                        {
                            upcoming.Add(new DividendCalendarDto
                            {
                                AssetId = holding.AssetId,
                                AssetSymbol = holding.Asset.Symbol,
                                AssetName = holding.Asset.Name,
                                PaymentDate = paymentDate,
                                EstimatedAmount = monthPattern.AvgDividend,
                                Units = holding.Units,
                                DividendPerUnit = holding.Units > 0 ? monthPattern.AvgDividend / holding.Units : 0,
                                IsEstimate = true
                            });
                        }
                    }
                }
            }
        }

        return Ok(upcoming.OrderBy(d => d.PaymentDate));
    }

    /// <summary>
    /// Get dividend payment history
    /// </summary>
    [HttpGet("history/{portfolioId}")]
    public async Task<ActionResult<IEnumerable<DividendHistoryDto>>> GetDividendHistory(
        Guid portfolioId,
        [FromQuery] int fiscalYear = 0)
    {
        if (fiscalYear == 0) fiscalYear = DateTime.UtcNow.Year + (DateTime.UtcNow.Month > 6 ? 1 : 0);

        var start = new DateTime(fiscalYear - 1, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(fiscalYear, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        var dividends = await _context.Transactions
            .Include(t => t.Asset)
            .Include(t => t.Account)
            .Where(t => t.Account.PortfolioId == portfolioId
                     && t.Type == Models.TransactionType.Dividend
                     && t.EffectiveDate >= start
                     && t.EffectiveDate <= end)
            .OrderByDescending(t => t.EffectiveDate)
            .Select(t => new DividendHistoryDto
            {
                Date = t.EffectiveDate,
                AssetSymbol = t.Asset.Symbol,
                AssetName = t.Asset.Name,
                Amount = t.Amount,
                FrankingCredits = t.Amount * 0.3m / 0.7m // Approximate franking credits
            })
            .ToListAsync();

        return Ok(dividends);
    }

    /// <summary>
    /// Get monthly dividend summary for charting
    /// </summary>
    [HttpGet("summary/{portfolioId}")]
    public async Task<ActionResult<IEnumerable<MonthlyDividendSummaryDto>>> GetMonthlySummary(Guid portfolioId)
    {
        var twoYearsAgo = DateTime.UtcNow.AddYears(-2);

        var summary = await _context.Transactions
            .Include(t => t.Account)
            .Where(t => t.Account.PortfolioId == portfolioId
                     && t.Type == Models.TransactionType.Dividend
                     && t.EffectiveDate >= twoYearsAgo)
            .GroupBy(t => new { t.EffectiveDate.Year, t.EffectiveDate.Month })
            .Select(g => new MonthlyDividendSummaryDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalAmount = g.Sum(t => t.Amount),
                PaymentCount = g.Count()
            })
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToListAsync();

        // Add month names
        var monthNames = new[] { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        foreach (var item in summary)
        {
            item.MonthName = monthNames[item.Month];
        }

        return Ok(summary);
    }
}
