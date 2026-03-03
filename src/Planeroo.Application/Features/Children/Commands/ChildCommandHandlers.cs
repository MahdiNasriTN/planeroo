using MediatR;
using Planeroo.Application.DTOs.Children;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Application.Features.Children.Commands;

public class CreateChildHandler : IRequestHandler<CreateChildCommand, Result<ChildDetailDto>>
{
    private readonly IRepository<Child> _childRepo;
    private readonly IRepository<Parent> _parentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateChildHandler(
        IRepository<Child> childRepo,
        IRepository<Parent> parentRepo,
        IUnitOfWork unitOfWork)
    {
        _childRepo = childRepo;
        _parentRepo = parentRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChildDetailDto>> Handle(CreateChildCommand cmd, CancellationToken ct)
    {
        var parent = await _parentRepo.GetByIdAsync(cmd.ParentId, ct);
        if (parent is null)
            return Result<ChildDetailDto>.Failure("Parent not found", 404);

        var child = new Child
        {
            ParentId = cmd.ParentId,
            FirstName = cmd.FirstName,
            LastName = cmd.LastName,
            DateOfBirth = DateTime.SpecifyKind(cmd.DateOfBirth, DateTimeKind.Utc),
            GradeLevel = cmd.GradeLevel,
            SchoolName = cmd.SchoolName,
            Pin = cmd.Pin,
            FavoriteColor = cmd.FavoriteColor,
            MascotName = cmd.MascotName ?? "Roo"
        };

        await _childRepo.AddAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new ChildDetailDto(
            child.Id, child.FirstName, child.LastName,
            child.DateOfBirth, child.Age, child.GradeLevel,
            child.AvatarUrl, child.SchoolName,
            child.TotalXp, child.CurrentLevel,
            child.CurrentStreak, child.LongestStreak,
            child.FavoriteColor, child.MascotName,
            child.LastActivityDate, 0, 0, new List<BadgeDto>(), child.Pin
        );

        return Result<ChildDetailDto>.Success(dto);
    }
}

public class UpdateChildHandler : IRequestHandler<UpdateChildCommand, Result<ChildDetailDto>>
{
    private readonly IRepository<Child> _childRepo;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateChildHandler(IRepository<Child> childRepo, IUnitOfWork unitOfWork)
    {
        _childRepo = childRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ChildDetailDto>> Handle(UpdateChildCommand cmd, CancellationToken ct)
    {
        var child = await _childRepo.GetByIdAsync(cmd.ChildId, ct);
        if (child is null)
            return Result<ChildDetailDto>.Failure("Child not found", 404);
        if (child.ParentId != cmd.ParentId)
            return Result<ChildDetailDto>.Failure("Access denied", 403);

        child.FirstName = cmd.FirstName;
        child.LastName = cmd.LastName;
        child.GradeLevel = cmd.GradeLevel;
        child.SchoolName = cmd.SchoolName;
        child.Pin = cmd.Pin;
        child.FavoriteColor = cmd.FavoriteColor;
        child.MascotName = cmd.MascotName;
        child.UpdatedAt = DateTime.UtcNow;

        await _childRepo.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new ChildDetailDto(
            child.Id, child.FirstName, child.LastName,
            child.DateOfBirth, child.Age, child.GradeLevel,
            child.AvatarUrl, child.SchoolName,
            child.TotalXp, child.CurrentLevel,
            child.CurrentStreak, child.LongestStreak,
            child.FavoriteColor, child.MascotName,
            child.LastActivityDate, 0, 0, null, child.Pin
        );

        return Result<ChildDetailDto>.Success(dto);
    }
}

public class DeleteChildHandler : IRequestHandler<DeleteChildCommand, Result>
{
    private readonly IRepository<Child> _childRepo;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteChildHandler(IRepository<Child> childRepo, IUnitOfWork unitOfWork)
    {
        _childRepo = childRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteChildCommand cmd, CancellationToken ct)
    {
        var child = await _childRepo.GetByIdAsync(cmd.ChildId, ct);
        if (child is null)
            return Result.Failure("Child not found", 404);
        if (child.ParentId != cmd.ParentId)
            return Result.Failure("Access denied", 403);

        await _childRepo.DeleteAsync(cmd.ChildId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
