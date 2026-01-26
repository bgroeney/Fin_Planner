using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models.Entities;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers;

#region DTOs
public class CreateTrustAccountDto
{
    public Guid PortfolioId { get; set; }
    public string TrustName { get; set; } = string.Empty;
    public string? ABN { get; set; }
    public string TrusteeName { get; set; } = string.Empty;
    public string TrustType { get; set; } = "Discretionary";
    public DateTime? TrustDeedDate { get; set; }
}

public class CreateBeneficiaryDto
{
    public Guid? PersonAccountId { get; set; }
    public Guid? CompanyAccountId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public bool IsEligible { get; set; } = true;
}

public class CreateTrustIncomeDto
{
    public int FiscalYear { get; set; }
    public decimal FrankedDividends { get; set; }
    public decimal UnfrankedIncome { get; set; }
    public decimal DiscountCapitalGains { get; set; }
    public decimal NonDiscountCapitalGains { get; set; }
    public decimal RentalIncome { get; set; }
    public decimal FrankingCredits { get; set; }
}

public class CreateDistributionDto
{
    public Guid BeneficiaryId { get; set; }
    public int FiscalYear { get; set; }
    public DateTime ResolutionDate { get; set; }
    public decimal FrankedDividends { get; set; }
    public decimal UnfrankedIncome { get; set; }
    public decimal DiscountCapitalGains { get; set; }
    public decimal NonDiscountCapitalGains { get; set; }
    public decimal FrankingCredits { get; set; }
}
#endregion

[Authorize]
[ApiController]
[Route("api/trusts")]
public class TrustController : ControllerBase
{
    private readonly FinPlannerDbContext _context;
    private readonly ITaxDistributionService _taxService;
    private readonly ILogger<TrustController> _logger;

    public TrustController(
        FinPlannerDbContext context,
        ITaxDistributionService taxService,
        ILogger<TrustController> logger)
    {
        _context = context;
        _taxService = taxService;
        _logger = logger;
    }

    #region Trust CRUD
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrustAccount>>> GetTrustAccounts([FromQuery] Guid? portfolioId)
    {
        var query = _context.TrustAccounts
            .Include(t => t.Beneficiaries)
            .AsQueryable();

        if (portfolioId.HasValue)
        {
            query = query.Where(t => t.PortfolioId == portfolioId.Value);
        }

        return await query.OrderBy(t => t.TrustName).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TrustAccount>> GetTrustAccount(Guid id)
    {
        var trust = await _context.TrustAccounts
            .Include(t => t.Beneficiaries)
                .ThenInclude(b => b.PersonAccount)
            .Include(t => t.Beneficiaries)
                .ThenInclude(b => b.CompanyAccount)
            .Include(t => t.TrustIncomes.OrderByDescending(ti => ti.FiscalYear).Take(3))
            .Include(t => t.Distributions.OrderByDescending(d => d.FiscalYear).Take(20))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trust == null) return NotFound();
        return trust;
    }

    [HttpPost]
    public async Task<ActionResult<TrustAccount>> CreateTrustAccount(CreateTrustAccountDto dto)
    {
        var trust = new TrustAccount
        {
            Id = Guid.NewGuid(),
            PortfolioId = dto.PortfolioId,
            TrustName = dto.TrustName,
            ABN = dto.ABN,
            TrusteeName = dto.TrusteeName,
            TrustType = dto.TrustType,
            TrustDeedDate = dto.TrustDeedDate.HasValue ?
                DateTime.SpecifyKind(dto.TrustDeedDate.Value, DateTimeKind.Utc) : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TrustAccounts.Add(trust);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTrustAccount), new { id = trust.Id }, trust);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTrustAccount(Guid id, CreateTrustAccountDto dto)
    {
        var trust = await _context.TrustAccounts.FindAsync(id);
        if (trust == null) return NotFound();

        trust.TrustName = dto.TrustName;
        trust.ABN = dto.ABN;
        trust.TrusteeName = dto.TrusteeName;
        trust.TrustType = dto.TrustType;
        trust.TrustDeedDate = dto.TrustDeedDate.HasValue ?
            DateTime.SpecifyKind(dto.TrustDeedDate.Value, DateTimeKind.Utc) : null;
        trust.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrustAccount(Guid id)
    {
        var trust = await _context.TrustAccounts.FindAsync(id);
        if (trust == null) return NotFound();

        _context.TrustAccounts.Remove(trust);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Beneficiaries
    [HttpPost("{trustId}/beneficiaries")]
    public async Task<ActionResult<TrustBeneficiary>> AddBeneficiary(Guid trustId, CreateBeneficiaryDto dto)
    {
        var trust = await _context.TrustAccounts.FindAsync(trustId);
        if (trust == null) return NotFound("Trust not found");

        var beneficiary = new TrustBeneficiary
        {
            Id = Guid.NewGuid(),
            TrustAccountId = trustId,
            PersonAccountId = dto.PersonAccountId,
            CompanyAccountId = dto.CompanyAccountId,
            BeneficiaryName = dto.BeneficiaryName,
            Relationship = dto.Relationship,
            IsEligible = dto.IsEligible,
            CreatedAt = DateTime.UtcNow
        };

        _context.TrustBeneficiaries.Add(beneficiary);
        await _context.SaveChangesAsync();

        return Created($"/api/trusts/{trustId}/beneficiaries/{beneficiary.Id}", beneficiary);
    }

    [HttpPut("{trustId}/beneficiaries/{beneficiaryId}")]
    public async Task<IActionResult> UpdateBeneficiary(Guid trustId, Guid beneficiaryId, CreateBeneficiaryDto dto)
    {
        var beneficiary = await _context.TrustBeneficiaries
            .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.TrustAccountId == trustId);

        if (beneficiary == null) return NotFound();

        beneficiary.BeneficiaryName = dto.BeneficiaryName;
        beneficiary.Relationship = dto.Relationship;
        beneficiary.IsEligible = dto.IsEligible;
        beneficiary.PersonAccountId = dto.PersonAccountId;
        beneficiary.CompanyAccountId = dto.CompanyAccountId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{trustId}/beneficiaries/{beneficiaryId}")]
    public async Task<IActionResult> RemoveBeneficiary(Guid trustId, Guid beneficiaryId)
    {
        var beneficiary = await _context.TrustBeneficiaries
            .FirstOrDefaultAsync(b => b.Id == beneficiaryId && b.TrustAccountId == trustId);

        if (beneficiary == null) return NotFound();

        _context.TrustBeneficiaries.Remove(beneficiary);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Trust Income
    [HttpPost("{trustId}/income")]
    public async Task<ActionResult<TrustIncome>> RecordTrustIncome(Guid trustId, CreateTrustIncomeDto dto)
    {
        var trust = await _context.TrustAccounts.FindAsync(trustId);
        if (trust == null) return NotFound("Trust not found");

        // Check if income already exists for this FY
        var existing = await _context.TrustIncomes
            .FirstOrDefaultAsync(ti => ti.TrustAccountId == trustId && ti.FiscalYear == dto.FiscalYear);

        if (existing != null)
        {
            // Update existing
            existing.FrankedDividends = dto.FrankedDividends;
            existing.UnfrankedIncome = dto.UnfrankedIncome;
            existing.DiscountCapitalGains = dto.DiscountCapitalGains;
            existing.NonDiscountCapitalGains = dto.NonDiscountCapitalGains;
            existing.RentalIncome = dto.RentalIncome;
            existing.FrankingCredits = dto.FrankingCredits;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        var income = new TrustIncome
        {
            Id = Guid.NewGuid(),
            TrustAccountId = trustId,
            FiscalYear = dto.FiscalYear,
            FrankedDividends = dto.FrankedDividends,
            UnfrankedIncome = dto.UnfrankedIncome,
            DiscountCapitalGains = dto.DiscountCapitalGains,
            NonDiscountCapitalGains = dto.NonDiscountCapitalGains,
            RentalIncome = dto.RentalIncome,
            FrankingCredits = dto.FrankingCredits,
            CreatedAt = DateTime.UtcNow
        };

        _context.TrustIncomes.Add(income);
        await _context.SaveChangesAsync();

        return Created($"/api/trusts/{trustId}/income/{income.Id}", income);
    }

    [HttpGet("{trustId}/income/{fiscalYear}")]
    public async Task<ActionResult<TrustIncome>> GetTrustIncome(Guid trustId, int fiscalYear)
    {
        var income = await _context.TrustIncomes
            .FirstOrDefaultAsync(ti => ti.TrustAccountId == trustId && ti.FiscalYear == fiscalYear);

        if (income == null) return NotFound();
        return income;
    }
    #endregion

    #region Distributions
    [HttpPost("{trustId}/distributions")]
    public async Task<ActionResult<TrustDistribution>> CreateDistribution(Guid trustId, CreateDistributionDto dto)
    {
        var trust = await _context.TrustAccounts.FindAsync(trustId);
        if (trust == null) return NotFound("Trust not found");

        var beneficiary = await _context.TrustBeneficiaries.FindAsync(dto.BeneficiaryId);
        if (beneficiary == null || beneficiary.TrustAccountId != trustId)
            return BadRequest("Invalid beneficiary");

        var distribution = new TrustDistribution
        {
            Id = Guid.NewGuid(),
            TrustAccountId = trustId,
            BeneficiaryId = dto.BeneficiaryId,
            FiscalYear = dto.FiscalYear,
            ResolutionDate = DateTime.SpecifyKind(dto.ResolutionDate, DateTimeKind.Utc),
            FrankedDividends = dto.FrankedDividends,
            UnfrankedIncome = dto.UnfrankedIncome,
            DiscountCapitalGains = dto.DiscountCapitalGains,
            NonDiscountCapitalGains = dto.NonDiscountCapitalGains,
            FrankingCredits = dto.FrankingCredits,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        _context.TrustDistributions.Add(distribution);
        await _context.SaveChangesAsync();

        return Created($"/api/trusts/{trustId}/distributions/{distribution.Id}", distribution);
    }

    [HttpGet("{trustId}/distributions")]
    public async Task<ActionResult<IEnumerable<TrustDistribution>>> GetDistributions(
        Guid trustId, [FromQuery] int? fiscalYear)
    {
        var query = _context.TrustDistributions
            .Include(d => d.Beneficiary)
            .Where(d => d.TrustAccountId == trustId);

        if (fiscalYear.HasValue)
        {
            query = query.Where(d => d.FiscalYear == fiscalYear.Value);
        }

        return await query.OrderByDescending(d => d.FiscalYear)
            .ThenBy(d => d.Beneficiary!.BeneficiaryName)
            .ToListAsync();
    }

    [HttpDelete("{trustId}/distributions/{distributionId}")]
    public async Task<IActionResult> DeleteDistribution(Guid trustId, Guid distributionId)
    {
        var distribution = await _context.TrustDistributions
            .FirstOrDefaultAsync(d => d.Id == distributionId && d.TrustAccountId == trustId);

        if (distribution == null) return NotFound();
        if (distribution.Status != "Draft")
            return BadRequest("Can only delete draft distributions");

        _context.TrustDistributions.Remove(distribution);
        await _context.SaveChangesAsync();
        return NoContent();
    }
    #endregion

    #region Tax Optimization
    [HttpGet("{trustId}/optimize-distribution")]
    public async Task<ActionResult<TaxDistributionResult>> OptimizeDistribution(
        Guid trustId, [FromQuery] int fiscalYear = 0)
    {
        if (fiscalYear == 0)
        {
            fiscalYear = DateTime.UtcNow.Month > 6 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;
        }

        try
        {
            var result = await _taxService.OptimizeDistributionAsync(trustId, fiscalYear);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{trustId}/apply-optimization")]
    public async Task<IActionResult> ApplyOptimization(Guid trustId, [FromQuery] int fiscalYear = 0)
    {
        if (fiscalYear == 0)
        {
            fiscalYear = DateTime.UtcNow.Month > 6 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;
        }

        var result = await _taxService.OptimizeDistributionAsync(trustId, fiscalYear);

        // Create distributions from recommendations
        foreach (var rec in result.Recommendations)
        {
            var distribution = new TrustDistribution
            {
                Id = Guid.NewGuid(),
                TrustAccountId = trustId,
                BeneficiaryId = rec.BeneficiaryId,
                FiscalYear = fiscalYear,
                ResolutionDate = DateTime.UtcNow,
                FrankedDividends = rec.FrankedDividends,
                UnfrankedIncome = rec.UnfrankedIncome,
                DiscountCapitalGains = rec.DiscountCapitalGains,
                NonDiscountCapitalGains = rec.NonDiscountCapitalGains,
                FrankingCredits = rec.FrankingCredits,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            };

            _context.TrustDistributions.Add(distribution);
        }

        await _context.SaveChangesAsync();

        return Ok(new { Message = $"Created {result.Recommendations.Count} draft distributions" });
    }
    #endregion
}
