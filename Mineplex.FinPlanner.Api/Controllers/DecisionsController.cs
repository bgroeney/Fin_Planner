using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using Mineplex.FinPlanner.Api.Services;
using System.Security.Claims;
using System.Text.Json;

namespace Mineplex.FinPlanner.Api.Controllers
{
    // DTOs
    public record DecisionDto
    {
        public Guid Id { get; init; }
        public Guid PortfolioId { get; init; }
        public string PortfolioName { get; init; } = "";
        public string? Title { get; init; }
        public string Type { get; init; } = "";
        public string Status { get; init; } = "";
        public string? Rationale { get; init; }
        public string? SnapshotBefore { get; init; }
        public string? SnapshotAfter { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? ApprovedAt { get; init; }

        public DateTime? ImplementedAt { get; init; }
        public string? AllocationMethod { get; init; }
        public decimal? ProjectedTaxImpact { get; init; }
    }

    public record CreateDecisionDto
    {
        public Guid PortfolioId { get; init; }
        public string? Title { get; init; }
        public string? Rationale { get; init; }
        public string? SnapshotBefore { get; init; }
        public string? SnapshotAfter { get; init; }
        public bool SaveAsDraft { get; init; }
        public string? AllocationMethod { get; init; }
        public decimal? ProjectedTaxImpact { get; init; }
    }

    public record UpdateDecisionDto
    {
        public string? Title { get; init; }
        public string? Rationale { get; init; }
        public string? SnapshotAfter { get; init; }
    }

    public record DecisionSummaryDto
    {
        public int Total { get; init; }
        public int Pending { get; init; }
        public int Draft { get; init; }
        public int Approved { get; init; }
        public int ToBeImplemented { get; init; }
        public int Rejected { get; init; }
        public int Implemented { get; init; }
    }

    public record DecisionLogDto
    {
        public Guid Id { get; init; }
        public string Action { get; init; } = "";
        public string Details { get; init; } = "";
        public DateTime Timestamp { get; init; }
        public string UserEmail { get; init; } = "";
    }

    public record AIPortfolioRecommendationDto
    {
        public string Type { get; init; } = "Rebalancing";
        public string Title { get; init; } = "";
        public string Rationale { get; init; } = "";
        public List<RecommendedAction> Actions { get; init; } = new();
    }

    public record RecommendedAction
    {
        public string Symbol { get; init; } = "";
        public string AssetName { get; init; } = "";
        public string Action { get; init; } = ""; // Buy, Sell, Hold
        public decimal CurrentAllocation { get; init; }
        public decimal TargetAllocation { get; init; }
        public string Reasoning { get; init; } = "";
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DecisionsController : ControllerBase
    {
        private readonly FinPlannerDbContext _context;
        private readonly IAIService _aiService;

        public DecisionsController(FinPlannerDbContext context, IAIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
        }

        private void LogAction(Guid decisionId, string action, string details)
        {
            // Note: This relies on the caller calling SaveChangesAsync on the context
            _context.DecisionLogs.Add(new DecisionLog
            {
                Id = Guid.NewGuid(),
                DecisionId = decisionId,
                UserId = GetUserId(),
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get summary counts of decisions by status
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<DecisionSummaryDto>> GetSummary()
        {
            var userId = GetUserId();
            var portfolioIds = await _context.Portfolios
                .Where(p => p.OwnerId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var decisions = await _context.Decisions
                .Where(d => portfolioIds.Contains(d.PortfolioId))
                .ToListAsync();

            return new DecisionSummaryDto
            {
                Total = decisions.Count,
                Pending = decisions.Count(d => d.Status == "Pending"),
                Draft = decisions.Count(d => d.Status == "Draft"),
                Approved = decisions.Count(d => d.Status == "Approved"),
                ToBeImplemented = decisions.Count(d => d.Status == "To be implemented"),
                Rejected = decisions.Count(d => d.Status == "Rejected"),
                Implemented = decisions.Count(d => d.Status == "Implemented")
            };
        }

        /// <summary>
        /// Get all decisions for the current user across all portfolios
        /// </summary>
        [HttpGet("all")]
        public async Task<ActionResult<List<DecisionDto>>> GetAllDecisions([FromQuery] string? status = null)
        {
            var userId = GetUserId();
            var portfolioIds = await _context.Portfolios
                .Where(p => p.OwnerId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            var query = _context.Decisions
                .Include(d => d.Portfolio)
                .Where(d => portfolioIds.Contains(d.PortfolioId));

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(d => d.Status == status);
            }

            var decisions = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return decisions.Select(d => new DecisionDto
            {
                Id = d.Id,
                PortfolioId = d.PortfolioId,
                PortfolioName = d.Portfolio?.Name ?? "",
                Title = d.Title,
                Type = d.Type,
                Status = d.Status,
                Rationale = d.Rationale,
                SnapshotBefore = d.SnapshotBefore,
                SnapshotAfter = d.SnapshotAfter,
                CreatedAt = d.CreatedAt,
                ApprovedAt = d.ApprovedAt,
                ImplementedAt = d.ImplementedAt
            }).ToList();
        }

        /// <summary>
        /// Get decisions for a specific portfolio
        /// </summary>
        [HttpGet("portfolio/{portfolioId}")]
        public async Task<ActionResult<List<DecisionDto>>> GetDecisions(Guid portfolioId)
        {
            var decisions = await _context.Decisions
                .Include(d => d.Portfolio)
                .Where(d => d.PortfolioId == portfolioId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return decisions.Select(d => new DecisionDto
            {
                Id = d.Id,
                PortfolioId = d.PortfolioId,
                PortfolioName = d.Portfolio?.Name ?? "",
                Title = d.Title,
                Type = d.Type,
                Status = d.Status,
                Rationale = d.Rationale,
                SnapshotBefore = d.SnapshotBefore,
                SnapshotAfter = d.SnapshotAfter,
                CreatedAt = d.CreatedAt,
                ApprovedAt = d.ApprovedAt,
                ImplementedAt = d.ImplementedAt
            }).ToList();
        }

        /// <summary>
        /// Get history logs for a decision
        /// </summary>
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<DecisionLogDto>>> GetDecisionHistory(Guid id)
        {
            var logs = await _context.DecisionLogs
                .Where(l => l.DecisionId == id)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();

            // Fetch users for these logs
            var userIds = logs.Select(l => l.UserId).Distinct();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email);

            return logs.Select(l => new DecisionLogDto
            {
                Id = l.Id,
                Action = l.Action,
                Details = l.Details,
                Timestamp = l.Timestamp,
                UserEmail = users.TryGetValue(l.UserId, out var email) ? email : "Unknown"
            }).ToList();
        }

        /// <summary>
        /// Create a new decision (manual)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DecisionDto>> CreateDecision(CreateDecisionDto dto)
        {
            var decision = new Decision
            {
                Id = Guid.NewGuid(),
                PortfolioId = dto.PortfolioId,
                Title = dto.Title,
                Type = "Manual",
                Status = dto.SaveAsDraft ? "Draft" : "Pending",
                Rationale = dto.Rationale,
                SnapshotBefore = dto.SnapshotBefore ?? "{}",
                SnapshotAfter = dto.SnapshotAfter ?? "{}",
                AllocationMethod = dto.AllocationMethod,
                ProjectedTaxImpact = dto.ProjectedTaxImpact,
                CreatedAt = DateTime.UtcNow
            };

            _context.Decisions.Add(decision);
            LogAction(decision.Id, "Create", $"Created decision: {decision.Title}");
            await _context.SaveChangesAsync();

            var portfolio = await _context.Portfolios.FindAsync(dto.PortfolioId);

            return CreatedAtAction(nameof(GetDecisions), new { portfolioId = decision.PortfolioId }, new DecisionDto
            {
                Id = decision.Id,
                PortfolioId = decision.PortfolioId,
                PortfolioName = portfolio?.Name ?? "",
                Title = decision.Title,
                Type = decision.Type,
                Status = decision.Status,
                Rationale = decision.Rationale,
                SnapshotBefore = decision.SnapshotBefore,
                SnapshotAfter = decision.SnapshotAfter,
                CreatedAt = decision.CreatedAt
            });
        }

        /// <summary>
        /// Update a decision (only drafts can be edited)
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDecision(Guid id, UpdateDecisionDto dto)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            if (decision.Status != "Draft")
                return BadRequest("Only draft decisions can be edited");

            if (dto.Title != null) decision.Title = dto.Title;
            if (dto.Rationale != null) decision.Rationale = dto.Rationale;
            if (dto.SnapshotAfter != null) decision.SnapshotAfter = dto.SnapshotAfter;

            LogAction(decision.Id, "Update", "Updated decision properties");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Submit a draft decision for approval
        /// </summary>
        [HttpPut("{id}/submit")]
        public async Task<IActionResult> SubmitDecision(Guid id)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            if (decision.Status != "Draft")
                return BadRequest("Only draft decisions can be submitted");

            decision.Status = "Pending";
            LogAction(decision.Id, "StatusChange", "Submitted for approval (Draft -> Pending)");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Approve a pending decision (automatically moves to 'To be implemented')
        /// </summary>
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveDecision(Guid id)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            decision.Status = "To be implemented";
            decision.ApprovedAt = DateTime.UtcNow;
            LogAction(decision.Id, "StatusChange", "Decision Approved → Ready to Implement");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Move an approved decision to 'To be implemented' status
        /// </summary>
        [HttpPut("{id}/to-be-implemented")]
        public async Task<IActionResult> ToBeImplementedDecision(Guid id)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            if (decision.Status != "Approved")
                return BadRequest("Only approved decisions can move to 'To be implemented'");

            decision.Status = "To be implemented";
            LogAction(decision.Id, "StatusChange", "Moved to 'To be implemented'");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Reject a pending decision
        /// </summary>
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectDecision(Guid id)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            decision.Status = "Rejected";
            LogAction(decision.Id, "StatusChange", "Decision Rejected");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Mark an approved decision as implemented
        /// </summary>
        [HttpPut("{id}/implement")]
        public async Task<IActionResult> ImplementDecision(Guid id)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            if (decision.Status != "Approved" && decision.Status != "To be implemented")
                return BadRequest("Only approved or 'To be implemented' decisions can be implemented");

            decision.Status = "Implemented";
            decision.ImplementedAt = DateTime.UtcNow;
            LogAction(decision.Id, "StatusChange", "Decision Marked Implemented");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Delete a decision (only drafts and rejected)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDecision(Guid id)
        {
            var decision = await _context.Decisions.FindAsync(id);
            if (decision == null) return NotFound();

            // Remove restriction: Allow deleting any decision (soft delete)
            // if (decision.Status != "Draft" && decision.Status != "Rejected")
            //     return BadRequest("Only draft or rejected decisions can be deleted");

            // Soft Delete
            var oldStatus = decision.Status;
            decision.Status = "Deleted";
            LogAction(decision.Id, "Delete", $"Decision deleted (moved from {oldStatus} to Deleted)");
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Generate AI portfolio recommendations
        /// </summary>
        [HttpPost("ai-recommendations/{portfolioId}")]
        public async Task<ActionResult<AIPortfolioRecommendationDto>> GenerateAIRecommendations(Guid portfolioId)
        {
            var userId = GetUserId();

            // Verify portfolio ownership
            var portfolio = await _context.Portfolios
                .Include(p => p.BenchmarkAsset)
                .FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);

            if (portfolio == null) return NotFound("Portfolio not found");

            // Get categories with targets
            var categories = await _context.AssetCategories
                .Where(c => c.PortfolioId == portfolioId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            // Get holdings with current values
            var accountIds = await _context.Accounts
                .Where(a => a.PortfolioId == portfolioId)
                .Select(a => a.Id)
                .ToListAsync();

            var holdings = await _context.Holdings
                .Include(h => h.Asset)
                    .ThenInclude(a => a.CurrentPrice)
                .Include(h => h.Category)
                .Where(h => accountIds.Contains(h.AccountId))
                .ToListAsync();

            // Calculate total value and category allocations
            var totalValue = holdings.Sum(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0));

            var categoryAllocations = categories.Select(c =>
            {
                var categoryHoldings = holdings.Where(h => h.CategoryId == c.Id).ToList();
                var categoryValue = categoryHoldings.Sum(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0));
                var currentPercent = totalValue > 0 ? (categoryValue / totalValue) * 100 : 0;
                var variance = currentPercent - c.TargetPercentage;

                return new
                {
                    Category = c.Name,
                    Target = c.TargetPercentage,
                    Current = currentPercent,
                    Variance = variance,
                    Holdings = categoryHoldings
                };
            }).ToList();

            // Build recommendations based on variance
            var actions = new List<RecommendedAction>();

            foreach (var alloc in categoryAllocations.Where(a => Math.Abs(a.Variance) > 2)) // >2% variance
            {
                var action = alloc.Variance > 0 ? "Sell" : "Buy";
                var topHolding = alloc.Holdings
                    .OrderByDescending(h => h.Units * (h.Asset.CurrentPrice?.Price ?? 0))
                    .FirstOrDefault();

                if (topHolding != null)
                {
                    actions.Add(new RecommendedAction
                    {
                        Symbol = topHolding.Asset.Symbol,
                        AssetName = topHolding.Asset.Name,
                        Action = action,
                        CurrentAllocation = Math.Round((decimal)alloc.Current, 2),
                        TargetAllocation = alloc.Target,
                        Reasoning = $"{alloc.Category} is {Math.Abs((decimal)alloc.Variance):F1}% {(alloc.Variance > 0 ? "overweight" : "underweight")}. Consider {action.ToLower()}ing {topHolding.Asset.Symbol} to rebalance."
                    });
                }
            }

            // Create AI decision record
            if (actions.Any())
            {
                var aiDecision = new Decision
                {
                    Id = Guid.NewGuid(),
                    PortfolioId = portfolioId,
                    Title = $"AI Rebalancing Recommendation - {DateTime.UtcNow:MMM dd, yyyy}",
                    Type = "AI",
                    Status = "Pending",
                    Rationale = $"Portfolio rebalancing suggested based on allocation variance analysis. {actions.Count} action(s) recommended.",
                    SnapshotBefore = JsonSerializer.Serialize(categoryAllocations.Select(a => new { a.Category, Current = a.Current })),
                    SnapshotAfter = JsonSerializer.Serialize(categoryAllocations.Select(a => new { a.Category, Target = a.Target })),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Decisions.Add(aiDecision);
                await _context.SaveChangesAsync();
            }

            return new AIPortfolioRecommendationDto
            {
                Type = "Rebalancing",
                Title = $"Portfolio Rebalancing Analysis",
                Rationale = actions.Any()
                    ? $"Based on your target allocation, {actions.Count} adjustment(s) are recommended to optimize portfolio balance."
                    : "Your portfolio is well-balanced. No immediate actions recommended.",
                Actions = actions
            };
        }
        /// <summary>
        /// Backfill decisions for existing transactions
        /// </summary>
        [HttpPost("backfill/{portfolioId}")]
        public async Task<IActionResult> BackfillDecisions(Guid portfolioId)
        {
            var userId = GetUserId();
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.OwnerId == userId);

            if (portfolio == null) return NotFound("Portfolio not found");

            var accountIds = await _context.Accounts
                .Where(a => a.PortfolioId == portfolioId)
                .Select(a => a.Id)
                .ToListAsync();

            var transactions = await _context.Transactions
                .Where(t => accountIds.Contains(t.AccountId) && (t.Type == TransactionType.Buy || t.Type == TransactionType.Sell))
                .Include(t => t.Asset)
                .ToListAsync();

            int addedCount = 0;
            var existingDecisions = await _context.Decisions
                .Where(d => d.PortfolioId == portfolioId)
                .ToListAsync();

            foreach (var txn in transactions)
            {
                var alreadyExists = existingDecisions.Any(d =>
                    d.ImplementedAt.HasValue &&
                    d.ImplementedAt.Value.Date == txn.EffectiveDate.Date &&
                    d.Title != null && d.Title.Contains(txn.Asset.Symbol) &&
                    d.Type == "Manual"
                );

                if (!alreadyExists)
                {
                    var action = txn.Type.ToString();
                    var decision = new Decision
                    {
                        Id = Guid.NewGuid(),
                        PortfolioId = portfolioId,
                        Title = $"{action} {txn.Units:F0} {txn.Asset.Symbol}",
                        Type = "Manual",
                        Status = "Implemented",
                        Rationale = $"Retrospective decision backfilled from transaction: {action} {txn.Units:F4} units of {txn.Asset.Name} on {txn.EffectiveDate:d}.",
                        SnapshotBefore = "{}",
                        SnapshotAfter = "{}",
                        CreatedAt = txn.EffectiveDate,
                        ApprovedAt = txn.EffectiveDate,
                        ImplementedAt = txn.EffectiveDate
                    };
                    _context.Decisions.Add(decision);
                    existingDecisions.Add(decision);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = $"Backfilled {addedCount} decisions." });
        }
    }
}
