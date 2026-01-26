using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models.Entities;
using Mineplex.FinPlanner.Api.Services;
using System.Security.Claims;

namespace Mineplex.FinPlanner.Api.Controllers;

#region DTOs
public class CreatePersonAccountDto
{
    public Guid PortfolioId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public decimal MarginalTaxRate { get; set; }
    public bool HasPrivateHealthInsurance { get; set; }
}

public class UpdatePersonAccountDto
{
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public decimal MarginalTaxRate { get; set; }
    public bool HasPrivateHealthInsurance { get; set; }
}

public class CreateSuperAccountDto
{
    public string FundName { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public decimal CurrentBalance { get; set; }
    public string InvestmentOption { get; set; } = "Balanced";
    public DateTime PreservationDate { get; set; }
}

public class CreatePayrollIncomeDto
{
    public int FiscalYear { get; set; }
    public string Employer { get; set; } = string.Empty;
    public decimal GrossSalary { get; set; }
    public decimal TaxWithheld { get; set; }
    public decimal SuperContribution { get; set; }
    public decimal SalarySacrifice { get; set; }
}

public class CreateDeductionDto
{
    public int FiscalYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool IsEstimate { get; set; }
}
#endregion

[Authorize]
[ApiController]
[Route("api/persons")]
public class PersonAccountsController : ControllerBase
{
    private readonly FinPlannerDbContext _context;
    private readonly ITaxDistributionService _taxService;
    private readonly ILogger<PersonAccountsController> _logger;

    public PersonAccountsController(
        FinPlannerDbContext context,
        ITaxDistributionService taxService,
        ILogger<PersonAccountsController> logger)
    {
        _context = context;
        _taxService = taxService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    #region Person CRUD
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PersonAccount>>> GetPersonAccounts([FromQuery] Guid? portfolioId)
    {
        var query = _context.PersonAccounts
            .Include(p => p.SuperAccounts)
            .AsQueryable();

        if (portfolioId.HasValue)
        {
            query = query.Where(p => p.PortfolioId == portfolioId.Value);
        }

        return await query.OrderBy(p => p.FullName).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PersonAccount>> GetPersonAccount(Guid id)
    {
        var person = await _context.PersonAccounts
            .Include(p => p.SuperAccounts)
            .Include(p => p.PayrollIncomes.OrderByDescending(pi => pi.FiscalYear).Take(3))
            .Include(p => p.Deductions.OrderByDescending(d => d.FiscalYear).Take(20))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person == null) return NotFound();
        return person;
    }

    [HttpPost]
    public async Task<ActionResult<PersonAccount>> CreatePersonAccount(CreatePersonAccountDto dto)
    {
        var person = new PersonAccount
        {
            Id = Guid.NewGuid(),
            PortfolioId = dto.PortfolioId,
            FullName = dto.FullName,
            DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc),
            MarginalTaxRate = dto.MarginalTaxRate,
            HasPrivateHealthInsurance = dto.HasPrivateHealthInsurance,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PersonAccounts.Add(person);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPersonAccount), new { id = person.Id }, person);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePersonAccount(Guid id, UpdatePersonAccountDto dto)
    {
        var person = await _context.PersonAccounts.FindAsync(id);
        if (person == null) return NotFound();

        person.FullName = dto.FullName;
        person.DateOfBirth = DateTime.SpecifyKind(dto.DateOfBirth, DateTimeKind.Utc);
        person.MarginalTaxRate = dto.MarginalTaxRate;
        person.HasPrivateHealthInsurance = dto.HasPrivateHealthInsurance;
        person.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePersonAccount(Guid id)
    {
        var person = await _context.PersonAccounts.FindAsync(id);
        if (person == null) return NotFound();

        _context.PersonAccounts.Remove(person);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Super Accounts
    [HttpPost("{personId}/super")]
    public async Task<ActionResult<SuperAccount>> CreateSuperAccount(Guid personId, CreateSuperAccountDto dto)
    {
        var person = await _context.PersonAccounts.FindAsync(personId);
        if (person == null) return NotFound("Person not found");

        var super = new SuperAccount
        {
            Id = Guid.NewGuid(),
            PersonAccountId = personId,
            FundName = dto.FundName,
            MemberNumber = dto.MemberNumber,
            CurrentBalance = dto.CurrentBalance,
            InvestmentOption = dto.InvestmentOption,
            PreservationDate = DateTime.SpecifyKind(dto.PreservationDate, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SuperAccounts.Add(super);
        await _context.SaveChangesAsync();

        return Created($"/api/persons/{personId}/super/{super.Id}", super);
    }

    [HttpPut("{personId}/super/{superId}")]
    public async Task<IActionResult> UpdateSuperAccount(Guid personId, Guid superId, CreateSuperAccountDto dto)
    {
        var super = await _context.SuperAccounts
            .FirstOrDefaultAsync(s => s.Id == superId && s.PersonAccountId == personId);

        if (super == null) return NotFound();

        super.FundName = dto.FundName;
        super.MemberNumber = dto.MemberNumber;
        super.CurrentBalance = dto.CurrentBalance;
        super.InvestmentOption = dto.InvestmentOption;
        super.PreservationDate = DateTime.SpecifyKind(dto.PreservationDate, DateTimeKind.Utc);
        super.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{personId}/super/{superId}")]
    public async Task<IActionResult> DeleteSuperAccount(Guid personId, Guid superId)
    {
        var super = await _context.SuperAccounts
            .FirstOrDefaultAsync(s => s.Id == superId && s.PersonAccountId == personId);

        if (super == null) return NotFound();

        _context.SuperAccounts.Remove(super);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Payroll Income
    [HttpPost("{personId}/income")]
    public async Task<ActionResult<PayrollIncome>> CreatePayrollIncome(Guid personId, CreatePayrollIncomeDto dto)
    {
        var person = await _context.PersonAccounts.FindAsync(personId);
        if (person == null) return NotFound("Person not found");

        var income = new PayrollIncome
        {
            Id = Guid.NewGuid(),
            PersonAccountId = personId,
            FiscalYear = dto.FiscalYear,
            Employer = dto.Employer,
            GrossSalary = dto.GrossSalary,
            TaxWithheld = dto.TaxWithheld,
            SuperContribution = dto.SuperContribution,
            SalarySacrifice = dto.SalarySacrifice,
            CreatedAt = DateTime.UtcNow
        };

        _context.PayrollIncomes.Add(income);
        await _context.SaveChangesAsync();

        return Created($"/api/persons/{personId}/income/{income.Id}", income);
    }

    [HttpDelete("{personId}/income/{incomeId}")]
    public async Task<IActionResult> DeletePayrollIncome(Guid personId, Guid incomeId)
    {
        var income = await _context.PayrollIncomes
            .FirstOrDefaultAsync(i => i.Id == incomeId && i.PersonAccountId == personId);

        if (income == null) return NotFound();

        _context.PayrollIncomes.Remove(income);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Deductions
    [HttpPost("{personId}/deductions")]
    public async Task<ActionResult<Deduction>> CreateDeduction(Guid personId, CreateDeductionDto dto)
    {
        var person = await _context.PersonAccounts.FindAsync(personId);
        if (person == null) return NotFound("Person not found");

        var deduction = new Deduction
        {
            Id = Guid.NewGuid(),
            PersonAccountId = personId,
            FiscalYear = dto.FiscalYear,
            Category = dto.Category,
            Description = dto.Description,
            Amount = dto.Amount,
            IsEstimate = dto.IsEstimate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Deductions.Add(deduction);
        await _context.SaveChangesAsync();

        return Created($"/api/persons/{personId}/deductions/{deduction.Id}", deduction);
    }

    [HttpDelete("{personId}/deductions/{deductionId}")]
    public async Task<IActionResult> DeleteDeduction(Guid personId, Guid deductionId)
    {
        var deduction = await _context.Deductions
            .FirstOrDefaultAsync(d => d.Id == deductionId && d.PersonAccountId == personId);

        if (deduction == null) return NotFound();

        _context.Deductions.Remove(deduction);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Tax Position
    [HttpGet("{personId}/tax-position")]
    public async Task<ActionResult<BeneficiaryTaxPosition>> GetTaxPosition(Guid personId, [FromQuery] int fiscalYear = 0)
    {
        if (fiscalYear == 0)
        {
            fiscalYear = DateTime.UtcNow.Month > 6 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;
        }

        var position = await _taxService.GetBeneficiaryTaxPositionAsync(personId, fiscalYear);
        return Ok(position);
    }
    #endregion
}
