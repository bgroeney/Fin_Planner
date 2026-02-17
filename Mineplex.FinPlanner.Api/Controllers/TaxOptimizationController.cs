using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models.Entities;

namespace Mineplex.FinPlanner.Api.Controllers;

#region DTOs
public class TrustDistributionOptimizationResult
{
    public List<TrustOptimizationDto> Trusts { get; set; } = new();
    public List<BeneficiaryDto> Beneficiaries { get; set; } = new();
    public decimal TotalTaxCurrent { get; set; }
    public decimal TotalTaxOptimized { get; set; }
    public decimal EstimatedSavings { get; set; }
}

public class TrustOptimizationDto
{
    public Guid TrustId { get; set; }
    public string TrustName { get; set; } = string.Empty;
    public decimal FrankedDividends { get; set; }
    public decimal OtherIncome { get; set; }
    public List<ProposedDistributionDto> ProposedDistributions { get; set; } = new();
}

public class BeneficiaryDto
{
    public Guid BeneficiaryId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public decimal MarginalTaxRate { get; set; }
    public decimal ExistingTaxableIncome { get; set; }
}

public class ProposedDistributionDto
{
    public Guid BeneficiaryId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal FrankedDividends { get; set; }
    public decimal OtherIncome { get; set; }
    public decimal FrankingCredits { get; set; }
    public decimal TaxableIncomeAfterThis { get; set; }
}
#endregion

[Authorize]
[ApiController]
[Route("api/tax-optimization")]
public class TaxOptimizationController : ControllerBase
{
    private readonly FinPlannerDbContext _context;
    private readonly ILogger<TaxOptimizationController> _logger;

    public TaxOptimizationController(
        FinPlannerDbContext context,
        ILogger<TaxOptimizationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get trust distribution optimization for a portfolio
    /// Returns trusts with their income, beneficiaries with tax rates, and proposed distributions
    /// </summary>
    [HttpGet("trust-distributions/{portfolioId}")]
    public async Task<ActionResult<TrustDistributionOptimizationResult>> GetTrustDistributions(
        Guid portfolioId,
        [FromQuery] int fiscalYear = 0)
    {
        // Default to current fiscal year
        if (fiscalYear == 0)
        {
            fiscalYear = DateTime.UtcNow.Month > 6 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;
        }

        // Get all trusts for this portfolio with their beneficiaries and income
        var trusts = await _context.TrustAccounts
            .Where(t => t.PortfolioId == portfolioId)
            .Include(t => t.Beneficiaries)
                .ThenInclude(b => b.PersonAccount)
            .Include(t => t.Beneficiaries)
                .ThenInclude(b => b.CompanyAccount)
            .Include(t => t.TrustIncomes.Where(ti => ti.FiscalYear == fiscalYear))
            .ToListAsync();

        if (!trusts.Any())
        {
            return Ok(new TrustDistributionOptimizationResult());
        }

        // Build beneficaries list with tax rates
        var beneficiaries = new List<BeneficiaryDto>();
        var seenBeneficiaryIds = new HashSet<Guid>();

        foreach (var trust in trusts)
        {
            foreach (var b in trust.Beneficiaries.Where(b => b.IsEligible))
            {
                if (seenBeneficiaryIds.Contains(b.Id)) continue;
                seenBeneficiaryIds.Add(b.Id);

                decimal taxRate = 0.325m; // Default to 32.5% bracket
                if (b.PersonAccount != null)
                {
                    taxRate = b.PersonAccount.MarginalTaxRate;
                }
                else if (b.CompanyAccount != null)
                {
                    taxRate = b.CompanyAccount.IsBaseRateEntity ? 0.25m : 0.30m;
                }

                beneficiaries.Add(new BeneficiaryDto
                {
                    BeneficiaryId = b.Id,
                    BeneficiaryName = b.BeneficiaryName,
                    MarginalTaxRate = taxRate,
                    ExistingTaxableIncome = 0 // Could be enhanced to track existing income
                });
            }
        }

        // Sort beneficiaries by tax rate (lowest first for optimal distribution)
        var sortedBeneficiaries = beneficiaries.OrderBy(b => b.MarginalTaxRate).ToList();

        // Build trust optimization results
        var trustResults = new List<TrustOptimizationDto>();
        decimal totalTaxCurrent = 0;
        decimal totalTaxOptimized = 0;

        foreach (var trust in trusts)
        {
            var income = trust.TrustIncomes.FirstOrDefault();
            if (income == null) continue;

            decimal frankedDividends = income.FrankedDividends;
            decimal otherIncome = income.UnfrankedIncome + income.RentalIncome +
                                  income.DiscountCapitalGains * 0.5m + income.NonDiscountCapitalGains;
            decimal totalIncome = frankedDividends + otherIncome;
            decimal frankingCredits = income.FrankingCredits;

            if (totalIncome <= 0) continue;

            // Calculate current tax (assume distribution to highest tax rate beneficiary)
            var highestRateBeneficiary = sortedBeneficiaries.LastOrDefault();
            decimal currentTax = highestRateBeneficiary != null
                ? (totalIncome + frankingCredits) * highestRateBeneficiary.MarginalTaxRate - frankingCredits
                : totalIncome * 0.45m;
            totalTaxCurrent += Math.Max(0, currentTax);

            // Build proposed distributions (distribute to lowest tax bracket beneficiaries)
            var proposedDistributions = new List<ProposedDistributionDto>();
            decimal remainingIncome = totalIncome;
            decimal remainingFranked = frankedDividends;
            decimal remainingOther = otherIncome;
            decimal remainingCredits = frankingCredits;

            // Get beneficiaries for this specific trust
            var trustBeneficiaries = trust.Beneficiaries
                .Where(b => b.IsEligible)
                .Select(b => beneficiaries.FirstOrDefault(bd => bd.BeneficiaryId == b.Id))
                .Where(b => b != null)
                .OrderBy(b => b!.MarginalTaxRate)
                .ToList();

            if (!trustBeneficiaries.Any()) continue;

            // Simple distribution: allocate proportionally to lowest tax rate beneficiaries
            decimal perBeneficiary = remainingIncome / trustBeneficiaries.Count;

            foreach (var ben in trustBeneficiaries)
            {
                if (remainingIncome <= 0) break;

                decimal allocation = Math.Min(perBeneficiary, remainingIncome);
                decimal frankedPortion = remainingFranked > 0
                    ? Math.Min(remainingFranked, allocation * (frankedDividends / totalIncome))
                    : 0;
                decimal otherPortion = allocation - frankedPortion;
                decimal creditPortion = remainingCredits > 0
                    ? remainingCredits * (allocation / totalIncome)
                    : 0;

                proposedDistributions.Add(new ProposedDistributionDto
                {
                    BeneficiaryId = ben!.BeneficiaryId,
                    BeneficiaryName = ben.BeneficiaryName,
                    TotalAmount = Math.Round(allocation, 2),
                    FrankedDividends = Math.Round(frankedPortion, 2),
                    OtherIncome = Math.Round(otherPortion, 2),
                    FrankingCredits = Math.Round(creditPortion, 2),
                    TaxableIncomeAfterThis = Math.Round(ben.ExistingTaxableIncome + allocation + creditPortion, 2)
                });

                // Calculate optimized tax for this distribution
                decimal taxOnThis = (allocation + creditPortion) * ben.MarginalTaxRate - creditPortion;
                totalTaxOptimized += Math.Max(0, taxOnThis);

                remainingIncome -= allocation;
                remainingFranked -= frankedPortion;
                remainingOther -= otherPortion;
                remainingCredits -= creditPortion;
            }

            trustResults.Add(new TrustOptimizationDto
            {
                TrustId = trust.Id,
                TrustName = trust.TrustName,
                FrankedDividends = frankedDividends,
                OtherIncome = otherIncome,
                ProposedDistributions = proposedDistributions
            });
        }

        return Ok(new TrustDistributionOptimizationResult
        {
            Trusts = trustResults,
            Beneficiaries = sortedBeneficiaries,
            TotalTaxCurrent = Math.Round(totalTaxCurrent, 2),
            TotalTaxOptimized = Math.Round(totalTaxOptimized, 2),
            EstimatedSavings = Math.Round(Math.Max(0, totalTaxCurrent - totalTaxOptimized), 2)
        });
    }
}
