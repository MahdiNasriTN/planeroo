using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// Notification sent to a parent.
/// </summary>
public class Notification : BaseEntity
{
    public Guid ParentId { get; set; }
    public Guid? ChildId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }

    // Navigation
    public Parent Parent { get; set; } = null!;
}
