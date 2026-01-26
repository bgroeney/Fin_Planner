using System.Text.Json;
using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services;

public interface IAuditService
{
    Task LogAsync(Guid userId, string userEmail, string action, string entityType, Guid entityId, string entityName, object? oldValues = null, object? newValues = null, string? ipAddress = null);
    Task LogCreateAsync<T>(Guid userId, string userEmail, T entity, string entityName, string? ipAddress = null) where T : class;
    Task LogUpdateAsync<T>(Guid userId, string userEmail, T oldEntity, T newEntity, string entityName, string? ipAddress = null) where T : class;
    Task LogDeleteAsync<T>(Guid userId, string userEmail, T entity, string entityName, string? ipAddress = null) where T : class;
}

public class AuditService : IAuditService
{
    private readonly FinPlannerDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(FinPlannerDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(Guid userId, string userEmail, string action, string entityType, Guid entityId, string entityName, object? oldValues = null, object? newValues = null, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Don't fail the main operation if audit logging fails
            _logger.LogError(ex, "Failed to create audit log for {Action} on {EntityType} {EntityId}", action, entityType, entityId);
        }
    }

    public async Task LogCreateAsync<T>(Guid userId, string userEmail, T entity, string entityName, string? ipAddress = null) where T : class
    {
        var entityId = GetEntityId(entity);
        await LogAsync(userId, userEmail, "Create", typeof(T).Name, entityId, entityName, null, entity, ipAddress);
    }

    public async Task LogUpdateAsync<T>(Guid userId, string userEmail, T oldEntity, T newEntity, string entityName, string? ipAddress = null) where T : class
    {
        var entityId = GetEntityId(newEntity);
        await LogAsync(userId, userEmail, "Update", typeof(T).Name, entityId, entityName, oldEntity, newEntity, ipAddress);
    }

    public async Task LogDeleteAsync<T>(Guid userId, string userEmail, T entity, string entityName, string? ipAddress = null) where T : class
    {
        var entityId = GetEntityId(entity);
        await LogAsync(userId, userEmail, "Delete", typeof(T).Name, entityId, entityName, entity, null, ipAddress);
    }

    private Guid GetEntityId<T>(T entity) where T : class
    {
        var idProperty = typeof(T).GetProperty("Id");
        if (idProperty != null && idProperty.PropertyType == typeof(Guid))
        {
            return (Guid)(idProperty.GetValue(entity) ?? Guid.Empty);
        }
        return Guid.Empty;
    }
}
