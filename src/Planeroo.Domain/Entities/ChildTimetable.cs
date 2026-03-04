using Planeroo.Domain.Common;

namespace Planeroo.Domain.Entities;

/// <summary>
/// A child's weekly class schedule (emploi du temps), scanned from an image.
/// Each child can only have one active timetable; re-scanning replaces it.
/// </summary>
public class ChildTimetable : BaseEntity
{
    public Guid ChildId { get; set; }

    // Navigation
    public Child Child { get; set; } = null!;
    public ICollection<TimetableEntry> Entries { get; set; } = new List<TimetableEntry>();
}
