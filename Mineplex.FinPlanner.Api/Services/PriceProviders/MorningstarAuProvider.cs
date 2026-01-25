using Mineplex.FinPlanner.Api.Models;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using System.Text.Json;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    /// <summary>
    /// Morningstar Australia price provider for Australian managed funds using APIR codes.
    /// Scrapes unit prices from morningstar.com.au fund pages.
    /// </summary>
    public class MorningstarAuProvider : IPriceSourceProvider
    {
        private readonly ILogger<MorningstarAuProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAIService _aiService;

        // APIR codes are 9 characters: 3 letters + 4 digits + AU
        private static readonly Regex ApirCodePattern = new(@"^[A-Z]{3}\d{4}AU$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex patterns for extracting prices from Morningstar HTML
        private static readonly Regex EntryPricePattern = new(@"Entry\s*(?:Price)?[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExitPricePattern = new(@"Exit\s*(?:Price)?[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UnitPricePattern = new(@"Unit\s*(?:Price)?[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NavPricePattern = new(@"NAV[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string ProviderCode => "MORNINGSTAR_AU";

        public MorningstarAuProvider(
            ILogger<MorningstarAuProvider> logger,
            IHttpClientFactory httpClientFactory,
            IAIService aiService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _aiService = aiService;
        }

        public async Task<PriceResult> GetCurrentPriceAsync(Asset asset, string? apiKey = null)
        {
            var result = new PriceResult();

            if (!SupportsAsset(asset))
            {
                return result;
            }

            var apirCode = asset.Symbol.ToUpper();

            try
            {
                var price = await FetchPriceFromMorningstarAsync(apirCode);
                if (price.HasValue)
                {
                    result.Price = price.Value;
                    _logger.LogInformation("Morningstar AU: Retrieved price {Price} for APIR {Code}", price.Value, apirCode);
                    return result;
                }

                // Try alternative: direct fund manager websites via AI lookup
                _logger.LogDebug("Morningstar AU: No price found for {Code}, trying AI lookup...", apirCode);
                var aiSymbol = await _aiService.LookupSymbolAsync(asset.Name, "AU");
                if (!string.IsNullOrEmpty(aiSymbol) && aiSymbol != apirCode)
                {
                    result.SuggestedSymbol = aiSymbol;
                    _logger.LogInformation("Morningstar AU: AI suggested alternative symbol {Symbol} for {Name}", aiSymbol, asset.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Morningstar AU: Failed to fetch price for APIR {Code}", apirCode);
            }

            return result;
        }

        public async Task<Dictionary<Guid, PriceResult>> GetBatchPricesAsync(IEnumerable<Asset> assets, string? apiKey = null)
        {
            var results = new Dictionary<Guid, PriceResult>();
            var supportedAssets = assets.Where(a => SupportsAsset(a)).ToList();

            if (!supportedAssets.Any()) return results;

            // Morningstar doesn't have batch API, process individually with rate limiting
            foreach (var asset in supportedAssets)
            {
                try
                {
                    var priceResult = await GetCurrentPriceAsync(asset, apiKey);
                    if (priceResult.Price.HasValue)
                    {
                        results[asset.Id] = priceResult;
                    }

                    // Rate limiting: 500ms delay between requests
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Morningstar AU: Batch item failed for {Symbol}", asset.Symbol);
                }
            }

            return results;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            // Morningstar AU doesn't provide free historical price API
            // For managed funds, historical data is typically only available through paid services
            _logger.LogDebug("Morningstar AU: Historical prices not available for APIR {Code}", asset.Symbol);
            return new List<HistoricalPriceDto>();
        }

        public bool SupportsAsset(Asset asset)
        {
            if (string.IsNullOrWhiteSpace(asset.Symbol)) return false;

            // Check if it matches APIR code pattern
            if (ApirCodePattern.IsMatch(asset.Symbol)) return true;

            // Also support assets explicitly marked as ManagedFund with AU market
            if (asset.AssetType?.ToLower() == "managedfund" &&
                (asset.Market?.ToUpper() == "AU" || asset.Market?.ToUpper() == "ASX"))
            {
                return true;
            }

            return false;
        }

        private async Task<decimal?> FetchPriceFromMorningstarAsync(string apirCode)
        {
            var url = $"https://www.morningstar.com.au/Funds/FundReport/{apirCode}";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Morningstar AU: HTTP {Status} for {Code}", response.StatusCode, apirCode);
                return null;
            }

            var html = await response.Content.ReadAsStringAsync();

            // Try to extract price using various patterns
            var price = TryExtractPrice(html, EntryPricePattern) ??
                       TryExtractPrice(html, UnitPricePattern) ??
                       TryExtractPrice(html, NavPricePattern) ??
                       TryExtractPrice(html, ExitPricePattern);

            if (price.HasValue)
            {
                _logger.LogDebug("Morningstar AU: Extracted price {Price} for {Code}", price.Value, apirCode);
            }
            else
            {
                _logger.LogDebug("Morningstar AU: Could not extract price from HTML for {Code}", apirCode);
            }

            return price;
        }

        private decimal? TryExtractPrice(string html, Regex pattern)
        {
            var match = pattern.Match(html);
            if (match.Success && match.Groups.Count > 1)
            {
                var priceStr = match.Groups[1].Value.Replace(",", "");
                if (decimal.TryParse(priceStr, out var price) && price > 0)
                {
                    return price;
                }
            }
            return null;
        }
    }
}
