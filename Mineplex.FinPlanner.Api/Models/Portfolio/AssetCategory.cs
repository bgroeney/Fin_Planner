using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class AssetCategory
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Portfolio Portfolio { get; set; } = null!;
        public required string Name { get; set; }
        public required string Code { get; set; }
        public decimal TargetPercentage { get; set; }
        public int DisplayOrder { get; set; }
    }
}
