using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planeroo.Application.DTOs.AI;
using Planeroo.Application.Interfaces;

namespace Planeroo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "ParentOrChild")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly IOcrService _ocrService;

    public AIController(IAIService aiService, IOcrService ocrService)
    {
        _aiService = aiService;
        _ocrService = ocrService;
    }

    /// <summary>
    /// Chat with the AI assistant (child-safe).
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(AIChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat([FromBody] AIChatRequest request, CancellationToken ct)
    {
        var result = await _aiService.ChatAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all study sheets for a child.
    /// </summary>
    [HttpGet("study-sheets/{childId:guid}")]
    [ProducesResponseType(typeof(List<StudySheetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStudySheets(Guid childId, CancellationToken ct)
    {
        var result = await _aiService.GetStudySheetsAsync(childId, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Generate an AI study sheet.
    /// </summary>
    [HttpPost("study-sheet")]
    [ProducesResponseType(typeof(StudySheetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateStudySheet([FromBody] GenerateStudySheetRequest request, CancellationToken ct)
    {
        var result = await _aiService.GenerateStudySheetAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Scan an agenda image with OCR.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanAgenda([FromForm] Guid childId, IFormFile image, CancellationToken ct)
    {
        if (image is null || image.Length == 0)
            return BadRequest(new { message = "No image provided" });

        if (image.Length > 10 * 1024 * 1024) // 10MB
            return BadRequest(new { message = "Image too large. Maximum 10MB." });

        using var stream = image.OpenReadStream();
        var result = await _ocrService.ProcessImageAsync(childId, stream, image.FileName, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirm detected homeworks from scan and persist them as homework tasks.
    /// </summary>
    [HttpPost("scan/confirm")]
    [Authorize(Policy = "ParentOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmScan([FromBody] ConfirmScanRequest request, CancellationToken ct)
    {
        if (request.Tasks == null || request.Tasks.Count == 0)
            return BadRequest(new { message = "No tasks to confirm." });

        var result = await _ocrService.ConfirmScanAsync(request, ct);
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.Error });

        return Ok(new { message = "Tasks confirmed and added to planning", tasksCreated = result.Value });
    }
}
