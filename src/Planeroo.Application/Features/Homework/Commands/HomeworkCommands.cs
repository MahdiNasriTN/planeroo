using MediatR;
using Planeroo.Application.DTOs.Homework;
using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;

namespace Planeroo.Application.Features.Homework.Commands;

public record CreateHomeworkCommand(
    Guid ChildId,
    string Title,
    string? Description,
    SubjectType Subject,
    HomeworkPriority Priority,
    DateTime DueDate,
    int? EstimatedMinutes = null
) : IRequest<Result<HomeworkDto>>;

public record UpdateHomeworkCommand(
    Guid HomeworkId,
    string? Title,
    string? Description,
    SubjectType? Subject,
    HomeworkPriority? Priority,
    DateTime? DueDate,
    int? EstimatedMinutes,
    string? Notes
) : IRequest<Result<HomeworkDto>>;

public record CompleteHomeworkCommand(
    Guid HomeworkId,
    Guid ChildId,
    int ActualMinutes
) : IRequest<Result<HomeworkDto>>;

public record DeleteHomeworkCommand(Guid HomeworkId) : IRequest<Result>;
