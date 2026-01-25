using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    /// <summary>
    /// Stores the latest available price for each asset
    /// </summary>
    public class CurrentPrice
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Asset Asset { get; set; } = null!;

        [Column(TypeName = "decimal(18,6)")]
        public decimal Price { get; set; }

        public DateTime LastUpdated { get; set; }

        public required string SourceUsed { get; set; } // e.g., "YAHOO", "ALPHAVANTAGE"
    }

    /// <summary>
    /// Stores daily market close prices for trend graphs
    /// </summary>
    public class HistoricalPrice
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Asset Asset { get; set; } = null!;

        [Column(TypeName = "date")]
        public DateTime Date { get; set; } // Date only, no time component

        [Column(TypeName = "decimal(18,6)")]
        public decimal ClosePrice { get; set; }
    }

    /// <summary>
    /// Configuration for price data providers
    /// </summary>
    public class PriceSource
    {
        public Guid Id { get; set; }

        public required string Name { get; set; } // e.g., "Yahoo Finance"

        public required string Code { get; set; } // e.g., "YAHOO", "ALPHAVANTAGE"

        public int Priority { get; set; } // Lower number = higher priority

        public bool IsEnabled { get; set; }

        public string? ApiKey { get; set; } // Nullable for free sources

        public int RateLimitPerMinute { get; set; }

        public string? ConfigurationJson { get; set; } // Additional provider-specific config
    }

    /// <summary>
    /// Asset-specific price source overrides
    /// Allows manual configuration when default sources don't work
    /// </summary>
    public class AssetPriceSourceOverride
    {
        public Guid Id { get; set; }

        public Guid AssetId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public Asset Asset { get; set; } = null!;

        public Guid? PriceSourceId { get; set; }
        public PriceSource? PriceSource { get; set; }

        public bool IsPreferred { get; set; } // If true, use this source first

        public string? CustomSymbol { get; set; } // Custom symbol to use for this source (e.g., "CBA.AX" instead of"CBA")
        public string? SuggestedSymbol { get; set; } // Symbol suggested by AI (passed back to Admin UI)

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Application-wide system settings
    /// </summary>
    public class SystemSetting
    {
        [Key]
        public required string Key { get; set; } // e.g., "PriceUpdateIntervalMinutes"

        public required string Value { get; set; }

        public string? Description { get; set; }

        public DateTime LastModified { get; set; }
    }
}
