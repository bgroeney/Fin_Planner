using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using System.Security.Claims;

namespace Mineplex.FinPlanner.Api.Controllers
{
    public class CreateGoalDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime TargetDate { get; set; }
    }

    public class UpdateGoalDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime TargetDate { get; set; }
        public bool IsCompleted { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GoalsController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;
        private readonly ILogger<GoalsController> _logger;

        public GoalsController(FinPlannerDbContext context, ILogger<GoalsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("User ID claim not found or invalid. Claims: {Claims}",
                    string.Join(", ", User.Claims.Select(c => $"{c.Type}:{c.Value}")));
                return Guid.Empty;
            }
            return userId;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Goal>>> GetGoals()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            return await _context.Goals
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.TargetDate)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Goal>> GetGoal(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null) return NotFound();

            return goal;
        }

        [HttpPost]
        public async Task<ActionResult<Goal>> CreateGoal(CreateGoalDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            _logger.LogInformation("Creating goal {GoalName} for user {UserId}", dto.Name, userId);

            // PostgreSQL with Npgsql requires UTC for timestamptz
            var targetDate = DateTime.SpecifyKind(dto.TargetDate, DateTimeKind.Utc);

            var goal = new Goal
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                TargetAmount = dto.TargetAmount,
                CurrentAmount = dto.CurrentAmount,
                TargetDate = targetDate,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Goals.Add(goal);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetGoal), new { id = goal.Id }, goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save goal to database");
                return StatusCode(500, "Internal server error while saving goal");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGoal(Guid id, UpdateGoalDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null) return NotFound();

            goal.Name = dto.Name;
            goal.TargetAmount = dto.TargetAmount;
            goal.CurrentAmount = dto.CurrentAmount;
            goal.TargetDate = DateTime.SpecifyKind(dto.TargetDate, DateTimeKind.Utc);
            goal.IsCompleted = dto.IsCompleted;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var goal = await _context.Goals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);

            if (goal == null) return NotFound();

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
