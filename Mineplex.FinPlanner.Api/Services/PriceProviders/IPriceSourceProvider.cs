using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    public class PriceResult
    {
        public decimal? Price { get; set; }
        public string? SuggestedSymbol { get; set; }
    }

    /// <summary>
    /// Common interface for all market price data providers
    /// </summary>
    public interface IPriceSourceProvider
    {
        /// <summary>
        /// Unique code identifying this provider (e.g., "YAHOO", "ALPHAVANTAGE")
        /// </summary>
        string ProviderCode { get; }

        /// <summary>
        /// Get current price for a single asset
        /// </summary>
        Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null);

        /// <summary>
        /// Get current prices for multiple assets in a batch
        /// </summary>
        Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null);

        /// <summary>
        /// Get historical prices for an asset within a date range
        /// </summary>
        Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null);

        /// <summary>
        /// Check if this provider supports a given asset
        /// </summary>
        bool SupportsAsset(Asset asset);
    }
}
