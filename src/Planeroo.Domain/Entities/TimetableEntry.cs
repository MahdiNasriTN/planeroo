using Planeroo.Domain.Common;

namespace Planeroo.Domain.Entities;

/// <summary>
/// A single slot in a child's class timetable (e.g. Maths on Monday 8h–10h).
/// </summary>
public class TimetableEntry : BaseEntity
{
    public Guid TimetableId { get; set; }

    /// <summary>Day name in English: "Monday", "Tuesday", …</summary>
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>Start time string, e.g. "08:00"</summary>
    public string StartTime { get; set; } = string.Empty;

    /// <summary>End time string, e.g. "09:30"</summary>
    public string EndTime { get; set; } = string.Empty;

    /// <summary>Normalised English subject key used by the system (e.g. "Mathematics").</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Display name in the original language (e.g. "Mathématiques").</summary>
    public string SubjectDisplayName { get; set; } = string.Empty;

    /// <summary>"Morning" or "Afternoon"</summary>
    public string? Period { get; set; }

    // Navigation
    public ChildTimetable Timetable { get; set; } = null!;
}
