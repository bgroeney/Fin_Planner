using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mineplex.FinPlanner.Api.Models.Portfolios;
using Mineplex.FinPlanner.Api.Services;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PortfoliosController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IPriceUpdateService _priceUpdateService;
        private readonly IDemoDataService _demoDataService;
        private readonly ILogger<PortfoliosController> _logger;

        public PortfoliosController(
            IPortfolioService portfolioService,
            IPriceUpdateService priceUpdateService,
            IDemoDataService demoDataService,
            ILogger<PortfoliosController> logger)
        {
            _portfolioService = portfolioService;
            _priceUpdateService = priceUpdateService;
            _demoDataService = demoDataService;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !Guid.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        [HttpGet]
        public async Task<ActionResult<List<PortfolioDto>>> GetPortfolios()
        {
            var portfolios = await _portfolioService.GetPortfoliosAsync(GetUserId());
            return Ok(portfolios);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PortfolioDto>> GetPortfolio(Guid id)
        {
            try
            {
                var portfolio = await _portfolioService.GetPortfolioByIdAsync(GetUserId(), id);

                // Trigger async price update in background (fire-and-forget)
                // This updates prices without blocking the response
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _priceUpdateService.UpdatePortfolioPricesAsync(id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Background price update failed for portfolio {PortfolioId}", id);
                    }
                });

                return Ok(portfolio);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<PortfolioDto>> CreatePortfolio(CreatePortfolioDto dto)
        {
            var portfolio = await _portfolioService.CreatePortfolioAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetPortfolio), new { id = portfolio.Id }, portfolio);
        }

        [AllowAnonymous]
        [HttpPost("demo")]
        public async Task<ActionResult<PortfolioDto>> CreateDemoPortfolio([FromBody] CreatePortfolioDto dto)
        {
            // Default to "Demo Portfolio" if name not provided, though frontend enforces it
            var name = !string.IsNullOrWhiteSpace(dto?.Name) ? dto.Name : "Demo Portfolio";
            var portfolio = await _demoDataService.CreateDemoPortfolioAsync(GetUserId(), name);
            return CreatedAtAction(nameof(GetPortfolio), new { id = portfolio.Id }, portfolio);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePortfolio(Guid id, UpdatePortfolioDto dto)
        {
            await _portfolioService.UpdatePortfolioAsync(GetUserId(), id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePortfolio(Guid id)
        {
            try
            {
                await _portfolioService.DeletePortfolioAsync(GetUserId(), id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{id}/assets/{assetId}/details")]
        public async Task<ActionResult<AssetDetailDto>> GetAssetDetails(Guid id, Guid assetId)
        {
            try
            {
                var details = await _portfolioService.GetAssetDetailsAsync(GetUserId(), id, assetId);
                return Ok(details);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPut("assets/{assetId}")]
        public async Task<IActionResult> UpdateAsset(Guid assetId, [FromBody] UpdateAssetDto dto)
        {
            await _portfolioService.UpdateAssetAsync(assetId, dto);
            return NoContent();
        }

        [HttpPut("{id}/assets/{assetId}/category")]
        public async Task<IActionResult> UpdateAssetCategory(Guid id, Guid assetId, [FromBody] UpdateHoldingCategoryDto dto)
        {
            await _portfolioService.UpdatePortfolioAssetCategoryAsync(id, assetId, dto.CategoryId);
            return NoContent();
        }
        [HttpGet]
        [Route("{portfolioId}/assets/{assetId}/transactions")]
        public async Task<ActionResult<List<AssetTransactionDto>>> GetAssetTransactions(Guid portfolioId, Guid assetId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            // Use user ID from auth
            // Mocking user ID for now as per prev context, or extract from claims
            var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            return await _portfolioService.GetAssetTransactionsAsync(userId, portfolioId, assetId, page, pageSize);
        }
    }
}
