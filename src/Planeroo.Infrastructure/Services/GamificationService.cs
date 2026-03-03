using Planeroo.Application.DTOs.Gamification;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Enums;
using Planeroo.Domain.Interfaces;

namespace Planeroo.Infrastructure.Services;

public class GamificationService : IGamificationService
{
    private readonly IRepository<Child> _childRepo;
    private readonly IRepository<Badge> _badgeRepo;
    private readonly IRepository<Homework> _homeworkRepo;
    private readonly IUnitOfWork _unitOfWork;

    // XP thresholds per level
    private static readonly int[] LevelThresholds = {
        0, 100, 250, 500, 800, 1200, 1700, 2300, 3000, 3800,
        4700, 5700, 6800, 8000, 9300, 10700, 12200, 13800, 15500, 17300
    };

    public GamificationService(
        IRepository<Child> childRepo,
        IRepository<Badge> badgeRepo,
        IRepository<Homework> homeworkRepo,
        IUnitOfWork unitOfWork)
    {
        _childRepo = childRepo;
        _badgeRepo = badgeRepo;
        _homeworkRepo = homeworkRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GamificationProfileDto>> GetProfileAsync(Guid childId, CancellationToken ct = default)
    {
        var child = await _childRepo.GetByIdAsync(childId, ct);
        if (child is null)
            return Result<GamificationProfileDto>.Failure("Child not found", 404);

        var badges = (await _badgeRepo.GetAllAsync(ct))
            .Where(b => b.ChildId == childId)
            .Select(b => new BadgeSummaryDto(b.Id, b.Name, b.Description, b.IconName,
                b.Category.ToString(), b.EarnedAt, b.IsNew))
            .ToList();

        var xpToNext = GetXpForLevel(child.CurrentLevel + 1) - child.TotalXp;
        var levelProgress = child.CurrentLevel < LevelThresholds.Length - 1
            ? (double)(child.TotalXp - GetXpForLevel(child.CurrentLevel)) /
              (GetXpForLevel(child.CurrentLevel + 1) - GetXpForLevel(child.CurrentLevel))
            : 1.0;

        var mood = await GetMascotMoodAsync(childId, ct);
        var message = await GetMotivationalMessageAsync(childId, ct);

        var profile = new GamificationProfileDto(
            childId, child.FirstName, child.TotalXp, child.CurrentLevel,
            Math.Max(0, xpToNext), Math.Clamp(levelProgress, 0, 1),
            child.CurrentStreak, child.LongestStreak,
            badges, GenerateWeeklyActivity(child), mood, message
        );

        return Result<GamificationProfileDto>.Success(profile);
    }

    public async Task<Result<LevelUpDto?>> AddXpAsync(Guid childId, int amount, string reason, CancellationToken ct = default)
    {
        var child = await _childRepo.GetByIdAsync(childId, ct);
        if (child is null)
            return Result<LevelUpDto?>.Failure("Child not found", 404);

        child.TotalXp += amount;
        var oldLevel = child.CurrentLevel;
        child.CurrentLevel = CalculateLevel(child.TotalXp);
        child.UpdatedAt = DateTime.UtcNow;

        await _childRepo.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (child.CurrentLevel > oldLevel)
        {
            var levelUp = new LevelUpDto(
                child.CurrentLevel,
                GetXpForLevel(child.CurrentLevel + 1),
                null,
                GetLevelUpMessage(child.CurrentLevel)
            );
            return Result<LevelUpDto?>.Success(levelUp);
        }

        return Result<LevelUpDto?>.Success(null);
    }

    public async Task CheckAndAwardBadgesAsync(Guid childId, CancellationToken ct = default)
    {
        var child = await _childRepo.GetByIdAsync(childId, ct);
        if (child is null) return;

        var existingBadges = (await _badgeRepo.GetAllAsync(ct))
            .Where(b => b.ChildId == childId)
            .Select(b => b.Name)
            .ToHashSet();

        var homeworks = (await _homeworkRepo.GetAllAsync(ct))
            .Where(h => h.ChildId == childId)
            .ToList();

        var newBadges = new List<Badge>();

        // Streak badges
        if (child.CurrentStreak >= 3 && !existingBadges.Contains("3-Day Streak"))
            newBadges.Add(CreateBadge(childId, "3-Day Streak", "Complete tasks 3 days in a row!", "streak_3", BadgeCategory.Streak, 25));
        if (child.CurrentStreak >= 7 && !existingBadges.Contains("Weekly Warrior"))
            newBadges.Add(CreateBadge(childId, "Weekly Warrior", "7-day streak champion!", "streak_7", BadgeCategory.Streak, 50));
        if (child.CurrentStreak >= 30 && !existingBadges.Contains("Monthly Master"))
            newBadges.Add(CreateBadge(childId, "Monthly Master", "30-day streak legend!", "streak_30", BadgeCategory.Streak, 200));

        // Completion badges
        var completedCount = homeworks.Count(h => h.Status == HomeworkStatus.Completed);
        if (completedCount >= 1 && !existingBadges.Contains("First Step"))
            newBadges.Add(CreateBadge(childId, "First Step", "Completed your first homework!", "first_step", BadgeCategory.Completion, 15));
        if (completedCount >= 10 && !existingBadges.Contains("Getting Started"))
            newBadges.Add(CreateBadge(childId, "Getting Started", "10 homeworks completed!", "completion_10", BadgeCategory.Completion, 50));
        if (completedCount >= 50 && !existingBadges.Contains("Homework Hero"))
            newBadges.Add(CreateBadge(childId, "Homework Hero", "50 homeworks completed!", "completion_50", BadgeCategory.Completion, 150));
        if (completedCount >= 100 && !existingBadges.Contains("Champion Learner"))
            newBadges.Add(CreateBadge(childId, "Champion Learner", "100 homeworks completed!", "completion_100", BadgeCategory.Champion, 300));

        // Speed badges
        var earlyCompletions = homeworks.Count(h =>
            h.Status == HomeworkStatus.Completed &&
            h.CompletedAt.HasValue &&
            h.CompletedAt.Value < h.DueDate.AddDays(-1));
        if (earlyCompletions >= 5 && !existingBadges.Contains("Early Bird"))
            newBadges.Add(CreateBadge(childId, "Early Bird", "Completed 5 tasks early!", "early_bird", BadgeCategory.Speed, 75));

        foreach (var badge in newBadges)
        {
            await _badgeRepo.AddAsync(badge, ct);
            child.TotalXp += badge.XpReward;
        }

        if (newBadges.Any())
        {
            await _childRepo.UpdateAsync(child, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateStreakAsync(Guid childId, CancellationToken ct = default)
    {
        var child = await _childRepo.GetByIdAsync(childId, ct);
        if (child is null) return;

        var today = DateTime.UtcNow.Date;
        if (child.LastActivityDate?.Date == today)
            return; // Already recorded today

        if (child.LastActivityDate?.Date == today.AddDays(-1))
        {
            child.CurrentStreak++;
        }
        else if (child.LastActivityDate?.Date < today.AddDays(-1))
        {
            child.CurrentStreak = 1;
        }
        else
        {
            child.CurrentStreak = 1;
        }

        if (child.CurrentStreak > child.LongestStreak)
            child.LongestStreak = child.CurrentStreak;

        child.LastActivityDate = today;
        await _childRepo.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public Task<string> GetMotivationalMessageAsync(Guid childId, CancellationToken ct = default)
    {
        var messages = new[]
        {
            "You're doing amazing! Keep it up! 🌟",
            "Every step forward counts! 🚀",
            "You're becoming a superstar learner! ⭐",
            "Roo is so proud of you! 🦘",
            "Great focus today! You're unstoppable! 💪",
            "Learning is your superpower! 🦸",
            "You're on fire! Keep going! 🔥",
            "One task at a time, you've got this! 🎯",
            "Your brain is growing stronger every day! 🧠",
            "Champions never give up! You're a champion! 🏆"
        };

        var index = Math.Abs(childId.GetHashCode()) % messages.Length;
        return Task.FromResult(messages[index]);
    }

    public Task<string> GetMascotMoodAsync(Guid childId, CancellationToken ct = default)
    {
        // In production, this would analyze recent activity
        return Task.FromResult("happy");
    }

    private static Badge CreateBadge(Guid childId, string name, string desc, string icon, BadgeCategory category, int xp)
        => new()
        {
            ChildId = childId,
            Name = name,
            Description = desc,
            IconName = icon,
            Category = category,
            XpReward = xp,
            EarnedAt = DateTime.UtcNow,
            IsNew = true
        };

    private static int CalculateLevel(int totalXp)
    {
        for (int i = LevelThresholds.Length - 1; i >= 0; i--)
        {
            if (totalXp >= LevelThresholds[i])
                return i + 1;
        }
        return 1;
    }

    private static int GetXpForLevel(int level)
    {
        if (level <= 0) return 0;
        if (level > LevelThresholds.Length) return LevelThresholds[^1] + (level - LevelThresholds.Length) * 2000;
        return LevelThresholds[level - 1];
    }

    private static string GetLevelUpMessage(int level) => level switch
    {
        2 => "Level 2! You're getting started! 🌱",
        3 => "Level 3! Keep growing! 🌿",
        5 => "Level 5! Half way to double digits! ⭐",
        10 => "Level 10! You're a learning machine! 🤖",
        15 => "Level 15! Almost a master! 🏅",
        20 => "Level 20! LEGENDARY! 👑",
        _ => $"Level {level}! Amazing progress! 🎉"
    };

    private static List<WeeklyStreakDto> GenerateWeeklyActivity(Child child)
    {
        var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var today = (int)DateTime.UtcNow.DayOfWeek;
        return days.Select((day, i) => new WeeklyStreakDto(
            day, i < child.CurrentStreak % 7, i < child.CurrentStreak % 7 ? 10 : 0
        )).ToList();
    }
}
