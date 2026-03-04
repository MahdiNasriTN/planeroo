using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Planning;
using Planeroo.Application.Interfaces;
using System.Globalization;

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
    /// Get the current (or specified) week's planning for a child.
    /// Defaults to smart mode so all pending tasks are distributed across the week
    /// regardless of their due date, matching what the AI generate sheet produces.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeeklyPlanningDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeeklyPlan(
        [FromQuery] Guid childId,
        [FromQuery] string mode = "smart",
        [FromQuery] int weekNumber = 0,
        [FromQuery] int year = 0,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var resolvedWeek = weekNumber > 0 ? weekNumber : ISOWeek.GetWeekOfYear(now);
        var resolvedYear = year       > 0 ? year       : ISOWeek.GetYear(now);

        var request = new GeneratePlanningRequest(
            ChildId:         childId,
            WeekNumber:      resolvedWeek,
            Year:            resolvedYear,
            AvailableSlots:  null,
            AutoBalance:     true,
            Mode:            mode
        );

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
