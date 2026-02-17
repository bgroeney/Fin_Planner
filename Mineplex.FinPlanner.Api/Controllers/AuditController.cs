using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;

namespace Mineplex.FinPlanner.Api.Controllers;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}

public class AuditLogDetailDto : AuditLogDto
{
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly FinPlannerDbContext _context;

    public AuditController(FinPlannerDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get paginated audit logs (admin only in production, all users for now)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAuditLogs(
        [FromQuery] Guid? portfolioId = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (portfolioId.HasValue)
            query = query.Where(a => a.PortfolioId == portfolioId.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(a => a.EntityType == entityType);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(a => a.Action == action);

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserEmail = a.UserEmail,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityName = a.EntityName,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress
            })
            .ToListAsync();

        return Ok(logs);
    }

    /// <summary>
    /// Get detailed audit log entry with old/new values
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AuditLogDetailDto>> GetAuditLogDetail(Guid id)
    {
        var log = await _context.AuditLogs
            .Where(a => a.Id == id)
            .Select(a => new AuditLogDetailDto
            {
                Id = a.Id,
                UserEmail = a.UserEmail,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityName = a.EntityName,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress,
                OldValues = a.OldValues,
                NewValues = a.NewValues
            })
            .FirstOrDefaultAsync();

        if (log == null) return NotFound();

        return Ok(log);
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetEntityAuditLogs(string entityType, Guid entityId)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                UserEmail = a.UserEmail,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityName = a.EntityName,
                Timestamp = a.Timestamp,
                IpAddress = a.IpAddress
            })
            .ToListAsync();

        return Ok(logs);
    }
}
