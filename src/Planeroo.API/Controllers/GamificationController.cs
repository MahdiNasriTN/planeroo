using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Gamification;
using Planeroo.Application.Interfaces;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ParentOrChild")]
[Produces("application/json")]
public class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamification;

    public GamificationController(IGamificationService gamification)
    {
        _gamification = gamification;
    }

    /// <summary>
    /// Get gamification profile for a child.
    /// </summary>
    [HttpGet("{childId:guid}")]
    [ProducesResponseType(typeof(GamificationProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(Guid childId, CancellationToken ct)
    {
        var result = await _gamification.GetProfileAsync(childId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get motivational message for a child.
    /// </summary>
    [HttpGet("{childId:guid}/motivation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMotivation(Guid childId, CancellationToken ct)
    {
        var message = await _gamification.GetMotivationalMessageAsync(childId, ct);
        var mood = await _gamification.GetMascotMoodAsync(childId, ct);

        return Ok(new { message, mascotMood = mood });
    }
}
