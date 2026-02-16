using Mineplex.FinPlanner.Api.Models;
using System.Text.RegularExpressions;
using PuppeteerSharp;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace Mineplex.FinPlanner.Api.Services.PriceProviders
{
    /// <summary>
    /// Morningstar Australia price provider for Australian managed funds using APIR codes.
    /// Uses PuppeteerSharp to scrape fund prices from the Morningstar AU website.
    /// 
    /// Strategy: Navigate directly to Morningstar fund page, wait for SPA to render, extract price.
    /// Falls back to search if direct URL doesn't work.
    /// </summary>
    public class MorningstarAuProvider : IPriceSourceProvider
    {
        private readonly ILogger<MorningstarAuProvider> _logger;
        private readonly IAIService _aiService;

        // APIR codes are 9 characters: 3 letters + 4 digits + AU
        private static readonly Regex ApirCodePattern = new(@"^[A-Z]{3}\d{4}AU$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex patterns for extracting prices from rendered page text
        private static readonly Regex EntryPricePattern = new(@"Entry\s*(?:Price)?[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExitPricePattern = new(@"Exit\s*(?:Price)?[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UnitPricePattern = new(@"Unit\s*(?:Price)?[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NavPricePattern = new(@"NAV[:\s]*\$?\s*([\d,]+\.?\d*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static bool _browserDownloaded = false;
        private static readonly SemaphoreSlim _browserDownloadLock = new(1, 1);

        public string ProviderCode => "MORNINGSTAR_AU";

        public MorningstarAuProvider(
            ILogger<MorningstarAuProvider> logger,
            IHttpClientFactory httpClientFactory,
            IAIService aiService)
        {
            _logger = logger;
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
                await EnsureBrowserDownloadedAsync();

                var price = await FetchPriceAsync(apirCode);
                if (price.HasValue)
                {
                    result.Price = price.Value;
                    _logger.LogInformation("Morningstar AU: Retrieved price {Price} for APIR {Code}", price.Value, apirCode);
                    return result;
                }

                _logger.LogWarning("Morningstar AU: No price found for {Code}", apirCode);
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

            _logger.LogInformation("Morningstar AU: Processing batch of {Count} managed funds", supportedAssets.Count);

            // Use a single browser instance for the whole batch
            await EnsureBrowserDownloadedAsync();

            IBrowser? browser = null;
            try
            {
                browser = await LaunchBrowserAsync();

                foreach (var asset in supportedAssets)
                {
                    try
                    {
                        var price = await FetchPriceWithBrowserAsync(browser, asset.Symbol.ToUpper());
                        if (price.HasValue)
                        {
                            results[asset.Id] = new PriceResult { Price = price.Value };
                            _logger.LogInformation("Morningstar AU: Retrieved price {Price} for APIR {Code}", price.Value, asset.Symbol);
                        }
                        else
                        {
                            _logger.LogWarning("Morningstar AU: No price found for {Code}", asset.Symbol);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Morningstar AU: Failed to fetch price for APIR {Code}", asset.Symbol);
                    }

                    // Be polite between requests
                    if (supportedAssets.Count > 1)
                    {
                        await Task.Delay(2000);
                    }
                }
            }
            finally
            {
                if (browser != null) await browser.DisposeAsync();
            }

            return results;
        }

        public async Task<List<HistoricalPriceDto>> GetHistoricalPricesAsync(Asset asset, DateTime from, DateTime to, string? apiKey = null)
        {
            _logger.LogDebug("Morningstar AU: Historical prices not available for APIR {Code}", asset.Symbol);
            return await Task.FromResult(new List<HistoricalPriceDto>());
        }

        public bool SupportsAsset(Asset asset)
        {
            if (string.IsNullOrWhiteSpace(asset.Symbol)) return false;
            if (ApirCodePattern.IsMatch(asset.Symbol)) return true;
            if (asset.AssetType?.ToLower() == "managedfund" &&
                (asset.Market?.ToUpper() == "AU" || asset.Market?.ToUpper() == "ASX"))
            {
                return true;
            }
            return false;
        }

        private async Task EnsureBrowserDownloadedAsync()
        {
            if (_browserDownloaded) return;

            await _browserDownloadLock.WaitAsync();
            try
            {
                if (_browserDownloaded) return;
                _logger.LogInformation("Morningstar AU: Downloading browser for Puppeteer...");
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                _browserDownloaded = true;
                _logger.LogInformation("Morningstar AU: Browser downloaded successfully.");
            }
            finally
            {
                _browserDownloadLock.Release();
            }
        }

        private async Task<IBrowser> LaunchBrowserAsync()
        {
            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = false, // Non-headless avoids most anti-bot detection
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-setuid-sandbox",
                    "--disable-blink-features=AutomationControlled",
                    "--window-size=1440,900",
                    "--window-position=-2000,-2000" // Position off-screen so window isn't visible
                }
            });
        }

        /// <summary>
        /// Fetch price for a single APIR code (creates its own browser).
        /// Used by GetCurrentPriceAsync for single asset lookups.
        /// </summary>
        private async Task<decimal?> FetchPriceAsync(string apirCode)
        {
            IBrowser? browser = null;
            try
            {
                browser = await LaunchBrowserAsync();
                return await FetchPriceWithBrowserAsync(browser, apirCode);
            }
            finally
            {
                if (browser != null) await browser.DisposeAsync();
            }
        }

        /// <summary>
        /// Core scraping logic using an existing browser instance.
        /// 
        /// Strategy:
        /// 1. Go to Morningstar AU homepage
        /// 2. Click the search button to open the search widget  
        /// 3. Type the APIR code and wait for autocomplete
        /// 4. Click the fund result to navigate to fund page
        /// 5. Wait for the SPA to render price data
        /// 6. Extract Entry/Exit/Unit price from the rendered text
        /// </summary>
        private async Task<decimal?> FetchPriceWithBrowserAsync(IBrowser browser, string apirCode)
        {
            IPage? page = null;

            try
            {
                page = await browser.NewPageAsync();

                // Set viewport
                await page.SetViewportAsync(new ViewPortOptions { Width = 1440, Height = 900 });

                // Set a standard user agent
                await page.SetUserAgentAsync(
                    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36");

                // Remove webdriver flag
                await page.EvaluateExpressionOnNewDocumentAsync(
                    "Object.defineProperty(navigator, 'webdriver', { get: () => false });");

                // --- Step 1: Navigate to Morningstar AU homepage ---
                _logger.LogDebug("Morningstar AU: Navigating to homepage for APIR {Code}", apirCode);
                await page.GoToAsync("https://www.morningstar.com.au", new NavigationOptions
                {
                    WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                    Timeout = 30000
                });

                // Small delay to let any JS init complete
                await Task.Delay(2000);

                // --- Step 2: Click the search button ---
                var searchClicked = await page.EvaluateFunctionAsync<bool>(@"() => {
                    const buttons = Array.from(document.querySelectorAll('button'));
                    const searchBtn = buttons.find(b => b.textContent.trim() === 'Search');
                    if (searchBtn) {
                        searchBtn.click();
                        return true;
                    }
                    return false;
                }");

                if (!searchClicked)
                {
                    _logger.LogWarning("Morningstar AU: Could not find search button for {Code}", apirCode);
                    return null;
                }

                _logger.LogDebug("Morningstar AU: Search button clicked, waiting for input");
                await Task.Delay(2000);

                // --- Step 3: Find and focus the search input ---
                var inputReady = await page.EvaluateFunctionAsync<bool>(@"() => {
                    const selectors = [
                        'input.mds-search-field__input__mca-dfd',
                        'input[aria-label=""Search""]',
                        'input[placeholder*=""Search""]'
                    ];
                    for (const sel of selectors) {
                        const input = document.querySelector(sel);
                        if (input && input.offsetParent !== null) {
                            input.focus();
                            input.click();
                            return true;
                        }
                    }
                    return false;
                }");

                if (!inputReady)
                {
                    _logger.LogWarning("Morningstar AU: Search input not found after clicking search button for {Code}", apirCode);
                    return null;
                }

                _logger.LogDebug("Morningstar AU: Search input focused, typing APIR {Code}", apirCode);

                // --- Step 4: Type the APIR code character by character ---
                foreach (var c in apirCode)
                {
                    await page.Keyboard.TypeAsync(c.ToString());
                    await Task.Delay(80);
                }

                // Wait for autocomplete results
                _logger.LogDebug("Morningstar AU: Waiting for autocomplete results");
                await Task.Delay(3000);

                // --- Step 5: Click the autocomplete result ---
                var resultClicked = await page.EvaluateFunctionAsync<string>(@"(apir) => {
                    const links = Array.from(document.querySelectorAll('a'));
                    
                    // Find fund links in dropdown
                    const fundLink = links.find(l => 
                        l.href && 
                        l.href.includes('/investments/security/fund/') &&
                        l.offsetParent !== null
                    );

                    if (fundLink) {
                        const href = fundLink.href;
                        fundLink.click();
                        return href;
                    }
                    return null;
                }", apirCode);

                if (string.IsNullOrEmpty(resultClicked))
                {
                    _logger.LogWarning("Morningstar AU: No fund result in autocomplete for {Code}", apirCode);
                    return null;
                }

                _logger.LogDebug("Morningstar AU: Clicked fund result: {Link}", resultClicked);

                // Wait for navigation to fund page
                try
                {
                    await page.WaitForNavigationAsync(new NavigationOptions
                    {
                        WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                        Timeout = 20000
                    });
                }
                catch (TimeoutException)
                {
                    _logger.LogDebug("Morningstar AU: Navigation timeout, checking page content anyway");
                }

                // --- Step 6: Wait for SPA to render and extract price ---
                await Task.Delay(5000); // Give SPA time to fetch and render data

                var pageText = await page.EvaluateFunctionAsync<string>("() => document.body.innerText");

                if (string.IsNullOrEmpty(pageText) || pageText.Length < 100)
                {
                    _logger.LogWarning("Morningstar AU: Page text too short ({Length} chars) for {Code}",
                        pageText?.Length ?? 0, apirCode);
                    return null;
                }

                _logger.LogDebug("Morningstar AU: Page text length: {Length} for {Code}", pageText.Length, apirCode);

                var price = TryExtractPrice(pageText, EntryPricePattern) ??
                            TryExtractPrice(pageText, ExitPricePattern) ??
                            TryExtractPrice(pageText, UnitPricePattern) ??
                            TryExtractPrice(pageText, NavPricePattern);

                if (!price.HasValue)
                {
                    var snippet = pageText.Length > 500 ? pageText[..500] : pageText;
                    _logger.LogWarning("Morningstar AU: Could not extract price for {Code}. Page snippet: {Snippet}",
                        apirCode, snippet);
                }

                return price;
            }
            finally
            {
                if (page != null) await page.DisposeAsync();
            }
        }

        private decimal? TryExtractPrice(string text, Regex pattern)
        {
            var match = pattern.Match(text);
            if (match.Success && match.Groups.Count > 1)
            {
                var priceStr = match.Groups[1].Value.Replace(",", "").Replace("$", "").Trim();
                if (decimal.TryParse(priceStr, out var price) && price > 0)
                {
                    return price;
                }
            }
            return null;
        }
    }
}
