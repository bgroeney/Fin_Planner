using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Property ledger for granular cashflow tracking
    /// </summary>
    public class PropertyLedger
    {
        public Guid Id { get; set; }
        public Guid PropertyId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public CommercialProperty Property { get; set; } = null!;

        public DateTime Date { get; set; }

        /// <summary>
        /// Transaction type: Rent_Gross, Outgoing_Recoverable, Council_Rates, Water_Rates, 
        /// Insurance, Management_Fee, Repairs_CapEx, Other_Income, Other_Expense
        /// </summary>
        [Required]
        public required string Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Is this an income (positive) or expense (negative) entry
        /// </summary>
        public bool IsIncome { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Optional reference to source document
        /// </summary>
        public Guid? FileUploadId { get; set; }
        public FileUpload? FileUpload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
