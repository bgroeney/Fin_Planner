using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Tracks all file uploads for auditing, rollback, and re-download
    /// </summary>
    public class FileUpload
    {
        public Guid Id { get; set; }

        public Guid PortfolioId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Portfolio Portfolio { get; set; } = null!;

        public Guid UploadedByUserId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public User? UploadedBy { get; set; }

        [Required]
        public required string FileName { get; set; }

        [Required]
        public required string FileType { get; set; } // "PortfolioValuation", "TransactionListing"

        [Required]
        public required string StoragePath { get; set; } // Relative path to stored file

        public long FileSizeBytes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date of the valuation snapshot (extracted from file content)
        /// </summary>
        public DateTime? ValuationDate { get; set; }

        /// <summary>
        /// If false, the import has been "unloaded" - data removed but file kept
        /// </summary>
        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }

        /// <summary>
        /// Number of records processed from this file
        /// </summary>
        public int RecordsProcessed { get; set; }

        /// <summary>
        /// Account associated with this import (if any)
        /// </summary>
        public Guid? AccountId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Account? Account { get; set; }

        /// <summary>
        /// Property Deal associated with this file (if any)
        /// </summary>
        public Guid? PropertyDealId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public PropertyDeal? PropertyDeal { get; set; }

        /// <summary>
        /// Comma-separated tags for categorization (e.g. "Contract", "Due Diligence")
        /// </summary>
        public string? Tags { get; set; }
    }

    /// <summary>
    /// Stores prices extracted from valuation files as a fallback price source
    /// </summary>
    public class ImportedAssetPrice
    {
        public Guid Id { get; set; }

        public Guid FileUploadId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public FileUpload FileUpload { get; set; } = null!;

        public Guid AssetId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Asset Asset { get; set; } = null!;

        /// <summary>
        /// Calculated unit price (TotalValue / Units)
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Units held at time of valuation
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal Units { get; set; }

        /// <summary>
        /// Total value from the valuation file
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalValue { get; set; }

        /// <summary>
        /// Date of the valuation
        /// </summary>
        public DateTime ValuationDate { get; set; }

        /// <summary>
        /// When this price record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// DTO for file upload history list
    /// </summary>
    public class FileUploadDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ValuationDate { get; set; }
        public bool IsActive { get; set; }
        public int RecordsProcessed { get; set; }
        public string? AccountName { get; set; }
    }
}
