using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.Timetable;
using Planeroo.Application.Interfaces;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ParentOrChild")]
[Produces("application/json")]
public class TimetableController : ControllerBase
{
    private readonly ITimetableService _timetableService;

    public TimetableController(ITimetableService timetableService)
    {
        _timetableService = timetableService;
    }

    /// <summary>
    /// Scan a weekly timetable image and extract subjects/slots.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(ScanTimetableResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanTimetable([FromForm] Guid childId, IFormFile image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new { message = "No image provided." });

        await using var stream = image.OpenReadStream();
        var result = await _timetableService.ScanTimetableAsync(childId, stream, image.FileName, ct);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirm and save the scanned timetable for a child (replaces existing).
    /// </summary>
    [HttpPost("confirm")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(TimetableDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmTimetable([FromBody] ConfirmTimetableRequest request, CancellationToken ct)
    {
        var result = await _timetableService.ConfirmTimetableAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get the saved timetable for a child.
    /// </summary>
    [HttpGet("{childId:guid}")]
    [ProducesResponseType(typeof(TimetableDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimetable(Guid childId, CancellationToken ct)
    {
        var result = await _timetableService.GetTimetableAsync(childId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        if (result.Value is null)
            return NotFound(new { message = "No timetable found for this child." });

        return Ok(result.Value);
    }

    /// <summary>
    /// Update a specific timetable entry.
    /// </summary>
    [HttpPut("entries/{entryId:guid}")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(TimetableEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntry(Guid entryId, [FromBody] ScannedTimetableEntryDto dto, CancellationToken ct)
    {
        var result = await _timetableService.UpdateEntryAsync(entryId, dto, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a specific timetable entry.
    /// </summary>
    [HttpDelete("entries/{entryId:guid}")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid entryId, CancellationToken ct)
    {
        var result = await _timetableService.DeleteEntryAsync(entryId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return NoContent();
    }
}
