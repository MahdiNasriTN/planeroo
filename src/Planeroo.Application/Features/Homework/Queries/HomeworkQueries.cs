using MediatR;
using Planeroo.Application.DTOs.Homework;
using Planeroo.Domain.Common;

namespace Planeroo.Application.Features.Homework.Queries;

public record GetHomeworksByChildQuery(Guid ChildId) : IRequest<Result<List<HomeworkDto>>>;
public record GetHomeworkByIdQuery(Guid HomeworkId) : IRequest<Result<HomeworkDto>>;
public record GetHomeworkSummaryQuery(Guid ChildId) : IRequest<Result<HomeworkSummaryDto>>;
public record GetOverdueHomeworksQuery(Guid ChildId) : IRequest<Result<List<HomeworkDto>>>;
