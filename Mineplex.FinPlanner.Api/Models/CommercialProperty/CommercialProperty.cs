using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Commercial property entity - independent from main Assets table
    /// </summary>
    public class CommercialProperty
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public User Owner { get; set; } = null!;

        [Required]
        public required string Address { get; set; }

        public string? TitleReference { get; set; }

        /// <summary>
        /// Total Gross Floor Area in square meters
        /// </summary>
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalGFA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchasePrice { get; set; }

        public DateTime PurchaseDate { get; set; }

        /// <summary>
        /// Building type: Retail, Office, Industrial, Mixed
        /// </summary>
        [Required]
        public required string BuildingType { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional link to a proxy Asset for portfolio integration
        /// </summary>
        public Guid? ProxyAssetId { get; set; }
        public Asset? ProxyAsset { get; set; }

        // Navigation properties
        public ICollection<PropertyValuation> Valuations { get; set; } = new List<PropertyValuation>();
        public ICollection<PropertyLedger> LedgerEntries { get; set; } = new List<PropertyLedger>();
        public ICollection<LeaseProfile> Leases { get; set; } = new List<LeaseProfile>();
    }
}
