using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Lease profile for forecasting and vacancy tracking
    /// </summary>
    public class LeaseProfile
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public CommercialProperty Property { get; set; } = null!;

        [Required]
        public required string TenantName { get; set; }

        /// <summary>
        /// Unit or area identifier within the property
        /// </summary>
        public string? UnitReference { get; set; }

        public DateTime LeaseStart { get; set; }
        public DateTime LeaseEnd { get; set; }

        /// <summary>
        /// Option period description (e.g., "5+5", "3+3+3")
        /// </summary>
        public string? OptionPeriod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentRent { get; set; }

        /// <summary>
        /// Rent review type: Fixed, CPI, Market
        /// </summary>
        [Required]
        public required string ReviewType { get; set; }

        /// <summary>
        /// Annual rent review percentage (e.g., 3.5 for 3.5%)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? ReviewPercentage { get; set; }

        /// <summary>
        /// Percentage of outgoings recoverable from tenant (0-100)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal OutgoingsRecoverable { get; set; } = 100;

        /// <summary>
        /// Gross lettable area for this lease in square meters
        /// </summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal? GLA { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
