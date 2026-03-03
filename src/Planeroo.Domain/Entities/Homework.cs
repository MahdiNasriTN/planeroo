using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// Homework task detected from scan or manually created.
/// </summary>
public class Homework : BaseEntity
{
    public Guid ChildId { get; set; }
    public Guid? ScanSessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SubjectType Subject { get; set; }
    public HomeworkStatus Status { get; set; } = HomeworkStatus.Pending;
    public HomeworkPriority Priority { get; set; } = HomeworkPriority.Medium;
    public DateTime DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public int? ActualMinutes { get; set; }
    public int XpReward { get; set; } = 10;
    public string? Notes { get; set; }
    public bool IsAutoDetected { get; set; } = false;

    // Navigation
    public Child Child { get; set; } = null!;
    public ScanSession? ScanSession { get; set; }
    public ICollection<PlanningSlot> PlanningSlots { get; set; } = new List<PlanningSlot>();
}
