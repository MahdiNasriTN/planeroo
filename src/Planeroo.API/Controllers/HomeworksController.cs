using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Homework;
using Planeroo.Application.Features.Homework.Commands;
using Planeroo.Application.Features.Homework.Queries;
using System.Security.Claims;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ParentOrChild")]
[Produces("application/json")]
public class HomeworksController : ControllerBase
{
    private readonly IMediator _mediator;

    public HomeworksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all homeworks for a child.
    /// </summary>
    [HttpGet("child/{childId:guid}")]
    [ProducesResponseType(typeof(List<HomeworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByChild(Guid childId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHomeworksByChildQuery(childId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get homework summary for a child.
    /// </summary>
    [HttpGet("child/{childId:guid}/summary")]
    [ProducesResponseType(typeof(HomeworkSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(Guid childId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHomeworkSummaryQuery(childId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a specific homework.
    /// </summary>
    [HttpGet("{homeworkId:guid}")]
    [ProducesResponseType(typeof(HomeworkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid homeworkId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHomeworkByIdQuery(homeworkId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new homework.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(HomeworkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateHomeworkRequest request, CancellationToken ct)
    {
        var command = new CreateHomeworkCommand(
            request.ChildId, request.Title, request.Description,
            request.Subject, request.Priority, request.DueDate,
            request.EstimatedMinutes
        );

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return CreatedAtAction(nameof(GetById), new { homeworkId = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Mark homework as completed.
    /// </summary>
    [HttpPost("{homeworkId:guid}/complete")]
    [ProducesResponseType(typeof(HomeworkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(Guid homeworkId, [FromBody] CompleteHomeworkRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var command = new CompleteHomeworkCommand(homeworkId, userId, request.ActualMinutes);

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get overdue homeworks for a child.
    /// </summary>
    [HttpGet("child/{childId:guid}/overdue")]
    [ProducesResponseType(typeof(List<HomeworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdue(Guid childId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOverdueHomeworksQuery(childId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }
}
