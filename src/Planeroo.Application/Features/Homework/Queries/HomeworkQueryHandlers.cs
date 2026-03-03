using MediatR;
using Planeroo.Application.DTOs.Homework;
using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Application.Features.Homework.Queries;

public class GetHomeworksByChildHandler : IRequestHandler<GetHomeworksByChildQuery, Result<List<HomeworkDto>>>
{
    private readonly IRepository<Domain.Entities.Homework> _repo;

    public GetHomeworksByChildHandler(IRepository<Domain.Entities.Homework> repo) => _repo = repo;

    public async Task<Result<List<HomeworkDto>>> Handle(GetHomeworksByChildQuery req, CancellationToken ct)
    {
        var all = await _repo.GetAllAsync(ct);
        var dtos = all
            .Where(h => h.ChildId == req.ChildId)
            .OrderByDescending(h => h.DueDate)
            .Select(HomeworkMapper.MapToDto)
            .ToList();

        return Result<List<HomeworkDto>>.Success(dtos);
    }
}

public class GetHomeworkByIdHandler : IRequestHandler<GetHomeworkByIdQuery, Result<HomeworkDto>>
{
    private readonly IRepository<Domain.Entities.Homework> _repo;

    public GetHomeworkByIdHandler(IRepository<Domain.Entities.Homework> repo) => _repo = repo;

    public async Task<Result<HomeworkDto>> Handle(GetHomeworkByIdQuery req, CancellationToken ct)
    {
        var hw = await _repo.GetByIdAsync(req.HomeworkId, ct);
        if (hw is null)
            return Result<HomeworkDto>.Failure("Homework not found", 404);

        return Result<HomeworkDto>.Success(HomeworkMapper.MapToDto(hw));
    }
}

public class GetHomeworkSummaryHandler : IRequestHandler<GetHomeworkSummaryQuery, Result<HomeworkSummaryDto>>
{
    private readonly IRepository<Domain.Entities.Homework> _repo;

    public GetHomeworkSummaryHandler(IRepository<Domain.Entities.Homework> repo) => _repo = repo;

    public async Task<Result<HomeworkSummaryDto>> Handle(GetHomeworkSummaryQuery req, CancellationToken ct)
    {
        var all = await _repo.GetAllAsync(ct);
        var list = all.Where(h => h.ChildId == req.ChildId).ToList();

        var total = list.Count;
        var pending = list.Count(h => h.Status == HomeworkStatus.Pending);
        var inProgress = list.Count(h => h.Status == HomeworkStatus.InProgress);
        var completed = list.Count(h => h.Status == HomeworkStatus.Completed);
        var overdue = list.Count(h => h.DueDate < DateTime.UtcNow && h.Status != HomeworkStatus.Completed);
        var totalXp = list.Where(h => h.Status == HomeworkStatus.Completed).Sum(h => h.XpReward);
        var rate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0;

        return Result<HomeworkSummaryDto>.Success(
            new HomeworkSummaryDto(total, pending, inProgress, completed, overdue, rate, totalXp));
    }
}

public class GetOverdueHomeworksHandler : IRequestHandler<GetOverdueHomeworksQuery, Result<List<HomeworkDto>>>
{
    private readonly IRepository<Domain.Entities.Homework> _repo;

    public GetOverdueHomeworksHandler(IRepository<Domain.Entities.Homework> repo) => _repo = repo;

    public async Task<Result<List<HomeworkDto>>> Handle(GetOverdueHomeworksQuery req, CancellationToken ct)
    {
        var all = await _repo.GetAllAsync(ct);
        var dtos = all
            .Where(h => h.ChildId == req.ChildId && h.DueDate < DateTime.UtcNow && h.Status != HomeworkStatus.Completed)
            .OrderBy(h => h.DueDate)
            .Select(HomeworkMapper.MapToDto)
            .ToList();

        return Result<List<HomeworkDto>>.Success(dtos);
    }
}

internal static class HomeworkMapper
{
    internal static HomeworkDto MapToDto(Domain.Entities.Homework hw) => new(
        hw.Id, hw.ChildId, hw.Title, hw.Description,
        hw.Subject.ToString(), hw.Status.ToString(), hw.Priority.ToString(),
        hw.DueDate, hw.CompletedAt, hw.EstimatedMinutes, hw.ActualMinutes,
        hw.XpReward, hw.Notes, hw.IsAutoDetected,
        hw.DueDate < DateTime.UtcNow && hw.Status != HomeworkStatus.Completed,
        hw.CreatedAt
    );
}
