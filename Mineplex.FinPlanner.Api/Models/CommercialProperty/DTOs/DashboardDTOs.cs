using System;
using System.Collections.Generic;

namespace Mineplex.FinPlanner.Api.Models
{
    public class PropertyDashboardSummary
    {
        public decimal TotalValue { get; set; }
        public decimal TotalPurchasePrice { get; set; }
        public decimal NetYieldPercent { get; set; }
        public decimal GrossRentalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetIncome { get; set; }
        public int TotalProperties { get; set; }
        public int ActiveLeases { get; set; }
        public int ExpiringLeases { get; set; } // Expiring within 12 months
        public List<PropertySummaryDto> Properties { get; set; } = new();
        public List<VacancyDataPoint> VacancyHeatmap { get; set; } = new();
        public List<CashflowProjection> CashflowForecast { get; set; } = new();
    }

    public class VacancyDataPoint
    {
        public DateTime Month { get; set; }
        public required string PropertyAddress { get; set; }
        public Guid PropertyId { get; set; }
        public decimal ExpiringRent { get; set; }
        public required string TenantName { get; set; }
    }

    public class CashflowProjection
    {
        public DateTime Month { get; set; }
        public decimal ProjectedIncome { get; set; }
        public decimal ProjectedExpenses { get; set; }
        public decimal NetCashflow { get; set; }
    }
}
