using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TourPlanner.API.Dtos.TourLogs;
using TourPlanner.API.Mappers;
using TourPlanner.BusinessLayer.Services;

namespace TourPlanner.API.Controllers;

/// <summary>
/// REST endpoints for tour logs.
/// The controller owns the HTTP contract: DTO ↔ domain mapping happens here,
/// business errors surface as exceptions from the service and are translated
/// to HTTP status codes by <c>ExceptionHandlingMiddleware</c>.
///
/// Every action scopes to the caller's user id (JWT <c>sub</c> claim) so a
/// user only ever sees the logs of tours they own.
/// </summary>
[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public class TourLogsController : ControllerBase
{
    private readonly ITourLogService _service;
    private readonly ILogger<TourLogsController> _logger;

    public TourLogsController(ITourLogService service, ILogger<TourLogsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    private Guid CurrentUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (Guid.TryParse(sub, out var userId)) return userId;
        throw new UnauthorizedAccessException("Missing or malformed 'sub' claim.");
    }

    /// <summary>
    /// GET /api/logs — list all logs for the current user (optionally filtered by tour).
    /// Example: <c>GET /api/logs?tourId=&lt;guid&gt;</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TourLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TourLogDto>>> GetAll([FromQuery] Guid? tourId)
    {
        var userId = CurrentUserId();
        var logs = tourId.HasValue
            ? await _service.GetByTourIdAsync(userId, tourId.Value)
            : await _service.GetAllAsync(userId);
        return Ok(logs.Select(TourLogMapper.ToDto).ToList());
    }

    /// <summary>GET /api/logs/{id} — single log by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TourLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourLogDto>> GetById(Guid id)
    {
        var log = await _service.GetByIdAsync(CurrentUserId(), id);
        return Ok(TourLogMapper.ToDto(log));
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(List<TourLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TourLogDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        var logs = await _service.SearchAsync(
            CurrentUserId(),
            q ?? string.Empty,
            limit,
            ct);

        return Ok(logs.Select(TourLogMapper.ToDto).ToList());
    }

    /// <summary>POST /api/logs — create a new log. Returns 201 + Location header.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TourLogDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourLogDto>> Create([FromBody] CreateTourLogDto dto)
    {
        var entity = TourLogMapper.FromCreateDto(dto);
        var created = await _service.CreateAsync(CurrentUserId(), entity);
        _logger.LogInformation("TourLog created: {LogId} for tour {TourId}", created.Id, created.TourId);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, TourLogMapper.ToDto(created));
    }

    /// <summary>PUT /api/logs/{id} — full update.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TourLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourLogDto>> Update(Guid id, [FromBody] UpdateTourLogDto dto)
    {
        var updated = await _service.UpdateAsync(CurrentUserId(), id, entity => TourLogMapper.ApplyUpdate(entity, dto));
        return Ok(TourLogMapper.ToDto(updated));
    }

    /// <summary>DELETE /api/logs/{id}.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(CurrentUserId(), id);
        _logger.LogInformation("TourLog deleted: {LogId}", id);
        return NoContent();
    }
}
