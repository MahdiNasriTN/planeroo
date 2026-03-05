namespace Planeroo.Application.DTOs.Auth;

public record RegisterParentRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Timezone,
    string Language,
    bool AcceptTerms
);

public record LoginRequest(
    string Email,
    string Password
);

public record ChildLoginRequest(
    Guid ChildId
);

public record ChildLoginByPinRequest(
    string Pin
);

public record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserProfileDto Profile
);

public record UserProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string Role,
    List<ChildSummaryDto>? Children,
    string? ParentLockPin = null
);

public record SetParentLockPinRequest(
    string? Pin
);

public record ChildSummaryDto(
    Guid Id,
    string FirstName,
    string? AvatarUrl,
    int GradeLevel,
    int TotalXp,
    int CurrentLevel,
    int CurrentStreak
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);
