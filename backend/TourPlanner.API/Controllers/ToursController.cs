using Microsoft.AspNetCore.Mvc;
using TourPlanner.API.Dtos.Tours;
using TourPlanner.API.Mappers;
using TourPlanner.BusinessLayer.Services;

namespace TourPlanner.API.Controllers;

/// <summary>
/// REST endpoints for tours.
/// The controller owns the HTTP contract: DTO ↔ domain mapping happens here,
/// business errors (NotFound, Validation, Conflict) surface as exceptions
/// from the service and are translated to HTTP status codes by
/// <c>ExceptionHandlingMiddleware</c>.
/// </summary>
[ApiController]
[Route("api/tours")]
[Produces("application/json")]
public class ToursController : ControllerBase
{
    private readonly ITourService _service;
    private readonly ILogger<ToursController> _logger;

    public ToursController(ITourService service, ILogger<ToursController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>GET /api/tours — list all tours.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TourDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TourDto>>> GetAll()
    {
        var tours = await _service.GetAllAsync();
        return Ok(tours.Select(TourMapper.ToDto).ToList());
    }

    /// <summary>
    /// GET /api/tours/search?q=…&amp;limit=… — PostgreSQL full-text search
    /// across tour names, descriptions, locations, transport/status, log
    /// comments, difficulty and rating. Empty queries return an empty list
    /// with 200 OK (not 400) so clients can render "no results" naturally.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<TourSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TourSearchResultDto>>> Search(
        [FromQuery] string q,
        [FromQuery] int limit,
        CancellationToken ct)
    {
        var results = await _service.SearchAsync(q ?? string.Empty, limit, ct);
        return Ok(results.Select(TourSearchResultMapper.ToDto).ToList());
    }

    /// <summary>GET /api/tours/{id} — single tour by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourDto>> GetById(Guid id)
    {
        var tour = await _service.GetByIdAsync(id);
        return Ok(TourMapper.ToDto(tour));
    }

    /// <summary>POST /api/tours — create a new tour. Returns 201 + Location header.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TourDto>> Create([FromBody] CreateTourDto dto)
    {
        // Mapper throws ArgumentException on invalid enum strings; middleware turns that into 400.
        var entity = TourMapper.FromCreateDto(dto);
        var created = await _service.CreateAsync(entity);
        _logger.LogInformation("Tour created: {TourId} ({Name})", created.Id, created.Name);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, TourMapper.ToDto(created));
    }

    /// <summary>PUT /api/tours/{id} — full update.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TourDto>> Update(Guid id, [FromBody] UpdateTourDto dto)
    {
        var updated = await _service.UpdateAsync(id, entity => TourMapper.ApplyUpdate(entity, dto));
        return Ok(TourMapper.ToDto(updated));
    }

    /// <summary>DELETE /api/tours/{id}.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        _logger.LogInformation("Tour deleted: {TourId}", id);
        return NoContent();
    }
}
