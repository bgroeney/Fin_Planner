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
        public async Task<ActionResult<RebalancingReportDto>> GetReport(Guid portfolioId, [FromQuery] decimal cashFlow = 0)
        {
            var report = await _rebalancingService.GetRebalancingReportAsync(portfolioId, cashFlow);
            return Ok(report);
        }

        [HttpPost("{portfolioId}/execute")]
        public async Task<ActionResult<List<Guid>>> Execute(Guid portfolioId, [FromBody] List<RebalancingActionDto> actions)
        {
            var (userId, userEmail) = GetUserInfo();
            if (userId == null) return Unauthorized();

            var result = await _rebalancingService.ExecuteRebalancingAsync(portfolioId, actions, userId.Value, userEmail);
            return Ok(result);
        }

        [HttpPost("{portfolioId}/execute-scheduled")]
        public async Task<ActionResult<ScheduleResultDto>> ExecuteScheduled(Guid portfolioId, [FromBody] ScheduledRebalancingRequestDto request)
        {
            var (userId, userEmail) = GetUserInfo();
            if (userId == null) return Unauthorized();

            var result = await _rebalancingService.ExecuteScheduledRebalancingAsync(portfolioId, request, userId.Value, userEmail);
            return Ok(result);
        }

        [HttpGet("{portfolioId}/schedules")]
        public async Task<ActionResult<List<RebalancingScheduleDto>>> GetSchedules(Guid portfolioId)
        {
            var schedules = await _rebalancingService.GetActiveSchedulesAsync(portfolioId);
            return Ok(schedules);
        }

        [HttpPost("schedules/{scheduleId}/execute-next")]
        public async Task<ActionResult<TrancheExecutionResultDto>> ExecuteNext(Guid scheduleId)
        {
            var (userId, userEmail) = GetUserInfo();
            if (userId == null) return Unauthorized();

            try
            {
                var result = await _rebalancingService.ExecuteNextTrancheAsync(scheduleId, userId.Value, userEmail);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("schedules/{scheduleId}/cancel")]
        public async Task<ActionResult> CancelSchedule(Guid scheduleId)
        {
            var (userId, userEmail) = GetUserInfo();
            if (userId == null) return Unauthorized();

            try
            {
                await _rebalancingService.CancelScheduleAsync(scheduleId, userId.Value, userEmail);
                return Ok(new { message = "Schedule cancelled." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private (Guid? userId, string userEmail) GetUserInfo()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "unknown@example.com";
            if (string.IsNullOrEmpty(userIdStr)) return (null, userEmail);
            return (Guid.Parse(userIdStr), userEmail);
        }
    }
}
