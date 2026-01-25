using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class AIRecommendation
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;
        public string Action { get; set; } = string.Empty; // Buy, Sell, Hold
        public string Summary { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
