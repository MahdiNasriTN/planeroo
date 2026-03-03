namespace Planeroo.Application.DTOs.Gamification;

public record GamificationProfileDto(
    Guid ChildId,
    string ChildName,
    int TotalXp,
    int CurrentLevel,
    int XpToNextLevel,
    double LevelProgress,
    int CurrentStreak,
    int LongestStreak,
    List<BadgeSummaryDto> Badges,
    List<WeeklyStreakDto> WeeklyActivity,
    string MascotMood,
    string MotivationalMessage
);

public record BadgeSummaryDto(
    Guid Id,
    string Name,
    string Description,
    string IconName,
    string Category,
    DateTime EarnedAt,
    bool IsNew
);

public record WeeklyStreakDto(
    string DayOfWeek,
    bool IsActive,
    int XpEarned
);

public record LeaderboardEntryDto(
    int Rank,
    Guid ChildId,
    string ChildName,
    string? AvatarUrl,
    int TotalXp,
    int CurrentLevel,
    int CurrentStreak
);

public record XpTransactionDto(
    int Amount,
    string Reason,
    DateTime EarnedAt
);

public record LevelUpDto(
    int NewLevel,
    int XpRequired,
    string? UnlockedBadgeName,
    string CelebrationMessage
);
