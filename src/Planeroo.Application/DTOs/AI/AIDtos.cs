namespace Planeroo.Application.DTOs.AI;

public record AIChatRequest(
    Guid ChildId,
    string Message,
    string? Topic
);

public record AIChatResponse(
    string Message,
    bool WasFiltered,
    string? Topic,
    string MascotReaction
);

public record GenerateStudySheetRequest(
    Guid ChildId,
    Guid? HomeworkId,
    string Subject,
    string Topic,
    int? TargetAge
);

public record StudySheetDto(
    Guid Id,
    Guid ChildId,
    string Title,
    string Subject,
    string Content,
    string? Summary,
    int TargetAge,
    int GradeLevel,
    bool IsFavorite,
    int ViewCount,
    DateTime CreatedAt
);

public record ScanResultDto(
    Guid ScanSessionId,
    string Status,
    string? RawText,
    double? ConfidenceScore,
    List<DetectedHomeworkDto> DetectedTasks,
    DateTime ProcessedAt
);

public record DetectedHomeworkDto(
    string Title,
    string? Description,
    string Subject,
    DateTime? DueDate,
    int? EstimatedMinutes,
    double Confidence
);

public record ConfirmScanRequest(
    Guid ScanSessionId,
    List<ConfirmedTask> Tasks
);

public record ConfirmedTask(
    string Title,
    string? Description,
    string Subject,
    DateTime DueDate,
    int EstimatedMinutes
);
