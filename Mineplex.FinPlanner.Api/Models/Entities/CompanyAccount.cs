namespace Mineplex.FinPlanner.Api.Models.Entities;

/// <summary>
/// Bucket company for retaining profits and managing Division 7A loans
/// </summary>
public class CompanyAccount
{
    public Guid Id { get; set; }
    public Guid PortfolioId { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string? ABN { get; set; }
    public string? ACN { get; set; }

    /// <summary>
    /// Accumulated retained profits after tax
    /// </summary>
    public decimal RetainedProfits { get; set; }

    /// <summary>
    /// Franking account balance for dividend payments
    /// </summary>
    public decimal FrankingAccountBalance { get; set; }

    /// <summary>
    /// Corporate tax rate (25% for base rate entities, 30% otherwise)
    /// </summary>
    public decimal TaxRate { get; set; } = 0.30m;

    /// <summary>
    /// Whether company qualifies as base rate entity (lower tax rate)
    /// </summary>
    public bool IsBaseRateEntity { get; set; } = true;

    public DateTime IncorporationDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Portfolio? Portfolio { get; set; }
    public ICollection<Division7ALoan> Division7ALoans { get; set; } = new List<Division7ALoan>();
    public ICollection<CompanyDividend> Dividends { get; set; } = new List<CompanyDividend>();
}

/// <summary>
/// Division 7A compliant loan from company to shareholder/associate
/// </summary>
public class Division7ALoan
{
    public Guid Id { get; set; }
    public Guid CompanyAccountId { get; set; }

    /// <summary>
    /// PersonAccountId of borrower
    /// </summary>
    public Guid BorrowerId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    /// <summary>
    /// Original loan principal
    /// </summary>
    public decimal PrincipalAmount { get; set; }

    /// <summary>
    /// Current outstanding balance
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Interest rate (must be at least RBA benchmark rate)
    /// </summary>
    public decimal InterestRate { get; set; }

    public DateTime LoanDate { get; set; }

    /// <summary>
    /// Maximum loan term (7 years unsecured, 25 years secured by property)
    /// </summary>
    public int LoanTermYears { get; set; } = 7;

    /// <summary>
    /// Whether loan is secured by real property
    /// </summary>
    public bool IsSecured { get; set; }

    /// <summary>
    /// Minimum yearly repayment to avoid deemed dividend
    /// </summary>
    public decimal MinimumYearlyRepayment { get; set; }

    /// <summary>
    /// Amount repaid in current FY
    /// </summary>
    public decimal RepaidThisYear { get; set; }

    /// <summary>
    /// Status: Active, PaidOut, Forgiven, Deemed
    /// </summary>
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CompanyAccount? CompanyAccount { get; set; }
    public PersonAccount? Borrower { get; set; }
}

/// <summary>
/// Dividend declared by company
/// </summary>
public class CompanyDividend
{
    public Guid Id { get; set; }
    public Guid CompanyAccountId { get; set; }

    public DateTime DeclarationDate { get; set; }
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Total dividend amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Franking percentage (0-100)
    /// </summary>
    public decimal FrankingPercentage { get; set; } = 100m;

    /// <summary>
    /// Franking credits attached
    /// </summary>
    public decimal FrankingCredits { get; set; }

    /// <summary>
    /// Status: Declared, Paid
    /// </summary>
    public string Status { get; set; } = "Declared";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CompanyAccount? CompanyAccount { get; set; }
}
