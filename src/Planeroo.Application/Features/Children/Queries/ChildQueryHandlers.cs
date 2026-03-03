using MediatR;
using Planeroo.Application.DTOs.Children;
using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;
using HomeworkEntity = Planeroo.Domain.Entities.Homework;
using ChildEntity = Planeroo.Domain.Entities.Child;

namespace Planeroo.Application.Features.Children.Queries;

public class GetChildrenByParentHandler : IRequestHandler<GetChildrenByParentQuery, Result<List<ChildDetailDto>>>
{
    private readonly IRepository<ChildEntity> _childRepo;
    private readonly IRepository<HomeworkEntity> _homeworkRepo;

    public GetChildrenByParentHandler(IRepository<ChildEntity> childRepo, IRepository<HomeworkEntity> homeworkRepo)
    {
        _childRepo = childRepo;
        _homeworkRepo = homeworkRepo;
    }

    public async Task<Result<List<ChildDetailDto>>> Handle(GetChildrenByParentQuery req, CancellationToken ct)
    {
        var allChildren = await _childRepo.GetAllAsync(ct);
        var children = allChildren.Where(c => c.ParentId == req.ParentId).ToList();

        var allHomeworks = await _homeworkRepo.GetAllAsync(ct);

        var dtos = children.Select(c =>
        {
            var childHw = allHomeworks.Where(h => h.ChildId == c.Id).ToList();
            var pending = childHw.Count(h => h.Status == HomeworkStatus.Pending || h.Status == HomeworkStatus.InProgress);
            var completed = childHw.Count(h => h.Status == HomeworkStatus.Completed);

            return new ChildDetailDto(
                c.Id, c.FirstName, c.LastName,
                c.DateOfBirth, c.Age, c.GradeLevel,
                c.AvatarUrl, c.SchoolName,
                c.TotalXp, c.CurrentLevel,
                c.CurrentStreak, c.LongestStreak,
                c.FavoriteColor, c.MascotName,
                c.LastActivityDate, pending, completed,
                null, c.Pin
            );
        }).ToList();

        return Result<List<ChildDetailDto>>.Success(dtos);
    }
}

public class GetChildByIdHandler : IRequestHandler<GetChildByIdQuery, Result<ChildDetailDto>>
{
    private readonly IRepository<ChildEntity> _childRepo;
    private readonly IRepository<HomeworkEntity> _homeworkRepo;

    public GetChildByIdHandler(IRepository<ChildEntity> childRepo, IRepository<HomeworkEntity> homeworkRepo)
    {
        _childRepo = childRepo;
        _homeworkRepo = homeworkRepo;
    }

    public async Task<Result<ChildDetailDto>> Handle(GetChildByIdQuery req, CancellationToken ct)
    {
        var child = await _childRepo.GetByIdAsync(req.ChildId, ct);
        if (child is null)
            return Result<ChildDetailDto>.Failure("Child not found", 404);
        if (child.ParentId != req.ParentId)
            return Result<ChildDetailDto>.Failure("Access denied", 403);

        var allHomeworks = await _homeworkRepo.GetAllAsync(ct);
        var childHw = allHomeworks.Where(h => h.ChildId == child.Id).ToList();
        var pending = childHw.Count(h => h.Status == HomeworkStatus.Pending || h.Status == HomeworkStatus.InProgress);
        var completed = childHw.Count(h => h.Status == HomeworkStatus.Completed);

        var dto = new ChildDetailDto(
            child.Id, child.FirstName, child.LastName,
            child.DateOfBirth, child.Age, child.GradeLevel,
            child.AvatarUrl, child.SchoolName,
            child.TotalXp, child.CurrentLevel,
            child.CurrentStreak, child.LongestStreak,
            child.FavoriteColor, child.MascotName,
            child.LastActivityDate, pending, completed,
            null, child.Pin
        );

        return Result<ChildDetailDto>.Success(dto);
    }
}
