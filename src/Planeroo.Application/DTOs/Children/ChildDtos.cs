using Planeroo.Domain.Enums;

namespace Planeroo.Application.DTOs.Children;

public record CreateChildRequest(
    string FirstName,
    string? LastName,
    DateTime DateOfBirth,
    int GradeLevel,
    string? SchoolName,
    string? Pin,
    string? FavoriteColor,
    string? MascotName
);

public record UpdateChildRequest(
    string FirstName,
    string? LastName,
    int GradeLevel,
    string? SchoolName,
    string? Pin,
    string? FavoriteColor,
    string? MascotName
);

public record ChildDetailDto(
    Guid Id,
    string FirstName,
    string? LastName,
    DateTime DateOfBirth,
    int Age,
    int GradeLevel,
    string? AvatarUrl,
    string? SchoolName,
    int TotalXp,
    int CurrentLevel,
    int CurrentStreak,
    int LongestStreak,
    string? FavoriteColor,
    string? MascotName,
    DateTime? LastActivityDate,
    int PendingHomeworks,
    int CompletedHomeworks,
    List<BadgeDto>? RecentBadges,
    string? Pin
);

public record BadgeDto(
    Guid Id,
    string Name,
    string Description,
    string IconName,
    string Category,
    int XpReward,
    DateTime EarnedAt,
    bool IsNew
);
