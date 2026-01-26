namespace Mineplex.FinPlanner.Api.Models.Entities;

/// <summary>
/// Represents an individual person within a portfolio structure for tax planning
/// </summary>
public class PersonAccount
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }

    /// <summary>
    /// Encrypted Tax File Number (TFN)
    /// </summary>
    public string? TaxFileNumber { get; set; }

    /// <summary>
    /// Current marginal tax rate (0.0 to 0.47)
    /// </summary>
    public decimal MarginalTaxRate { get; set; }

    /// <summary>
    /// Medicare levy rate (typically 0.02)
    /// </summary>
    public decimal MedicareLevyRate { get; set; } = 0.02m;

    /// <summary>
    /// Whether liable for Medicare Levy Surcharge
    /// </summary>
    public bool HasPrivateHealthInsurance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Portfolio? Portfolio { get; set; }
    public ICollection<SuperAccount> SuperAccounts { get; set; } = new List<SuperAccount>();
    public ICollection<PayrollIncome> PayrollIncomes { get; set; } = new List<PayrollIncome>();
    public ICollection<Deduction> Deductions { get; set; } = new List<Deduction>();
}

/// <summary>
/// Superannuation account linked to a person
/// </summary>
public class SuperAccount
{
    public Guid Id { get; set; }
    public Guid PersonAccountId { get; set; }

    public string FundName { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Investment option (e.g., "Growth", "Balanced", "Conservative")
    /// </summary>
    public string InvestmentOption { get; set; } = "Balanced";

    /// <summary>
    /// Date when preservation age is reached
    /// </summary>
    public DateTime PreservationDate { get; set; }

    // Current FY Contributions
    public decimal EmployerContributionYTD { get; set; }
    public decimal SalarySacrificeYTD { get; set; }
    public decimal PersonalContributionYTD { get; set; }

    /// <summary>
    /// Concessional cap remaining (typically $27,500)
    /// </summary>
    public decimal ConcessionalCapRemaining { get; set; } = 27500m;

    /// <summary>
    /// Non-concessional cap remaining (typically $110,000)
    /// </summary>
    public decimal NonConcessionalCapRemaining { get; set; } = 110000m;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PersonAccount? PersonAccount { get; set; }
}

/// <summary>
/// Employment income record for a financial year
/// </summary>
public class PayrollIncome
{
    public Guid Id { get; set; }
    public Guid PersonAccountId { get; set; }

    /// <summary>
    /// Australian Financial Year (e.g., 2025 for FY24-25)
    /// </summary>
    public int FiscalYear { get; set; }

    public string Employer { get; set; } = string.Empty;

    /// <summary>
    /// Total gross salary including super
    /// </summary>
    public decimal GrossSalary { get; set; }

    /// <summary>
    /// PAYG tax withheld
    /// </summary>
    public decimal TaxWithheld { get; set; }

    /// <summary>
    /// Super Guarantee contribution
    /// </summary>
    public decimal SuperContribution { get; set; }

    /// <summary>
    /// Additional salary sacrifice amount
    /// </summary>
    public decimal SalarySacrifice { get; set; }

    /// <summary>
    /// Reportable Fringe Benefits
    /// </summary>
    public decimal ReportableFringeBenefits { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PersonAccount? PersonAccount { get; set; }
}

/// <summary>
/// Tax deduction for a financial year
/// </summary>
public class Deduction
{
    public Guid Id { get; set; }
    public Guid PersonAccountId { get; set; }

    public int FiscalYear { get; set; }

    /// <summary>
    /// Category: WorkRelated, Investment, Donation, HealthInsurance, Other
    /// </summary>
    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this is an estimate or actual receipted amount
    /// </summary>
    public bool IsEstimate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public PersonAccount? PersonAccount { get; set; }
}
