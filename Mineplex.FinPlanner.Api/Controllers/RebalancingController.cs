using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mineplex.FinPlanner.Api.Services;
using System.Security.Claims;

namespace Mineplex.FinPlanner.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RebalancingController : ControllerBase
    {
        private readonly IRebalancingService _rebalancingService;

        public RebalancingController(IRebalancingService rebalancingService)
        {
            _rebalancingService = rebalancingService;
        }

        [HttpGet("{portfolioId}")]
        public async Task<ActionResult<RebalancingReportDto>> GetReport(Guid portfolioId)
        {
            var report = await _rebalancingService.GetRebalancingReportAsync(portfolioId);
            return Ok(report);
        }

        [HttpPost("{portfolioId}/execute")]
        public async Task<ActionResult<List<Guid>>> Execute(Guid portfolioId, [FromBody] List<RebalancingActionDto> actions)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com";
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = Guid.Parse(userIdStr);
            var result = await _rebalancingService.ExecuteRebalancingAsync(portfolioId, actions, userId, userEmail);
            return Ok(result);
        }
    }
}
