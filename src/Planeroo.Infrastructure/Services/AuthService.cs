using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Planeroo.Application.DTOs.Auth;
using Planeroo.Application.Interfaces;
using Planeroo.Domain.Common;
using Planeroo.Domain.Entities;
using Planeroo.Domain.Interfaces;
using Planeroo.Infrastructure.Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Planeroo.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IRepository<Parent> _parentRepo;
    private readonly IRepository<Child> _childRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly PlanerooDbContext _db;

    public AuthService(
        IRepository<Parent> parentRepo,
        IRepository<Child> childRepo,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        PlanerooDbContext db)
    {
        _parentRepo = parentRepo;
        _childRepo = childRepo;
        _unitOfWork = unitOfWork;
        _config = config;
        _db = db;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterParentRequest request, CancellationToken ct = default)
    {
        // Check unique email (simplified - in production use a specific query)
        var allParents = await _parentRepo.GetAllAsync(ct);
        if (allParents.Any(p => p.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return Result<AuthResponse>.Failure("Email already registered");

        var parent = new Parent
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Timezone = request.Timezone ?? "Europe/Paris",
            Language = request.Language ?? "fr",
            HasAcceptedTerms = request.AcceptTerms,
            TermsAcceptedAt = request.AcceptTerms ? DateTime.UtcNow : null,
            DataProcessingConsent = request.AcceptTerms
        };

        await _parentRepo.AddAsync(parent, ct);

        var (accessToken, expiresAt) = GenerateJwtToken(parent.Id, parent.Email, "Parent");
        var refreshToken = GenerateRefreshToken();

        parent.RefreshToken = refreshToken;
        parent.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _unitOfWork.SaveChangesAsync(ct);

        var profile = new UserProfileDto(
            parent.Id, parent.Email, parent.FirstName, parent.LastName,
            parent.AvatarUrl, "Parent", new List<ChildSummaryDto>()
        );

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, refreshToken, expiresAt, profile));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var allParents = await _parentRepo.GetAllAsync(ct);
        var parent = allParents.FirstOrDefault(p =>
            p.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

        if (parent is null || !BCrypt.Net.BCrypt.Verify(request.Password, parent.PasswordHash))
            return Result<AuthResponse>.Failure("Invalid email or password", 401);

        var (accessToken, expiresAt) = GenerateJwtToken(parent.Id, parent.Email, "Parent");
        var refreshToken = GenerateRefreshToken();

        parent.RefreshToken = refreshToken;
        parent.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        parent.LastLoginAt = DateTime.UtcNow;

        await _parentRepo.UpdateAsync(parent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var profile = new UserProfileDto(
            parent.Id, parent.Email, parent.FirstName, parent.LastName,
            parent.AvatarUrl, "Parent", null
        );

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, refreshToken, expiresAt, profile));
    }

    public async Task<Result<AuthResponse>> ChildLoginAsync(ChildLoginRequest request, CancellationToken ct = default)
    {
        var child = await _childRepo.GetByIdAsync(request.ChildId, ct);
        if (child is null)
            return Result<AuthResponse>.Failure("Child not found", 404);

        if (child.Pin != request.Pin)
            return Result<AuthResponse>.Failure("Invalid PIN", 401);

        var (accessToken, expiresAt) = GenerateJwtToken(child.Id, child.FirstName, "Child");
        var refreshToken = GenerateRefreshToken();

        child.LastActivityDate = DateTime.UtcNow;
        await _childRepo.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var profile = new UserProfileDto(
            child.Id, "", child.FirstName, child.LastName ?? "",
            child.AvatarUrl, "Child", null
        );

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, refreshToken, expiresAt, profile));
    }

    public async Task<Result<AuthResponse>> ChildLoginByPinAsync(ChildLoginByPinRequest request, CancellationToken ct = default)
    {
        var allChildren = await _childRepo.GetAllAsync(ct);
        var child = allChildren.FirstOrDefault(c => c.Pin == request.Pin);
        if (child is null)
            return Result<AuthResponse>.Failure("PIN incorrect ou enfant introuvable", 401);

        var (accessToken, expiresAt) = GenerateJwtToken(child.Id, child.FirstName, "Child");
        var refreshToken = GenerateRefreshToken();

        child.LastActivityDate = DateTime.UtcNow;
        await _childRepo.UpdateAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var profile = new UserProfileDto(
            child.Id, "", child.FirstName, child.LastName ?? "",
            child.AvatarUrl, "Child", null
        );

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, refreshToken, expiresAt, profile));
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
            return Result<AuthResponse>.Failure("Invalid token", 401);

        var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var parent = await _parentRepo.GetByIdAsync(userId, ct);

        if (parent is null || parent.RefreshToken != request.RefreshToken ||
            parent.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result<AuthResponse>.Failure("Invalid refresh token", 401);

        var (accessToken, expiresAt) = GenerateJwtToken(parent.Id, parent.Email, "Parent");
        var newRefreshToken = GenerateRefreshToken();

        parent.RefreshToken = newRefreshToken;
        parent.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _parentRepo.UpdateAsync(parent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var profile = new UserProfileDto(
            parent.Id, parent.Email, parent.FirstName, parent.LastName,
            parent.AvatarUrl, "Parent", null
        );

        return Result<AuthResponse>.Success(new AuthResponse(accessToken, newRefreshToken, expiresAt, profile));
    }

    public async Task<Result> ChangePasswordAsync(Guid parentId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var parent = await _parentRepo.GetByIdAsync(parentId, ct);
        if (parent is null)
            return Result.Failure("Parent not found", 404);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, parent.PasswordHash))
            return Result.Failure("Current password is incorrect");

        parent.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        parent.UpdatedAt = DateTime.UtcNow;

        await _parentRepo.UpdateAsync(parent, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> LogoutAsync(Guid userId, CancellationToken ct = default)
    {
        var parent = await _parentRepo.GetByIdAsync(userId, ct);
        if (parent is not null)
        {
            parent.RefreshToken = null;
            parent.RefreshTokenExpiryTime = null;
            await _parentRepo.UpdateAsync(parent, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result<UserProfileDto>> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        // Try parent first
        var parent = await _db.Parents
            .Include(p => p.Children)
            .FirstOrDefaultAsync(p => p.Id == userId && !p.IsDeleted, ct);

        if (parent is not null)
        {
            var children = parent.Children
                .Where(c => !c.IsDeleted)
                .Select(c => new ChildSummaryDto(
                    c.Id, c.FirstName, c.AvatarUrl, c.GradeLevel, c.TotalXp, c.CurrentLevel, c.CurrentStreak
                )).ToList();

            var parentProfile = new UserProfileDto(
                parent.Id, parent.Email, parent.FirstName, parent.LastName,
                parent.AvatarUrl, "Parent", children
            );

            return Result<UserProfileDto>.Success(parentProfile);
        }

        // Fall back to child
        var child = await _childRepo.GetByIdAsync(userId, ct);
        if (child is null)
            return Result<UserProfileDto>.Failure("User not found", 404);

        var childProfile = new UserProfileDto(
            child.Id, "", child.FirstName, child.LastName ?? "",
            child.AvatarUrl, "Child", null
        );

        return Result<UserProfileDto>.Success(childProfile);
    }

    public Task<Result> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        // Implementation would verify email token
        return Task.FromResult(Result.Success());
    }

    private (string token, DateTime expiresAt) GenerateJwtToken(Guid userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Jwt:Secret"] ?? "PlanerooSuperSecretKey2024!@#$%^&*()PlanerooKey"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddHours(2);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "Planeroo",
            audience: _config["Jwt:Audience"] ?? "PlanerooApp",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["Jwt:Secret"] ?? "PlanerooSuperSecretKey2024!@#$%^&*()PlanerooKey")),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            return null;

        return principal;
    }
}
