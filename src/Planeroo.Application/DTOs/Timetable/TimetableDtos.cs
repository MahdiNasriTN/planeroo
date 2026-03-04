namespace Planeroo.Application.DTOs.Timetable;

// ── Scan result (before confirmation) ──────────────────────────────────────

public record ScannedTimetableEntryDto(
    string DayOfWeek,
    string StartTime,
    string EndTime,
    string Subject,
    string SubjectDisplayName,
    string? Period,
    double Confidence
);

public record ScanTimetableResultDto(
    string RawText,
    List<ScannedTimetableEntryDto> DetectedEntries,
    double ConfidenceScore,
    DateTime ProcessedAt
);

// ── Confirm request ──────────────────────────────────────────────────────────

public record ConfirmTimetableRequest(
    Guid ChildId,
    List<ScannedTimetableEntryDto> Entries
);

// ── Stored timetable ─────────────────────────────────────────────────────────

public record TimetableEntryDto(
    Guid Id,
    string DayOfWeek,
    string StartTime,
    string EndTime,
    string Subject,
    string SubjectDisplayName,
    string? Period
);

public record TimetableDto(
    Guid Id,
    Guid ChildId,
    DateTime CreatedAt,
    List<TimetableEntryDto> Entries
);
