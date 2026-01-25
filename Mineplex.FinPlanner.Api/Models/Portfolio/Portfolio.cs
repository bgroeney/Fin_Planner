using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Portfolio
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public User Owner { get; set; } = null!;
        public required string Name { get; set; }
        [Column(TypeName = "jsonb")]
        public required string TargetAllocation { get; set; } // JSON

        public Guid? BenchmarkAssetId { get; set; }
        public Asset? BenchmarkAsset { get; set; }
    }
}
