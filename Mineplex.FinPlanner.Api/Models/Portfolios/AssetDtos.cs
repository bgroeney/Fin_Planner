using System;
using System.Collections.Generic;

namespace Mineplex.FinPlanner.Api.Models.Portfolios
{
    public class AssetDetailDto
    {
        public Guid AssetId { get; set; }
        public Guid? CategoryId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public double AnnualisedReturn { get; set; }
        public PositionSummaryDto Position { get; set; } = new();
        public List<Services.HistoricalPriceDto> History { get; set; } = new();
        // Transactions removed for lazy loading
    }

    public class PositionSummaryDto
    {
        public decimal Units { get; set; }
        public decimal AvgCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ReturnPercent { get; set; }
    }

    public class AssetTransactionDto
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // Buy/Sell
        public decimal Units { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public decimal Value { get; set; }
        public decimal ProfitLoss { get; set; }
    }

    public class UpdateAssetDto
    {
        public string? Name { get; set; }
        public string? Sector { get; set; }
    }

    public class UpdateHoldingCategoryDto
    {
        public Guid CategoryId { get; set; }
    }

    public class HoldingDto
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public Guid AssetId { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Units { get; set; }
        public decimal AvgCost { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal CurrentValue { get; set; }
    }
}
