using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// Achievement badge earned by a child.
/// </summary>
public class Badge : BaseEntity
{
    public Guid ChildId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public BadgeCategory Category { get; set; }
    public int XpReward { get; set; } = 50;
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    public bool IsNew { get; set; } = true;

    // Navigation
    public Child Child { get; set; } = null!;
}
