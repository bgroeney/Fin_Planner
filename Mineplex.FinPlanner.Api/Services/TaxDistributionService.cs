using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models.Entities;

namespace Mineplex.FinPlanner.Api.Services;

/// <summary>
/// Calculates optimal distribution of trust income across beneficiaries to minimize overall family tax
/// </summary>
public interface ITaxDistributionService
{
    Task<TaxDistributionResult> OptimizeDistributionAsync(Guid trustAccountId, int fiscalYear);
    decimal CalculateMarginalTaxRate(decimal taxableIncome);
    Task<BeneficiaryTaxPosition> GetBeneficiaryTaxPositionAsync(Guid personAccountId, int fiscalYear);
}

public class TaxDistributionService : ITaxDistributionService
{
    private readonly FinPlannerDbContext _context;
    private readonly ILogger<TaxDistributionService> _logger;

    // FY2025 Australian Tax Brackets (Stage 3 Tax Cuts)
    private static readonly (decimal threshold, decimal rate)[] TaxBrackets = new[]
    {
        (18200m, 0.0m),    // Tax-free threshold
        (45000m, 0.16m),   // 16% bracket
        (135000m, 0.30m),  // 30% bracket
        (190000m, 0.37m),  // 37% bracket
        (decimal.MaxValue, 0.45m)  // 45% top bracket
    };

    private const decimal MedicareLevyRate = 0.02m;
    private const decimal MedicareLevyThreshold = 24276m; // FY24-25 Individual threshold
    private const decimal CompanyTaxRate = 0.30m;
    private const decimal BaseRateEntityTaxRate = 0.25m;

    public TaxDistributionService(FinPlannerDbContext context, ILogger<TaxDistributionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate current tax position for an individual beneficiary
    /// </summary>
    public async Task<BeneficiaryTaxPosition> GetBeneficiaryTaxPositionAsync(Guid personAccountId, int fiscalYear)
    {
        var person = await _context.PersonAccounts
            .Include(p => p.PayrollIncomes.Where(pi => pi.FiscalYear == fiscalYear))
            .Include(p => p.Deductions.Where(d => d.FiscalYear == fiscalYear))
            .FirstOrDefaultAsync(p => p.Id == personAccountId);

        if (person == null)
        {
            return new BeneficiaryTaxPosition { PersonAccountId = personAccountId };
        }

        var grossIncome = person.PayrollIncomes.Sum(pi => pi.GrossSalary);
        var deductions = person.Deductions.Sum(d => d.Amount);
        var taxableIncome = Math.Max(0, grossIncome - deductions);

        return new BeneficiaryTaxPosition
        {
            PersonAccountId = personAccountId,
            PersonName = person.FullName,
            ExistingGrossIncome = grossIncome,
            TotalDeductions = deductions,
            TaxableIncomeBeforeDistribution = taxableIncome,
            MarginalTaxRate = CalculateMarginalTaxRate(taxableIncome),
            TaxPayableBeforeDistribution = CalculateTaxPayable(taxableIncome)
        };
    }

    public decimal CalculateMarginalTaxRate(decimal taxableIncome)
    {
        decimal rate = 0;
        foreach (var (threshold, r) in TaxBrackets)
        {
            if (taxableIncome <= threshold)
            {
                rate = r;
                break;
            }
        }

        // Add Medicare levy if above threshold
        if (taxableIncome >= MedicareLevyThreshold)
        {
            rate += MedicareLevyRate;
        }

        return rate;
    }

    public decimal CalculateTaxPayable(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0;

        decimal tax = 0;
        decimal previousThreshold = 0;

        foreach (var (threshold, rate) in TaxBrackets)
        {
            if (taxableIncome <= previousThreshold) break;

            var taxableInBracket = Math.Min(taxableIncome, threshold) - previousThreshold;
            tax += taxableInBracket * rate;
            previousThreshold = threshold;

            if (taxableIncome <= threshold) break;
        }

        // Add Medicare levy
        if (taxableIncome >= MedicareLevyThreshold)
        {
            // Simplified: fully applied if over threshold
            // In reality, there is a phase-in range, but for planning 2% is the standard assumption
            tax += taxableIncome * MedicareLevyRate;
        }

        return tax;
    }

    public async Task<TaxDistributionResult> OptimizeDistributionAsync(Guid trustAccountId, int fiscalYear)
    {
        var trust = await _context.TrustAccounts
            .Include(t => t.Beneficiaries.Where(b => b.IsEligible))
                .ThenInclude(b => b.PersonAccount)
            .Include(t => t.Beneficiaries.Where(b => b.IsEligible))
                .ThenInclude(b => b.CompanyAccount)
            .Include(t => t.TrustIncomes.Where(ti => ti.FiscalYear == fiscalYear))
            .FirstOrDefaultAsync(t => t.Id == trustAccountId);

        if (trust == null) throw new ArgumentException($"Trust account {trustAccountId} not found");

        var income = trust.TrustIncomes.FirstOrDefault();
        if (income == null)
        {
            return new TaxDistributionResult { TrustAccountId = trustAccountId, FiscalYear = fiscalYear, Message = "No income recorded" };
        }

        // 1. Prepare income buckets
        decimal remFranked = income.FrankedDividends;
        decimal remUnfranked = income.UnfrankedIncome + income.RentalIncome;
        decimal remDiscCG = income.DiscountCapitalGains;
        decimal remNormalCG = income.NonDiscountCapitalGains;
        decimal remFC = income.FrankingCredits;

        decimal totalIncome = remFranked + remUnfranked + remDiscCG + remNormalCG;

        var result = new TaxDistributionResult
        {
            TrustAccountId = trustAccountId,
            FiscalYear = fiscalYear,
            TotalDistributableIncome = totalIncome,
            TotalFrankingCredits = remFC
        };

        // 2. Prepare beneficiaries
        var beneficiaries = new List<BenInfo>();
        foreach (var b in trust.Beneficiaries.Where(eb => eb.IsEligible))
        {
            if (b.PersonAccountId.HasValue)
            {
                var pos = await GetBeneficiaryTaxPositionAsync(b.PersonAccountId.Value, fiscalYear);
                beneficiaries.Add(new BenInfo { Beneficiary = b, Position = pos, IsCompany = false });
            }
            else if (b.CompanyAccountId.HasValue)
            {
                var rate = b.CompanyAccount?.IsBaseRateEntity == true ? BaseRateEntityTaxRate : CompanyTaxRate;
                beneficiaries.Add(new BenInfo
                {
                    Beneficiary = b,
                    IsCompany = true,
                    Position = new BeneficiaryTaxPosition
                    {
                        PersonName = b.CompanyAccount?.CompanyName ?? b.BeneficiaryName,
                        MarginalTaxRate = rate
                    }
                });
            }
        }

        // 3. Optimization Loop
        // Strategy: Fill individuals up to specific thresholds (19%, 30%, 37%) then use companies or high-rate individuals
        var recs = beneficiaries.ToDictionary(b => b.Beneficiary.Id, b => new DistributionRecommendation
        {
            BeneficiaryId = b.Beneficiary.Id,
            BeneficiaryName = b.Position.PersonName,
            IsCompany = b.IsCompany,
            MarginalTaxRateBefore = b.Position.MarginalTaxRate,
            TaxableIncomeBeforeDistribution = b.Position.TaxableIncomeBeforeDistribution
        });

        // First pass: Fill individuals to the top of 30% bracket ($135k in FY25)
        // Actually, we use our unified brackets.
        DistributeIncome(recs, beneficiaries, ref remFranked, ref remUnfranked, ref remDiscCG, ref remNormalCG, ref remFC, 135000m);

        // Second pass: Fill individuals to the top of 37% bracket ($190k)
        DistributeIncome(recs, beneficiaries, ref remFranked, ref remUnfranked, ref remDiscCG, ref remNormalCG, ref remFC, 190000m);

        // Third pass: Distribute everything else (mostly to companies or high-rate individuals)
        DistributeIncome(recs, beneficiaries, ref remFranked, ref remUnfranked, ref remDiscCG, ref remNormalCG, ref remFC, decimal.MaxValue);

        // 4. Finalize Results
        foreach (var rec in recs.Values.Where(r => r.TotalDistribution > 0))
        {
            var bInfo = beneficiaries.First(b => b.Beneficiary.Id == rec.BeneficiaryId);
            var taxableAdded = rec.FrankedDividends + rec.UnfrankedIncome + (rec.DiscountCapitalGains * 0.5m) + rec.NonDiscountCapitalGains;

            rec.TaxableIncomeAfterDistribution = rec.TaxableIncomeBeforeDistribution + taxableAdded;
            rec.MarginalTaxRateAfter = CalculateMarginalTaxRate(rec.TaxableIncomeAfterDistribution);

            if (rec.IsCompany)
            {
                rec.MarginalTaxRateAfter = rec.MarginalTaxRateBefore; // Stays flat
                rec.EstimatedTaxOnDistribution = taxableAdded * rec.MarginalTaxRateAfter - rec.FrankingCredits;
            }
            else
            {
                rec.EstimatedTaxOnDistribution = CalculateTaxPayable(rec.TaxableIncomeAfterDistribution)
                                               - bInfo.Position.TaxPayableBeforeDistribution
                                               - rec.FrankingCredits;
            }

            result.Recommendations.Add(rec);
        }

        result.TotalEstimatedTax = result.Recommendations.Sum(r => r.EstimatedTaxOnDistribution);
        result.TotalFrankingCreditsUsed = result.Recommendations.Sum(r => r.FrankingCredits);
        result.Message = $"Income fully distributed across {result.Recommendations.Count} beneficiaries. Tax minimized via bracket filling.";

        return result;
    }

    private void DistributeIncome(
        Dictionary<Guid, DistributionRecommendation> recs,
        List<BenInfo> beneficiaries,
        ref decimal remFranked,
        ref decimal remUnfranked,
        ref decimal remDiscCG,
        ref decimal remNormalCG,
        ref decimal remFC,
        decimal targetThreshold)
    {
        // Sort individuals by lowest rate first
        // Companies sorted separately: if targetThreshold is high, we might use them
        var sorted = beneficiaries.OrderBy(b => b.IsCompany ? 0.30m : b.Position.MarginalTaxRate).ToList();

        foreach (var b in sorted)
        {
            if (remFranked <= 0 && remUnfranked <= 0 && remDiscCG <= 0 && remNormalCG <= 0) return;

            var rec = recs[b.Beneficiary.Id];
            var currentTaxable = b.IsCompany ? 0 : b.Position.TaxableIncomeBeforeDistribution +
                                (rec.FrankedDividends + rec.UnfrankedIncome + rec.DiscountCapitalGains * 0.5m + rec.NonDiscountCapitalGains);

            var capacity = b.IsCompany ? decimal.MaxValue : Math.Max(0, targetThreshold - currentTaxable);

            if (capacity <= 0 && !b.IsCompany) continue;

            // Distribute Franked first (maximizes credit usage)
            decimal f = Math.Min(remFranked, capacity);
            if (f > 0)
            {
                decimal credits = (f / remFranked) * remFC;
                rec.FrankedDividends += f;
                rec.FrankingCredits += credits;
                remFranked -= f;
                remFC -= credits;
                capacity -= f;
            }

            // Capital Gains (Discounted) - consume half capacity per unit of gain
            decimal dcg = Math.Min(remDiscCG, capacity * 2);
            if (dcg > 0)
            {
                rec.DiscountCapitalGains += dcg;
                remDiscCG -= dcg;
                capacity -= dcg * 0.5m;
            }

            // Normal CG
            decimal ncg = Math.Min(remNormalCG, capacity);
            if (ncg > 0)
            {
                rec.NonDiscountCapitalGains += ncg;
                remNormalCG -= ncg;
                capacity -= ncg;
            }

            // Unfranked
            decimal u = Math.Min(remUnfranked, capacity);
            if (u > 0)
            {
                rec.UnfrankedIncome += u;
                remUnfranked -= u;
                capacity -= u;
            }
        }
    }

    private class BenInfo
    {
        public TrustBeneficiary Beneficiary { get; set; } = null!;
        public BeneficiaryTaxPosition Position { get; set; } = null!;
        public bool IsCompany { get; set; }
    }
}

// Result DTOs
public class TaxDistributionResult
{
    public Guid TrustAccountId { get; set; }
    public int FiscalYear { get; set; }
    public decimal TotalDistributableIncome { get; set; }
    public decimal TotalFrankingCredits { get; set; }
    public decimal TotalFrankingCreditsUsed { get; set; }
    public decimal TotalEstimatedTax { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<DistributionRecommendation> Recommendations { get; set; } = new();
}

public class DistributionRecommendation
{
    public Guid BeneficiaryId { get; set; }
    public string BeneficiaryName { get; set; } = string.Empty;
    public bool IsCompany { get; set; }

    public decimal FrankedDividends { get; set; }
    public decimal UnfrankedIncome { get; set; }
    public decimal DiscountCapitalGains { get; set; }
    public decimal NonDiscountCapitalGains { get; set; }
    public decimal FrankingCredits { get; set; }

    public decimal TotalDistribution => FrankedDividends + UnfrankedIncome + DiscountCapitalGains + NonDiscountCapitalGains;

    public decimal TaxableIncomeBeforeDistribution { get; set; }
    public decimal TaxableIncomeAfterDistribution { get; set; }
    public decimal MarginalTaxRateBefore { get; set; }
    public decimal MarginalTaxRateAfter { get; set; }
    public decimal EstimatedTaxOnDistribution { get; set; }
}

public class BeneficiaryTaxPosition
{
    public Guid PersonAccountId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public bool IsCompany { get; set; }
    public decimal ExistingGrossIncome { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TaxableIncomeBeforeDistribution { get; set; }
    public decimal MarginalTaxRate { get; set; }
    public decimal TaxPayableBeforeDistribution { get; set; }
}
