using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Domain.Entities;

/// <summary>
/// Child profile managed by a parent.
/// </summary>
public class Child : BaseEntity
{
    public Guid ParentId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int GradeLevel { get; set; }
    public string? AvatarUrl { get; set; }
    public string? SchoolName { get; set; }
    public string? Pin { get; set; } // Simple PIN for child login

    // Gamification
    public int TotalXp { get; set; } = 0;
    public int CurrentLevel { get; set; } = 1;
    public int CurrentStreak { get; set; } = 0;
    public int LongestStreak { get; set; } = 0;
    public DateTime? LastActivityDate { get; set; }

    // Preferences
    public string? FavoriteColor { get; set; }
    public string? MascotName { get; set; } = "Roo";

    // Navigation
    public Parent Parent { get; set; } = null!;
    public ICollection<Homework> Homeworks { get; set; } = new List<Homework>();
    public ICollection<PlanningSlot> PlanningSlots { get; set; } = new List<PlanningSlot>();
    public ICollection<Badge> Badges { get; set; } = new List<Badge>();
    public ICollection<AIInteraction> AIInteractions { get; set; } = new List<AIInteraction>();
    public ICollection<ScanSession> ScanSessions { get; set; } = new List<ScanSession>();
    public ICollection<StudySheet> StudySheets { get; set; } = new List<StudySheet>();

    // Computed
    public int Age => DateTime.UtcNow.Year - DateOfBirth.Year -
        (DateTime.UtcNow.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
}
