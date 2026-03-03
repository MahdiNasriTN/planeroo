using MediatR;
using Planeroo.Application.DTOs.Homework;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Application.Features.Homework.Commands;

public class CreateHomeworkHandler : IRequestHandler<CreateHomeworkCommand, Result<HomeworkDto>>
{
    private readonly IRepository<Domain.Entities.Homework> _homeworkRepo;
    private readonly IRepository<Domain.Entities.Child> _childRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHomeworkHandler(
        IRepository<Domain.Entities.Homework> homeworkRepo,
        IRepository<Domain.Entities.Child> childRepo,
        IUnitOfWork unitOfWork)
    {
        _homeworkRepo = homeworkRepo;
        _childRepo = childRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<HomeworkDto>> Handle(CreateHomeworkCommand cmd, CancellationToken ct)
    {
        var child = await _childRepo.GetByIdAsync(cmd.ChildId, ct);
        if (child is null)
            return Result<HomeworkDto>.Failure("Child not found", 404);

        // Normalize DueDate to noon UTC so midnight-local (e.g. +01:00) dates
        // never slip to the previous UTC day during comparisons.
        var dueNoon = DateTime.SpecifyKind(
            cmd.DueDate.ToUniversalTime().AddHours(6).Date.AddHours(12),
            DateTimeKind.Utc);

        var homework = new Domain.Entities.Homework
        {
            ChildId = cmd.ChildId,
            Title = cmd.Title,
            Description = cmd.Description,
            Subject = cmd.Subject,
            Priority = cmd.Priority,
            DueDate = dueNoon,
            EstimatedMinutes = cmd.EstimatedMinutes ?? 0,
            XpReward = CalculateXpReward(cmd.Priority, cmd.EstimatedMinutes ?? 0)
        };

        await _homeworkRepo.AddAsync(homework, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<HomeworkDto>.Success(MapToDto(homework));
    }

    private static int CalculateXpReward(HomeworkPriority priority, int minutes)
    {
        var baseXp = priority switch
        {
            HomeworkPriority.Low => 5,
            HomeworkPriority.Medium => 10,
            HomeworkPriority.High => 20,
            HomeworkPriority.Urgent => 30,
            _ => 10
        };

        return baseXp + (minutes / 15) * 5;
    }

    private static HomeworkDto MapToDto(Domain.Entities.Homework hw) => new(
        hw.Id, hw.ChildId, hw.Title, hw.Description,
        hw.Subject.ToString(), hw.Status.ToString(), hw.Priority.ToString(),
        hw.DueDate, hw.CompletedAt, hw.EstimatedMinutes, hw.ActualMinutes,
        hw.XpReward, hw.Notes, hw.IsAutoDetected,
        hw.DueDate < DateTime.UtcNow && hw.Status != HomeworkStatus.Completed,
        hw.CreatedAt
    );
}

public class CompleteHomeworkHandler : IRequestHandler<CompleteHomeworkCommand, Result<HomeworkDto>>
{
    private readonly IRepository<Domain.Entities.Homework> _homeworkRepo;
    private readonly IGamificationService _gamification;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteHomeworkHandler(
        IRepository<Domain.Entities.Homework> homeworkRepo,
        IGamificationService gamification,
        IUnitOfWork unitOfWork)
    {
        _homeworkRepo = homeworkRepo;
        _gamification = gamification;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<HomeworkDto>> Handle(CompleteHomeworkCommand cmd, CancellationToken ct)
    {
        var homework = await _homeworkRepo.GetByIdAsync(cmd.HomeworkId, ct);
        if (homework is null)
            return Result<HomeworkDto>.Failure("Homework not found", 404);

        homework.Status = HomeworkStatus.Completed;
        homework.CompletedAt = DateTime.UtcNow;
        homework.ActualMinutes = cmd.ActualMinutes;
        homework.UpdatedAt = DateTime.UtcNow;

        await _homeworkRepo.UpdateAsync(homework, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Award XP and check badges
        await _gamification.AddXpAsync(cmd.ChildId, homework.XpReward, $"Completed: {homework.Title}", ct);
        await _gamification.UpdateStreakAsync(cmd.ChildId, ct);
        await _gamification.CheckAndAwardBadgesAsync(cmd.ChildId, ct);

        return Result<HomeworkDto>.Success(new HomeworkDto(
            homework.Id, homework.ChildId, homework.Title, homework.Description,
            homework.Subject.ToString(), homework.Status.ToString(), homework.Priority.ToString(),
            homework.DueDate, homework.CompletedAt, homework.EstimatedMinutes, homework.ActualMinutes,
            homework.XpReward, homework.Notes, homework.IsAutoDetected, false, homework.CreatedAt
        ));
    }
}
