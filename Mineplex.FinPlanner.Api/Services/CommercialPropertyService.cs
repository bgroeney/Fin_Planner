using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public class CommercialPropertyService : ICommercialPropertyService
    {
        private readonly FinPlannerDbContext _db;

        public CommercialPropertyService(FinPlannerDbContext db)
        {
            _db = db;
        }

        public async Task<PropertyDashboardSummary> GetDashboardSummaryAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            var twelveMonthsAhead = now.AddMonths(12);
            var twentyFourMonthsAhead = now.AddMonths(24);

            // Get all properties with related data
            var properties = await _db.CommercialProperties
                .Where(p => p.OwnerId == userId)
                .Include(p => p.Valuations.OrderByDescending(v => v.Date).Take(1))
                .Include(p => p.Leases.Where(l => l.IsActive))
                .Include(p => p.LedgerEntries.Where(l => l.Date >= now.AddYears(-1)))
                .ToListAsync();

            decimal totalValue = 0;
            decimal totalPurchasePrice = 0;
            decimal grossRentalIncome = 0;
            decimal totalExpenses = 0;
            int activeLeases = 0;
            int expiringLeases = 0;
            var propertySummaries = new List<PropertySummaryDto>();
            var vacancyData = new List<VacancyDataPoint>();

            foreach (var property in properties)
            {
                var latestValuation = property.Valuations?.FirstOrDefault();
                var currentValue = latestValuation?.Value ?? property.PurchasePrice;
                totalValue += currentValue;
                totalPurchasePrice += property.PurchasePrice;

                // Calculate income and expenses from ledger (last 12 months)
                var ledgerEntries = property.LedgerEntries ?? Enumerable.Empty<PropertyLedger>();
                var leases = property.Leases ?? Enumerable.Empty<LeaseProfile>();

                var propertyIncome = ledgerEntries
                    .Where(l => l.IsIncome)
                    .Sum(l => l.Amount);
                var propertyExpenses = ledgerEntries
                    .Where(l => !l.IsIncome)
                    .Sum(l => l.Amount);

                grossRentalIncome += propertyIncome;
                totalExpenses += propertyExpenses;

                // Count leases
                var propertyActiveLeases = leases.Count(l => l.IsActive);
                var propertyExpiringLeases = leases.Count(l => l.IsActive && l.LeaseEnd <= twelveMonthsAhead);
                activeLeases += propertyActiveLeases;
                expiringLeases += propertyExpiringLeases;

                // Calculate property net yield
                var propertyNetIncome = propertyIncome - propertyExpenses;
                var propertyNetYield = currentValue > 0 ? (propertyNetIncome / currentValue) * 100 : 0;

                propertySummaries.Add(new PropertySummaryDto
                {
                    Id = property.Id,
                    Address = property.Address,
                    BuildingType = property.BuildingType,
                    CurrentValue = currentValue,
                    NetYieldPercent = propertyNetYield,
                    LeaseCount = propertyActiveLeases,
                    NextLeaseExpiry = leases
                        .Where(l => l.IsActive)
                        .OrderBy(l => l.LeaseEnd)
                        .FirstOrDefault()?.LeaseEnd
                });

                // Build vacancy heatmap data
                foreach (var lease in leases.Where(l => l.IsActive && l.LeaseEnd <= twentyFourMonthsAhead))
                {
                    vacancyData.Add(new VacancyDataPoint
                    {
                        Month = new DateTime(lease.LeaseEnd.Year, lease.LeaseEnd.Month, 1),
                        PropertyAddress = property.Address,
                        PropertyId = property.Id,
                        ExpiringRent = lease.CurrentRent,
                        TenantName = lease.TenantName
                    });
                }
            }

            var netIncome = grossRentalIncome - totalExpenses;
            var netYieldPercent = totalValue > 0 ? (netIncome / totalValue) * 100 : 0;

            // Generate 12-month cashflow forecast
            var cashflowForecast = new List<CashflowProjection>();
            for (int i = 0; i < 12; i++)
            {
                var month = new DateTime(now.Year, now.Month, 1).AddMonths(i);

                // Project income from active leases
                decimal projectedIncome = 0;
                decimal projectedExpenses = 0;

                foreach (var property in properties)
                {
                    var leases = property.Leases ?? Enumerable.Empty<LeaseProfile>();
                    var ledgerEntries = property.LedgerEntries ?? Enumerable.Empty<PropertyLedger>();

                    foreach (var lease in leases.Where(l => l.IsActive && l.LeaseEnd >= month))
                    {
                        projectedIncome += lease.CurrentRent / 12; // Monthly rent
                    }

                    // Estimate expenses based on historical average
                    var expenseEntries = ledgerEntries.Where(l => !l.IsIncome).ToList();
                    var avgMonthlyExpenses = expenseEntries.Count > 0
                        ? expenseEntries.Average(l => l.Amount) / 12
                        : 0;
                    projectedExpenses += avgMonthlyExpenses;
                }

                cashflowForecast.Add(new CashflowProjection
                {
                    Month = month,
                    ProjectedIncome = projectedIncome,
                    ProjectedExpenses = projectedExpenses,
                    NetCashflow = projectedIncome - projectedExpenses
                });
            }

            return new PropertyDashboardSummary
            {
                TotalValue = totalValue,
                TotalPurchasePrice = totalPurchasePrice,
                NetYieldPercent = netYieldPercent,
                GrossRentalIncome = grossRentalIncome,
                TotalExpenses = totalExpenses,
                NetIncome = netIncome,
                TotalProperties = properties.Count,
                ActiveLeases = activeLeases,
                ExpiringLeases = expiringLeases,
                Properties = propertySummaries,
                VacancyHeatmap = vacancyData.OrderBy(v => v.Month).ToList(),
                CashflowForecast = cashflowForecast
            };
        }
    }
}
