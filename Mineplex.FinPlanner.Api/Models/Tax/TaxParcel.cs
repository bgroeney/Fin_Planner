using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class TaxParcel
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;
        public DateTime AcquisitionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostBase { get; set; } // Per unit

        [Column(TypeName = "decimal(18,6)")]
        public decimal Units { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal RemainingUnits { get; set; }
    }
}
