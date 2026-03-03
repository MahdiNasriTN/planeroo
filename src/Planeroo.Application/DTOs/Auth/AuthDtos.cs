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
    Guid ChildId,
    string Pin
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
    List<ChildSummaryDto>? Children
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
