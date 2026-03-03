using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// AI chat interaction with content moderation.
/// </summary>
public class AIInteraction : BaseEntity
{
    public Guid ChildId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
    public string? Topic { get; set; }
    public bool WasFiltered { get; set; } = false;
    public string? FilterReason { get; set; }
    public bool ParentReviewed { get; set; } = false;
    public double? SafetyScore { get; set; }
    public int TokensUsed { get; set; } = 0;

    // Navigation
    public Child Child { get; set; } = null!;
}
