using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mineplex.FinPlanner.Api.Models
{
    public class Asset
    {
        public Guid Id { get; set; }
        public required string Symbol { get; set; }
        public required string Name { get; set; }
        public required string AssetType { get; set; } // ETF, ManagedFund, TD, Cash
        public string? Market { get; set; } // ASX, US, etc.

        // Navigation properties
        public CurrentPrice? CurrentPrice { get; set; }
    }
}
