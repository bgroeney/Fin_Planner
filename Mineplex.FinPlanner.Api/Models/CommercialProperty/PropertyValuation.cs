using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Property valuation history
    /// </summary>
    public class PropertyValuation
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public CommercialProperty Property { get; set; } = null!;

        public DateTime Date { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        /// <summary>
        /// Source: Agent, External, Internal
        /// </summary>
        [Required]
        public required string Source { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
