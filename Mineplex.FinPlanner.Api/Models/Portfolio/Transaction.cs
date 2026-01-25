using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Account Account { get; set; } = null!;
        public Guid AssetId { get; set; }
        public Asset Asset { get; set; } = null!;
        public TransactionType Type { get; set; }
        public string? ExternalReferenceId { get; set; } // For duplicate detection

        [Column(TypeName = "decimal(18,6)")]
        public decimal Units { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime EffectiveDate { get; set; }
        public int AttachedOrder { get; set; } // For sorting transactions on same day
        public required string Narration { get; set; }

        public Guid? FileUploadId { get; set; }
        public FileUpload? FileUpload { get; set; }
    }

    public enum TransactionType
    {
        Buy,
        Sell,
        Distribution,
        Dividend,
        Fee_Direct,
        Fee_Indirect,
        Deposit,
        Withdrawal
    }
}
