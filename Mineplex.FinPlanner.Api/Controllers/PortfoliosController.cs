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
        private readonly IPortfolioSharingService _sharingService;
        private readonly ILogger<PortfoliosController> _logger;

        public PortfoliosController(
            IPortfolioService portfolioService,
            IPriceUpdateService priceUpdateService,
            IDemoDataService demoDataService,
            IPortfolioSharingService sharingService,
            ILogger<PortfoliosController> logger)
        {
            _portfolioService = portfolioService;
            _priceUpdateService = priceUpdateService;
            _demoDataService = demoDataService;
            _sharingService = sharingService;
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

        // ... (Skipping CreatePortfolio) ...

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

        [HttpPost("{id}/categories")]
        public async Task<ActionResult<CategoryTargetDto>> AddCategory(Guid id, [FromBody] CategoryTargetDto dto)
        {
            try
            {
                var result = await _portfolioService.AddCategoryAsync(id, dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/categories/{categoryId}")]
        public async Task<IActionResult> UpdateCategory(Guid id, Guid categoryId, [FromBody] CategoryTargetDto dto)
        {
            try
            {
                await _portfolioService.UpdateCategoryAsync(id, categoryId, dto);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/categories/{categoryId}")]
        public async Task<IActionResult> DeleteCategory(Guid id, Guid categoryId)
        {
            try
            {
                await _portfolioService.DeleteCategoryAsync(id, categoryId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }
        [HttpGet]
        [Route("{portfolioId}/assets/{assetId}/transactions")]
        public async Task<ActionResult<List<AssetTransactionDto>>> GetAssetTransactions(Guid portfolioId, Guid assetId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return await _portfolioService.GetAssetTransactionsAsync(GetUserId(), portfolioId, assetId, page, pageSize);
        }

        // ===== Sharing Endpoints =====

        [HttpGet("{id}/shares")]
        public async Task<ActionResult<List<PortfolioShareDto>>> GetShares(Guid id)
        {
            try
            {
                var shares = await _sharingService.GetSharesForPortfolioAsync(GetUserId(), id);
                return Ok(shares);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/shares")]
        public async Task<ActionResult<PortfolioShareDto>> SharePortfolio(Guid id, [FromBody] SharePortfolioRequest request)
        {
            try
            {
                var share = await _sharingService.SharePortfolioAsync(GetUserId(), id, request);
                return CreatedAtAction(nameof(GetShares), new { id }, share);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/shares/{shareId}")]
        public async Task<IActionResult> RevokeShare(Guid id, Guid shareId)
        {
            try
            {
                await _sharingService.RevokeShareAsync(GetUserId(), shareId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
