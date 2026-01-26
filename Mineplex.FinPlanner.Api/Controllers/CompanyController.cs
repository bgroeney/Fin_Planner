using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models.Entities;

namespace Mineplex.FinPlanner.Api.Controllers;

#region DTOs
public class CreateCompanyAccountDto
{
    public Guid PortfolioId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ABN { get; set; }
    public string? ACN { get; set; }
    public decimal RetainedProfits { get; set; }
    public decimal FrankingAccountBalance { get; set; }
    public bool IsBaseRateEntity { get; set; } = true;
    public DateTime IncorporationDate { get; set; }
}

public class CreateDivision7ALoanDto
{
    public Guid BorrowerId { get; set; }
    public string BorrowerName { get; set; } = string.Empty;
    public decimal PrincipalAmount { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime LoanDate { get; set; }
    public int LoanTermYears { get; set; } = 7;
    public bool IsSecured { get; set; }
}

public class CreateCompanyDividendDto
{
    public DateTime DeclarationDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public decimal FrankingPercentage { get; set; } = 100;
}

public class LoanRepaymentDto
{
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
}
#endregion

[Authorize]
[ApiController]
[Route("api/companies")]
public class CompanyController : ControllerBase
{
    private readonly FinPlannerDbContext _context;
    private readonly ILogger<CompanyController> _logger;

    // Division 7A Benchmark Interest Rate (RBA rate + margin)
    private const decimal Division7ABenchmarkRate = 0.0847m; // 8.47% for FY25

    public CompanyController(FinPlannerDbContext context, ILogger<CompanyController> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Company CRUD
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyAccount>>> GetCompanyAccounts([FromQuery] Guid? portfolioId)
    {
        var query = _context.CompanyAccounts
            .Include(c => c.Division7ALoans)
            .AsQueryable();

        if (portfolioId.HasValue)
        {
            query = query.Where(c => c.PortfolioId == portfolioId.Value);
        }

        return await query.OrderBy(c => c.CompanyName).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyAccount>> GetCompanyAccount(Guid id)
    {
        var company = await _context.CompanyAccounts
            .Include(c => c.Division7ALoans)
            .Include(c => c.Dividends.OrderByDescending(d => d.PaymentDate).Take(10))
            .FirstOrDefaultAsync(c => c.Id == id);

        if (company == null) return NotFound();
        return company;
    }

    [HttpPost]
    public async Task<ActionResult<CompanyAccount>> CreateCompanyAccount(CreateCompanyAccountDto dto)
    {
        var company = new CompanyAccount
        {
            Id = Guid.NewGuid(),
            PortfolioId = dto.PortfolioId,
            CompanyName = dto.CompanyName,
            ABN = dto.ABN,
            ACN = dto.ACN,
            RetainedProfits = dto.RetainedProfits,
            FrankingAccountBalance = dto.FrankingAccountBalance,
            IsBaseRateEntity = dto.IsBaseRateEntity,
            TaxRate = dto.IsBaseRateEntity ? 0.25m : 0.30m,
            IncorporationDate = DateTime.SpecifyKind(dto.IncorporationDate, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.CompanyAccounts.Add(company);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCompanyAccount), new { id = company.Id }, company);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCompanyAccount(Guid id, CreateCompanyAccountDto dto)
    {
        var company = await _context.CompanyAccounts.FindAsync(id);
        if (company == null) return NotFound();

        company.CompanyName = dto.CompanyName;
        company.ABN = dto.ABN;
        company.ACN = dto.ACN;
        company.RetainedProfits = dto.RetainedProfits;
        company.FrankingAccountBalance = dto.FrankingAccountBalance;
        company.IsBaseRateEntity = dto.IsBaseRateEntity;
        company.TaxRate = dto.IsBaseRateEntity ? 0.25m : 0.30m;
        company.IncorporationDate = DateTime.SpecifyKind(dto.IncorporationDate, DateTimeKind.Utc);
        company.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompanyAccount(Guid id)
    {
        var company = await _context.CompanyAccounts.FindAsync(id);
        if (company == null) return NotFound();

        _context.CompanyAccounts.Remove(company);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Division 7A Loans
    [HttpPost("{companyId}/loans")]
    public async Task<ActionResult<Division7ALoan>> CreateDivision7ALoan(Guid companyId, CreateDivision7ALoanDto dto)
    {
        var company = await _context.CompanyAccounts.FindAsync(companyId);
        if (company == null) return NotFound("Company not found");

        // Validate interest rate meets minimum
        if (dto.InterestRate < Division7ABenchmarkRate)
        {
            return BadRequest($"Interest rate must be at least {Division7ABenchmarkRate:P2} (RBA benchmark rate)");
        }

        // Calculate minimum yearly repayment
        var minRepayment = CalculateMinimumRepayment(dto.PrincipalAmount, dto.InterestRate, dto.LoanTermYears);

        var loan = new Division7ALoan
        {
            Id = Guid.NewGuid(),
            CompanyAccountId = companyId,
            BorrowerId = dto.BorrowerId,
            BorrowerName = dto.BorrowerName,
            PrincipalAmount = dto.PrincipalAmount,
            CurrentBalance = dto.PrincipalAmount,
            InterestRate = dto.InterestRate,
            LoanDate = DateTime.SpecifyKind(dto.LoanDate, DateTimeKind.Utc),
            LoanTermYears = dto.LoanTermYears,
            IsSecured = dto.IsSecured,
            MinimumYearlyRepayment = minRepayment,
            RepaidThisYear = 0,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Division7ALoans.Add(loan);
        await _context.SaveChangesAsync();

        return Created($"/api/companies/{companyId}/loans/{loan.Id}", loan);
    }

    [HttpGet("{companyId}/loans")]
    public async Task<ActionResult<IEnumerable<Division7ALoan>>> GetLoans(Guid companyId)
    {
        var loans = await _context.Division7ALoans
            .Where(l => l.CompanyAccountId == companyId)
            .OrderBy(l => l.LoanDate)
            .ToListAsync();

        return loans;
    }

    [HttpGet("{companyId}/loans/{loanId}")]
    public async Task<ActionResult<Division7ALoan>> GetLoan(Guid companyId, Guid loanId)
    {
        var loan = await _context.Division7ALoans
            .FirstOrDefaultAsync(l => l.Id == loanId && l.CompanyAccountId == companyId);

        if (loan == null) return NotFound();
        return loan;
    }

    [HttpPost("{companyId}/loans/{loanId}/repayment")]
    public async Task<IActionResult> RecordRepayment(Guid companyId, Guid loanId, LoanRepaymentDto dto)
    {
        var loan = await _context.Division7ALoans
            .FirstOrDefaultAsync(l => l.Id == loanId && l.CompanyAccountId == companyId);

        if (loan == null) return NotFound();

        loan.CurrentBalance -= dto.Amount;
        loan.RepaidThisYear += dto.Amount;
        loan.UpdatedAt = DateTime.UtcNow;

        if (loan.CurrentBalance <= 0)
        {
            loan.CurrentBalance = 0;
            loan.Status = "PaidOut";
        }

        await _context.SaveChangesAsync();

        // Check if minimum repayment met
        var warningMessage = loan.RepaidThisYear < loan.MinimumYearlyRepayment
            ? $"Warning: Repayments this FY (${loan.RepaidThisYear:N2}) are below minimum (${loan.MinimumYearlyRepayment:N2})"
            : null;

        return Ok(new
        {
            CurrentBalance = loan.CurrentBalance,
            RepaidThisYear = loan.RepaidThisYear,
            MinimumYearlyRepayment = loan.MinimumYearlyRepayment,
            Status = loan.Status,
            Warning = warningMessage
        });
    }

    [HttpPost("{companyId}/loans/year-end")]
    public async Task<IActionResult> ProcessYearEnd(Guid companyId)
    {
        var loans = await _context.Division7ALoans
            .Where(l => l.CompanyAccountId == companyId && l.Status == "Active")
            .ToListAsync();

        var results = new List<object>();

        foreach (var loan in loans)
        {
            // Check if minimum repayment was met
            var shortfall = loan.MinimumYearlyRepayment - loan.RepaidThisYear;

            if (shortfall > 0)
            {
                // Shortfall becomes a deemed dividend
                loan.Status = "Deemed";
                results.Add(new
                {
                    LoanId = loan.Id,
                    BorrowerName = loan.BorrowerName,
                    Shortfall = shortfall,
                    Status = "DEEMED DIVIDEND - Shortfall treated as unfranked dividend"
                });
            }
            else
            {
                // Reset for new year and recalculate minimum
                loan.RepaidThisYear = 0;
                loan.MinimumYearlyRepayment = CalculateMinimumRepayment(
                    loan.CurrentBalance, loan.InterestRate,
                    Math.Max(1, loan.LoanTermYears - 1));

                results.Add(new
                {
                    LoanId = loan.Id,
                    BorrowerName = loan.BorrowerName,
                    NewBalance = loan.CurrentBalance,
                    NewMinimumRepayment = loan.MinimumYearlyRepayment,
                    Status = "OK - Rolled over to new year"
                });
            }

            loan.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { ProcessedLoans = results.Count, Results = results });
    }

    private decimal CalculateMinimumRepayment(decimal principal, decimal interestRate, int remainingYears)
    {
        // Division 7A minimum repayment formula
        // PMT = P * r * (1+r)^n / ((1+r)^n - 1)
        var r = interestRate;
        var n = remainingYears;

        if (r == 0) return principal / n;

        var pow = (decimal)Math.Pow((double)(1 + r), n);
        var pmt = principal * r * pow / (pow - 1);

        return Math.Round(pmt, 2);
    }
    #endregion

    #region Dividends
    [HttpPost("{companyId}/dividends")]
    public async Task<ActionResult<CompanyDividend>> DeclareDividend(Guid companyId, CreateCompanyDividendDto dto)
    {
        var company = await _context.CompanyAccounts.FindAsync(companyId);
        if (company == null) return NotFound("Company not found");

        // Calculate franking credits
        var frankingCredits = dto.Amount * (dto.FrankingPercentage / 100m) *
                             (company.TaxRate / (1 - company.TaxRate));

        // Check franking account balance
        if (frankingCredits > company.FrankingAccountBalance)
        {
            return BadRequest($"Insufficient franking credits. Available: ${company.FrankingAccountBalance:N2}, Required: ${frankingCredits:N2}");
        }

        var dividend = new CompanyDividend
        {
            Id = Guid.NewGuid(),
            CompanyAccountId = companyId,
            DeclarationDate = DateTime.SpecifyKind(dto.DeclarationDate, DateTimeKind.Utc),
            PaymentDate = DateTime.SpecifyKind(dto.PaymentDate, DateTimeKind.Utc),
            Amount = dto.Amount,
            FrankingPercentage = dto.FrankingPercentage,
            FrankingCredits = frankingCredits,
            Status = "Declared",
            CreatedAt = DateTime.UtcNow
        };

        // Reduce franking account
        company.FrankingAccountBalance -= frankingCredits;
        company.RetainedProfits -= dto.Amount;
        company.UpdatedAt = DateTime.UtcNow;

        _context.CompanyDividends.Add(dividend);
        await _context.SaveChangesAsync();

        return Created($"/api/companies/{companyId}/dividends/{dividend.Id}", dividend);
    }

    [HttpGet("{companyId}/dividends")]
    public async Task<ActionResult<IEnumerable<CompanyDividend>>> GetDividends(Guid companyId)
    {
        var dividends = await _context.CompanyDividends
            .Where(d => d.CompanyAccountId == companyId)
            .OrderByDescending(d => d.PaymentDate)
            .ToListAsync();

        return dividends;
    }

    [HttpPost("{companyId}/dividends/{dividendId}/pay")]
    public async Task<IActionResult> PayDividend(Guid companyId, Guid dividendId)
    {
        var dividend = await _context.CompanyDividends
            .FirstOrDefaultAsync(d => d.Id == dividendId && d.CompanyAccountId == companyId);

        if (dividend == null) return NotFound();
        if (dividend.Status == "Paid") return BadRequest("Dividend already paid");

        dividend.Status = "Paid";
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Dividend marked as paid", Dividend = dividend });
    }
    #endregion

    #region Summary
    [HttpGet("{companyId}/summary")]
    public async Task<IActionResult> GetCompanySummary(Guid companyId)
    {
        var company = await _context.CompanyAccounts
            .Include(c => c.Division7ALoans.Where(l => l.Status == "Active"))
            .Include(c => c.Dividends)
            .FirstOrDefaultAsync(c => c.Id == companyId);

        if (company == null) return NotFound();

        var activeLoans = company.Division7ALoans.Count;
        var totalLoanBalance = company.Division7ALoans.Sum(l => l.CurrentBalance);
        var totalDividendsPaid = company.Dividends.Where(d => d.Status == "Paid").Sum(d => d.Amount);

        return Ok(new
        {
            company.CompanyName,
            company.RetainedProfits,
            company.FrankingAccountBalance,
            company.TaxRate,
            company.IsBaseRateEntity,
            ActiveLoans = activeLoans,
            TotalLoanBalance = totalLoanBalance,
            TotalDividendsPaid = totalDividendsPaid,
            Division7AWarnings = company.Division7ALoans
                .Where(l => l.RepaidThisYear < l.MinimumYearlyRepayment && l.Status == "Active")
                .Select(l => new
                {
                    l.BorrowerName,
                    l.MinimumYearlyRepayment,
                    l.RepaidThisYear,
                    Shortfall = l.MinimumYearlyRepayment - l.RepaidThisYear
                })
                .ToList()
        });
    }
    #endregion
}
