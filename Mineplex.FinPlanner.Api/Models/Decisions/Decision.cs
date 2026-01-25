using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Decision
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Portfolio Portfolio { get; set; } = null!;
        public string? Title { get; set; }
        public required string Type { get; set; } // AI, Manual
        public required string Status { get; set; } // Draft, Pending, Approved, Rejected, Implemented
        public string? Rationale { get; set; }

        [Column(TypeName = "jsonb")]
        public required string SnapshotBefore { get; set; }

        [Column(TypeName = "jsonb")]
        public required string SnapshotAfter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ProjectedTaxImpact { get; set; }

        public string? AllocationMethod { get; set; } // FIFO, MinTax, MaxGain, Specific

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ImplementedAt { get; set; }

        public Guid? FileUploadId { get; set; }
        public FileUpload? FileUpload { get; set; }
    }
}
