namespace Mineplex.FinPlanner.Api.Models;

/// <summary>
/// Tracks changes to financial data for compliance and audit purposes.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>
    /// Action performed: Create, Update, Delete
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity affected: Portfolio, Holding, Transaction, Goal, etc.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    /// <summary>
    /// Entity name or description for display
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of values before change (null for Create)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON representation of values after change (null for Delete)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the user making the change
    /// </summary>
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
