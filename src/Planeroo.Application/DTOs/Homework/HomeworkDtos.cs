using Planeroo.Domain.Enums;

namespace Planeroo.Application.DTOs.Homework;

public record CreateHomeworkRequest(
    Guid ChildId,
    string Title,
    string? Description,
    SubjectType Subject,
    HomeworkPriority Priority,
    DateTime DueDate,
    int? EstimatedMinutes = null
);

public record UpdateHomeworkRequest(
    string? Title,
    string? Description,
    SubjectType? Subject,
    HomeworkPriority? Priority,
    DateTime? DueDate,
    int? EstimatedMinutes,
    string? Notes
);

public record HomeworkDto(
    Guid Id,
    Guid ChildId,
    string Title,
    string? Description,
    string Subject,
    string Status,
    string Priority,
    DateTime DueDate,
    DateTime? CompletedAt,
    int EstimatedMinutes,
    int? ActualMinutes,
    int XpReward,
    string? Notes,
    bool IsAutoDetected,
    bool IsOverdue,
    DateTime CreatedAt
);

public record HomeworkSummaryDto(
    int TotalHomeworks,
    int Pending,
    int InProgress,
    int Completed,
    int Overdue,
    double CompletionRate,
    int TotalXpEarned
);

public record CompleteHomeworkRequest(
    int ActualMinutes
);
