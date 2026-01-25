using System;

namespace Mineplex.FinPlanner.Api.Models
{
    public class CreateLeaseRequest
    {
        public required string TenantName { get; set; }
        public string? UnitReference { get; set; }
        public DateTime LeaseStart { get; set; }
        public DateTime LeaseEnd { get; set; }
        public string? OptionPeriod { get; set; }
        public decimal CurrentRent { get; set; }
        public required string ReviewType { get; set; }
        public decimal? ReviewPercentage { get; set; }
        public decimal OutgoingsRecoverable { get; set; } = 100;
        public decimal? GLA { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLeaseRequest
    {
        public string? TenantName { get; set; }
        public string? UnitReference { get; set; }
        public DateTime? LeaseStart { get; set; }
        public DateTime? LeaseEnd { get; set; }
        public string? OptionPeriod { get; set; }
        public decimal? CurrentRent { get; set; }
        public string? ReviewType { get; set; }
        public decimal? ReviewPercentage { get; set; }
        public decimal? OutgoingsRecoverable { get; set; }
        public decimal? GLA { get; set; }
        public bool? IsActive { get; set; }
        public string? Notes { get; set; }
    }
}
