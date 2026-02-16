namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Represents a potential property acquisition being analyzed
    /// </summary>
    public class PropertyDeal
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? PortfolioId { get; set; }

        // Basic Info
        public required string Name { get; set; }
        public string? Address { get; set; }
        public string? BuildingType { get; set; }
        public string? Description { get; set; }

        public Guid? AcquiredAssetId { get; set; } // Link to Portfolio Asset when acquired

        // Status: 
        // Acquisition: Draft, Under Review, Proceed to Acquire, Offer Made, Under Due Diligence, Waiting Settlement, Acquired
        // Management: Tenanted, New Tenancy Required
        // Disposal: Under Sale Review, On Market, Under Sale Due Diligence, Sold
        // Analysis: Pass, Uneconomic
        public string Status { get; set; } = "Draft";

        // Financial Inputs
        public decimal AskingPrice { get; set; }
        public decimal EstimatedValue { get; set; }
        public decimal StampDutyRate { get; set; } = 5.5m; // Default 5.5%
        public decimal LegalCosts { get; set; } = 15000m;
        public decimal BuyersAgentFeeRate { get; set; } // % of Asking Price
        public decimal CapExReserve { get; set; }

        // Income Projections
        public decimal EstimatedGrossRent { get; set; }
        public decimal VacancyRatePercent { get; set; } = 5m; // Default 5%
        public decimal ManagementFeePercent { get; set; } = 7m; // Default 7%
        public decimal OutgoingsEstimate { get; set; }

        // Financing
        public decimal LoanAmount { get; set; }
        public decimal InterestRatePercent { get; set; } = 6.5m;
        public int LoanTermYears { get; set; } = 25;

        // Uncertainty Ranges (for Monte Carlo)
        public decimal RentVariancePercent { get; set; } = 10m; // ±10%
        public decimal VacancyVariancePercent { get; set; } = 5m; // ±5%
        public decimal InterestVariancePercent { get; set; } = 1m; // ±1%
        public decimal CapitalGrowthPercent { get; set; } = 3m; // Base growth
        public decimal CapitalGrowthVariancePercent { get; set; } = 2m; // ±2%

        public decimal RentalGrowthPercent { get; set; } = 2.5m; // Default 2.5% inflation
        public decimal VacancyGrowthPercent { get; set; } = 0m;
        public decimal ManagementGrowthPercent { get; set; } = 0m;
        public decimal OutgoingsGrowthPercent { get; set; } = 0m;

        // Risk/Analysis Parameters
        public decimal DiscountRate { get; set; } = 8m; // Default 8% discount rate for DCF

        // Analysis Period
        public int HoldingPeriodYears { get; set; } = 10;

        // Decision
        public string? DecisionRationale { get; set; }
        public DateTime? DecisionDate { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<DealSimulationResult>? SimulationResults { get; set; }
        public ICollection<DealStatusHistory>? StatusHistory { get; set; }

        public string? SpreadsheetOverridesJson { get; set; }

        // Detailed Modeling Fields
        public string? LeaseDetailsJson { get; set; }
        public string? LoanDetailsJson { get; set; }

        // Risk Analysis Settings (Monte Carlo distributions and correlations)
        public string? DistributionSettingsJson { get; set; }
        public string? CorrelationMatrixJson { get; set; }
    }

    /// <summary>
    /// Stores results of Monte Carlo simulation runs
    /// </summary>
    public class DealSimulationResult
    {
        public Guid Id { get; set; }
        public Guid DealId { get; set; }

        // Run metadata
        public DateTime RunDate { get; set; } = DateTime.UtcNow;
        public int Iterations { get; set; }

        // NPV Results
        public decimal MedianNPV { get; set; }
        public decimal P10NPV { get; set; } // 10th percentile (downside)
        public decimal P90NPV { get; set; } // 90th percentile (upside)

        // IRR Results
        public decimal MedianIRR { get; set; }
        public decimal P10IRR { get; set; }
        public decimal P90IRR { get; set; }

        // Cap Rate
        public decimal CalculatedCapRate { get; set; }

        // Recommendation
        public string RecommendedDecision { get; set; } = "Analyzing"; // Buy, Pass, Uneconomic

        // Histogram data (JSON array of bucket counts)
        public string? NPVHistogramJson { get; set; }
        public string? IRRHistogramJson { get; set; }
        public string? YearlyDCFJson { get; set; } // Stores P10/P50/P90 yearly series

        // Snapshot of inputs used
        public string? InputsSnapshotJson { get; set; }

        // Navigation
        public PropertyDeal? Deal { get; set; }
    }
}
