using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Planning;
using Planeroo.Application.Interfaces;
using System.Security.Claims;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ParentOrChild")]
[Produces("application/json")]
public class PlanningController : ControllerBase
{
    private readonly IPlanningEngine _planningEngine;

    public PlanningController(IPlanningEngine planningEngine)
    {
        _planningEngine = planningEngine;
    }

    /// <summary>
    /// Get the current week's planning for a child (generates on-the-fly from pending homework).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeeklyPlanningDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyPlan([FromQuery] Guid childId, CancellationToken ct)
    {
        var request = GeneratePlanningRequest.ForCurrentWeek(childId);
        var result = await _planningEngine.GenerateWeeklyPlanAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Generate a weekly study plan for a child.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(WeeklyPlanningDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GeneratePlan([FromBody] GeneratePlanningRequest request, CancellationToken ct)
    {
        var result = await _planningEngine.GenerateWeeklyPlanAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Rebalance the weekly plan after changes.
    /// </summary>
    [HttpPost("rebalance/{childId:guid}")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(WeeklyPlanningDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RebalancePlan(Guid childId, [FromQuery] int weekNumber, [FromQuery] int year, CancellationToken ct)
    {
        var result = await _planningEngine.RebalancePlanAsync(childId, weekNumber, year, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }
}
