using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Auth;
using Planeroo.Application.Interfaces;
using System.Security.Claims;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new parent account.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterParentRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return CreatedAtAction(nameof(Register), result.Value);
    }

    /// <summary>
    /// Login with email and password (parent).
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Login with PIN (child).
    /// </summary>
    [HttpPost("child-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChildLogin([FromBody] ChildLoginRequest request, CancellationToken ct)
    {
        var result = await _authService.ChildLoginAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Login with PIN only (child, from login screen).
    /// </summary>
    [HttpPost("child-login-pin")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChildLoginByPin([FromBody] ChildLoginByPinRequest request, CancellationToken ct)
    {
        var result = await _authService.ChildLoginByPinAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Change password (requires authentication).
    /// </summary>
    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.ChangePasswordAsync(userId, request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Get current user profile (requires authentication).
    /// </summary>
    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.GetProfileAsync(userId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Set or clear the parent lock PIN (protects parent space from children).
    /// </summary>
    [HttpPut("parent-lock-pin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetParentLockPin([FromBody] SetParentLockPinRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var result = await _authService.SetParentLockPinAsync(userId, request.Pin, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(new { message = "Parent lock PIN updated" });
    }

    /// <summary>
    /// Logout (invalidates refresh token).
    /// </summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _authService.LogoutAsync(userId, ct);
        return Ok(new { message = "Logged out successfully" });
    }
}
