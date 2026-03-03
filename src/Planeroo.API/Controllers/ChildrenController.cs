using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Children;
using Planeroo.Application.Features.Children.Commands;
using Planeroo.Application.Features.Children.Queries;
using System.Security.Claims;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ParentOnly")]
[Produces("application/json")]
public class ChildrenController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChildrenController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid ParentId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Get all children for the authenticated parent.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChildDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildren(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetChildrenByParentQuery(ParentId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get a specific child by ID.
    /// </summary>
    [HttpGet("{childId:guid}")]
    [ProducesResponseType(typeof(ChildDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChild(Guid childId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetChildByIdQuery(childId, ParentId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new child profile.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChildDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChild([FromBody] CreateChildRequest request, CancellationToken ct)
    {
        var command = new CreateChildCommand(
            ParentId, request.FirstName, request.LastName,
            request.DateOfBirth, request.GradeLevel,
            request.SchoolName, request.Pin,
            request.FavoriteColor, request.MascotName
        );

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return CreatedAtAction(nameof(GetChild), new { childId = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update a child profile.
    /// </summary>
    [HttpPut("{childId:guid}")]
    [ProducesResponseType(typeof(ChildDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateChild(Guid childId, [FromBody] UpdateChildRequest request, CancellationToken ct)
    {
        var command = new UpdateChildCommand(
            childId, ParentId, request.FirstName, request.LastName,
            request.GradeLevel, request.SchoolName, request.Pin,
            request.FavoriteColor, request.MascotName
        );

        var result = await _mediator.Send(command, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a child profile (soft delete).
    /// </summary>
    [HttpDelete("{childId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChild(Guid childId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteChildCommand(childId, ParentId), ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return NoContent();
    }
}
