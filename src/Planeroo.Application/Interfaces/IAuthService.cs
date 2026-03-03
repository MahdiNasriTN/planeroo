using Planeroo.Application.DTOs.Auth;
using Planeroo.Domain.Common;

namespace Planeroo.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterParentRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> ChildLoginAsync(ChildLoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> ChildLoginByPinAsync(ChildLoginByPinRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default);
    Task<Result> ChangePasswordAsync(Guid parentId, ChangePasswordRequest request, CancellationToken ct = default);
    Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default);
    Task<Result> VerifyEmailAsync(string token, CancellationToken ct = default);
}
