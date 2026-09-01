using AssesmentAGIT.Domain.DTOs;
using AssesmentAGIT.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace AssesmentAGIT.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlanningController : ControllerBase
{
    private readonly IPlanningService _planningService;

    public PlanningController(IPlanningService planningService)
    {
        _planningService = planningService;
    }

    /// <summary>
    /// Create and process a new planning. Idempotent on RequestCode.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlanningResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlanning([FromBody] CreatePlanningRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestCode))
            return BadRequest(new { error = "RequestCode is required." });

        if (request.Slots == null || request.Slots.Count == 0)
            return BadRequest(new { error = "At least one slot is required." });

        try
        {
            var result = await _planningService.CreatePlanningAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get paginated history of all planning submissions, ordered newest-first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlanningListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _planningService.GetPlanningHistoryAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get full detail of a single planning by its RequestCode.
    /// </summary>
    [HttpGet("{requestCode}")]
    [ProducesResponseType(typeof(PlanningResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(string requestCode)
    {
        var result = await _planningService.GetPlanningByRequestCodeAsync(requestCode);

        if (result == null)
            return NotFound(new { error = $"No planning found with RequestCode '{requestCode}'." });

        return Ok(result);
    }
}
