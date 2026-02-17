using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly FinPlannerDbContext _db;
        private readonly IPriceUpdateService _priceUpdateService;
        private readonly PriceSourceManager _priceManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            FinPlannerDbContext db,
            IPriceUpdateService priceUpdateService,
            PriceSourceManager priceManager,
            ILogger<AdminController> logger)
        {
            _db = db;
            _priceUpdateService = priceUpdateService;
            _priceManager = priceManager;
            _logger = logger;
        }

        // GET: api/admin/price-settings
        [HttpGet("price-settings")]
        public async Task<ActionResult<PriceSettingsDto>> GetPriceSettings()
        {
            var intervalSetting = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "PriceUpdateIntervalMinutes");

            var lastUpdate = await _db.CurrentPrices
                .OrderByDescending(cp => cp.LastUpdated)
                .Select(cp => cp.LastUpdated)
                .FirstOrDefaultAsync();

            return Ok(new PriceSettingsDto
            {
                UpdateIntervalMinutes = int.TryParse(intervalSetting?.Value, out var interval) ? interval : 15,
                LastUpdateTime = lastUpdate
            });
        }

        // PUT: api/admin/price-settings/interval
        [HttpPut("price-settings/interval")]
        public async Task<IActionResult> UpdatePriceInterval([FromBody] UpdateIntervalRequest request)
        {
            if (request.IntervalMinutes < 1 || request.IntervalMinutes > 1440)
            {
                return BadRequest("Interval must be between 1 and 1440 minutes");
            }

            var setting = await _db.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "PriceUpdateIntervalMinutes");

            if (setting == null)
            {
                setting = new SystemSetting
                {
                    Key = "PriceUpdateIntervalMinutes",
                    Value = request.IntervalMinutes.ToString(),
                    Description = "How often to update market prices (in minutes)",
                    LastModified = DateTime.UtcNow
                };
                _db.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = request.IntervalMinutes.ToString();
                setting.LastModified = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Price update interval changed to {Minutes} minutes", request.IntervalMinutes);

            return Ok();
        }

        // POST: api/admin/price-refresh
        [HttpPost("price-refresh")]
        public async Task<ActionResult<PriceUpdateResult>> TriggerPriceRefresh()
        {
            _logger.LogInformation("Manual price refresh triggered");
            var result = await _priceUpdateService.UpdateAllPricesAsync();
            return Ok(result);
        }

        // GET: api/admin/price-sources
        [HttpGet("price-sources")]
        public async Task<ActionResult<List<PriceSourceDto>>> GetPriceSources()
        {
            var sources = await _db.PriceSources
                .OrderBy(s => s.Priority)
                .ToListAsync();

            return Ok(sources.Select(s => new PriceSourceDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Priority = s.Priority,
                IsEnabled = s.IsEnabled,
                HasApiKey = !string.IsNullOrEmpty(s.ApiKey),
                RateLimitPerMinute = s.RateLimitPerMinute
            }));
        }

        // PUT: api/admin/price-sources/priority
        [HttpPut("price-sources/priority")]
        public async Task<IActionResult> UpdateSourcePriority([FromBody] List<SourcePriorityUpdate> updates)
        {
            foreach (var update in updates)
            {
                var source = await _db.PriceSources.FindAsync(update.SourceId);
                if (source != null)
                {
                    source.Priority = update.NewPriority;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Price source priorities updated");

            return Ok();
        }

        // PUT: api/admin/price-sources/{id}/toggle
        [HttpPut("price-sources/{id}/toggle")]
        public async Task<IActionResult> TogglePriceSource(Guid id)
        {
            var source = await _db.PriceSources.FindAsync(id);
            if (source == null)
            {
                return NotFound();
            }

            source.IsEnabled = !source.IsEnabled;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Price source {Source} {Status}",
                source.Name, source.IsEnabled ? "enabled" : "disabled");

            return Ok();
        }

        // PUT: api/admin/price-sources/{id}/apikey
        [HttpPut("price-sources/{id}/apikey")]
        public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] UpdateApiKeyRequest request)
        {
            var source = await _db.PriceSources.FindAsync(id);
            if (source == null)
            {
                return NotFound();
            }

            source.ApiKey = string.IsNullOrWhiteSpace(request.ApiKey) ? null : request.ApiKey;
            await _db.SaveChangesAsync();

            _logger.LogInformation("API key updated for source {Source}", source.Name);

            return Ok();
        }

        // PUT: api/admin/price-sources/{id}/config
        [HttpPut("price-sources/{id}/config")]
        public async Task<IActionResult> UpdateSourceConfig(Guid id, [FromBody] UpdateSourceConfigRequest request)
        {
            var source = await _db.PriceSources.FindAsync(id);
            if (source == null)
            {
                return NotFound();
            }

            source.ConfigurationJson = request.ConfigurationJson;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Configuration updated for source {Source}", source.Name);

            return Ok();
        }
        // GET: api/admin/assets/price-sources
        [HttpGet("assets/price-sources")]
        public async Task<ActionResult<List<AssetPriceSourceDto>>> GetAssetPriceSources()
        {
            var assets = await _db.Assets
                .Include(a => a.CurrentPrice)
                .ToListAsync();

            var overrides = await _db.AssetPriceSourceOverrides
                .Include(o => o.PriceSource)
                .Where(o => o.IsPreferred)
                .ToListAsync();

            var result = assets.Select(asset =>
            {
                var currentPrice = asset.CurrentPrice;
                var assetOverride = overrides.FirstOrDefault(o => o.AssetId == asset.Id);

                return new AssetPriceSourceDto
                {
                    AssetId = asset.Id,
                    Symbol = asset.Symbol,
                    Name = asset.Name,
                    CurrentSourceUsed = currentPrice?.SourceUsed,
                    LastUpdated = currentPrice?.LastUpdated,
                    CurrentPrice = currentPrice?.Price,
                    OverrideSourceId = assetOverride?.PriceSourceId,
                    CustomSymbol = assetOverride?.CustomSymbol,
                    SuggestedSymbol = assetOverride?.SuggestedSymbol
                };
            }).ToList();

            return Ok(result);
        }

        // PUT: api/admin/assets/{id}/price-source
        [HttpPut("assets/{id}/price-source")]
        public async Task<IActionResult> SetAssetPriceSource(Guid id, [FromBody] SetAssetSourceRequest request)
        {
            var asset = await _db.Assets.FindAsync(id);
            if (asset == null)
            {
                return NotFound("Asset not found");
            }

            // Remove existing preferred override
            var existing = await _db.AssetPriceSourceOverrides
                .Where(o => o.AssetId == id && o.IsPreferred)
                .ToListAsync();
            _db.AssetPriceSourceOverrides.RemoveRange(existing);

            // Add new override if either SourceId or CustomSymbol is provided
            if (request.SourceId.HasValue || !string.IsNullOrWhiteSpace(request.CustomSymbol))
            {
                _db.AssetPriceSourceOverrides.Add(new AssetPriceSourceOverride
                {
                    Id = Guid.NewGuid(),
                    AssetId = id,
                    PriceSourceId = request.SourceId,
                    IsPreferred = true,
                    CustomSymbol = request.CustomSymbol,
                    CreatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Updated price source override for asset {AssetId}. Source: {SourceId}, Symbol: {Symbol}",
                    id, request.SourceId, request.CustomSymbol);
            }
            else
            {
                _logger.LogInformation("Cleared price source override for asset {AssetId}", id);
            }

            await _db.SaveChangesAsync();
            await UpdateAssetPriceInternalAsync(asset);
            return Ok();
        }

        // POST: api/admin/assets/{id}/sync-price
        [HttpPost("assets/{id}/sync-price")]
        public async Task<ActionResult<AssetPriceSourceDto>> SyncAssetPrice(Guid id)
        {
            var asset = await _db.Assets.Include(a => a.CurrentPrice).FirstOrDefaultAsync(a => a.Id == id);
            if (asset == null) return NotFound("Asset not found");

            var priceInfo = await UpdateAssetPriceInternalAsync(asset);
            var assetOverride = await _db.AssetPriceSourceOverrides
                .Include(o => o.PriceSource)
                .FirstOrDefaultAsync(o => o.AssetId == id && o.IsPreferred);

            return Ok(new AssetPriceSourceDto
            {
                AssetId = asset.Id,
                Symbol = asset.Symbol,
                Name = asset.Name,
                CurrentSourceUsed = priceInfo?.SourceUsed,
                LastUpdated = DateTime.UtcNow,
                CurrentPrice = priceInfo?.Price,
                OverrideSourceId = assetOverride?.PriceSourceId,
                CustomSymbol = assetOverride?.CustomSymbol,
                SuggestedSymbol = assetOverride?.SuggestedSymbol
            });
        }

        private async Task<PriceInfo?> UpdateAssetPriceInternalAsync(Asset asset)
        {
            try
            {
                var priceInfo = await _priceManager.GetPriceWithFallbackAsync(asset);
                if (priceInfo != null)
                {
                    var currentPrice = await _db.CurrentPrices.FirstOrDefaultAsync(cp => cp.AssetId == asset.Id);
                    if (currentPrice == null)
                    {
                        currentPrice = new CurrentPrice
                        {
                            Id = Guid.NewGuid(),
                            AssetId = asset.Id,
                            SourceUsed = priceInfo.SourceUsed,
                            Price = priceInfo.Price,
                            LastUpdated = DateTime.UtcNow
                        };
                        _db.CurrentPrices.Add(currentPrice);
                    }
                    else
                    {
                        currentPrice.Price = priceInfo.Price;
                        currentPrice.SourceUsed = priceInfo.SourceUsed;
                        currentPrice.LastUpdated = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Price sync success for {Symbol} (Source: {Source})", asset.Symbol, priceInfo.SourceUsed);
                    return priceInfo;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Price sync failed for {Symbol}", asset.Symbol);
            }
            return null;
        }
    }

    // DTOs
    public class PriceSettingsDto
    {
        public int UpdateIntervalMinutes { get; set; }
        public DateTime? LastUpdateTime { get; set; }
    }

    public class UpdateIntervalRequest
    {
        public int IntervalMinutes { get; set; }
    }

    public class PriceSourceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsEnabled { get; set; }
        public bool HasApiKey { get; set; }
        public int RateLimitPerMinute { get; set; }
        public string? ConfigurationJson { get; set; }
    }

    public class SourcePriorityUpdate
    {
        public Guid SourceId { get; set; }
        public int NewPriority { get; set; }
    }

    public class UpdateApiKeyRequest
    {
        public string? ApiKey { get; set; }
    }

    public class UpdateSourceConfigRequest
    {
        public string? ConfigurationJson { get; set; }
    }

    public class AssetPriceSourceDto
    {
        public Guid AssetId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? CurrentSourceUsed { get; set; }
        public DateTime? LastUpdated { get; set; }
        public decimal? CurrentPrice { get; set; }
        public Guid? OverrideSourceId { get; set; }
        public string? CustomSymbol { get; set; } // Custom pricing symbol override
        public string? SuggestedSymbol { get; set; } // Symbol found by AI
    }

    public class SetAssetSourceRequest
    {
        public Guid? SourceId { get; set; } // null to reset to default
        public string? CustomSymbol { get; set; } // Custom pricing symbol
    }
}
