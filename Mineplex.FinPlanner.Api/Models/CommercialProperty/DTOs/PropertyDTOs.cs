using System;

namespace Mineplex.FinPlanner.Api.Models
{
    public class CreatePropertyRequest
    {
        public required string Address { get; set; }
        public string? TitleReference { get; set; }
        public decimal TotalGFA { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public required string BuildingType { get; set; }
        public string? Description { get; set; }
    }

    public class UpdatePropertyRequest
    {
        public string? Address { get; set; }
        public string? TitleReference { get; set; }
        public decimal? TotalGFA { get; set; }
        public string? BuildingType { get; set; }
        public string? Description { get; set; }
    }

    public class PropertySummaryDto
    {
        public Guid Id { get; set; }
        public required string Address { get; set; }
        public required string BuildingType { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal NetYieldPercent { get; set; }
        public int LeaseCount { get; set; }
        public DateTime? NextLeaseExpiry { get; set; }
    }
}
