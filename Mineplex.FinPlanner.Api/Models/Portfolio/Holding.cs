using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Holding
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Account Account { get; set; } = null!;
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;
        public Guid? CategoryId { get; set; }
        public AssetCategory? Category { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Units { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AvgCost { get; set; }

        [NotMapped]
        public decimal CurrentValue { get; set; } // Calculated
    }
}
