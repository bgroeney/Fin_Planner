using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface ICommercialPropertyService
    {
        Task<PropertyDashboardSummary> GetDashboardSummaryAsync(Guid userId);
    }
}
