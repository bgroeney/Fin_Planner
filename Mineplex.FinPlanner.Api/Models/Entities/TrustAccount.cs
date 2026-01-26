namespace Mineplex.FinPlanner.Api.Models.Entities;

/// <summary>
/// Family Discretionary Trust for tax planning
/// </summary>
public class TrustAccount
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }

    public string TrustName { get; set; } = string.Empty;
    public string? ABN { get; set; }
    public string TrusteeName { get; set; } = string.Empty;

    /// <summary>
    /// Type: Discretionary, Unit, Hybrid
    /// </summary>
    public string TrustType { get; set; } = "Discretionary";

    public DateTime? TrustDeedDate { get; set; }

    /// <summary>
    /// Trust must distribute all income by June 30
    /// </summary>
    public bool MustDistributeByYearEnd { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Portfolio? Portfolio { get; set; }
    public ICollection<TrustBeneficiary> Beneficiaries { get; set; } = new List<TrustBeneficiary>();
    public ICollection<TrustDistribution> Distributions { get; set; } = new List<TrustDistribution>();
    public ICollection<TrustIncome> TrustIncomes { get; set; } = new List<TrustIncome>();
}

/// <summary>
/// Beneficiary of a trust - can be Person or Company
/// </summary>
public class TrustBeneficiary
{
    public Guid Id { get; set; }
    public Guid TrustAccountId { get; set; }

    /// <summary>
    /// Link to PersonAccount if beneficiary is an individual
    /// </summary>
    public Guid? PersonAccountId { get; set; }

    /// <summary>
    /// Link to CompanyAccount if beneficiary is a bucket company
    /// </summary>
    public Guid? CompanyAccountId { get; set; }

    /// <summary>
    /// Name for display (auto-populated from linked entity)
    /// </summary>
    public string BeneficiaryName { get; set; } = string.Empty;

    /// <summary>
    /// Whether currently eligible to receive distributions
    /// </summary>
    public bool IsEligible { get; set; } = true;

    /// <summary>
    /// Relationship to primary (e.g., "Spouse", "Child", "Parent")
    /// </summary>
    public string? Relationship { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TrustAccount? TrustAccount { get; set; }
    public PersonAccount? PersonAccount { get; set; }
    public CompanyAccount? CompanyAccount { get; set; }
}

/// <summary>
/// Trust income for a financial year (before distribution)
/// </summary>
public class TrustIncome
{
    public Guid Id { get; set; }
    public Guid TrustAccountId { get; set; }

    public int FiscalYear { get; set; }

    /// <summary>
    /// Franked dividend income received
    /// </summary>
    public decimal FrankedDividends { get; set; }

    /// <summary>
    /// Unfranked dividend and interest income
    /// </summary>
    public decimal UnfrankedIncome { get; set; }

    /// <summary>
    /// Capital gains eligible for 50% discount
    /// </summary>
    public decimal DiscountCapitalGains { get; set; }

    /// <summary>
    /// Capital gains not eligible for discount
    /// </summary>
    public decimal NonDiscountCapitalGains { get; set; }

    /// <summary>
    /// Rental income from property
    /// </summary>
    public decimal RentalIncome { get; set; }

    /// <summary>
    /// Total franking credits available
    /// </summary>
    public decimal FrankingCredits { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TrustAccount? TrustAccount { get; set; }
}

/// <summary>
/// Distribution from trust to beneficiary for a fiscal year
/// </summary>
public class TrustDistribution
{
    public Guid Id { get; set; }
    public Guid TrustAccountId { get; set; }
    public Guid BeneficiaryId { get; set; }

    public int FiscalYear { get; set; }

    /// <summary>
    /// Date when distribution resolution was made (must be before June 30)
    /// </summary>
    public DateTime ResolutionDate { get; set; }

    /// <summary>
    /// Franked dividends distributed to this beneficiary
    /// </summary>
    public decimal FrankedDividends { get; set; }

    /// <summary>
    /// Unfranked income distributed
    /// </summary>
    public decimal UnfrankedIncome { get; set; }

    /// <summary>
    /// Discount capital gains distributed
    /// </summary>
    public decimal DiscountCapitalGains { get; set; }

    /// <summary>
    /// Non-discount capital gains distributed
    /// </summary>
    public decimal NonDiscountCapitalGains { get; set; }

    /// <summary>
    /// Franking credits attached to distribution
    /// </summary>
    public decimal FrankingCredits { get; set; }

    /// <summary>
    /// Status: Draft, Resolved, Paid
    /// </summary>
    public string Status { get; set; } = "Draft";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public TrustAccount? TrustAccount { get; set; }
    public TrustBeneficiary? Beneficiary { get; set; }
}
