using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// AI-generated study sheet for a child.
/// </summary>
public class StudySheet : BaseEntity
{
    public Guid ChildId { get; set; }
    public Guid? HomeworkId { get; set; }
    public string Title { get; set; } = string.Empty;
    public SubjectType Subject { get; set; }
    public string Content { get; set; } = string.Empty; // Markdown content
    public string? Summary { get; set; }
    public int TargetAge { get; set; }
    public int GradeLevel { get; set; }
    public bool IsFavorite { get; set; } = false;
    public int ViewCount { get; set; } = 0;

    // Navigation
    public Child Child { get; set; } = null!;
}
