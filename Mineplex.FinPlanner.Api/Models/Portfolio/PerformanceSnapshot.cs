using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class PerformanceSnapshot
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalValue { get; set; }

        [Column(TypeName = "jsonb")]
        public required string AllocationBreakdown { get; set; }
    }
}
