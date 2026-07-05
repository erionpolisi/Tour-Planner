using Microsoft.AspNetCore.Mvc;
using TourPlanner.API.Dtos.Routing;
using TourPlanner.BusinessLayer.Services.Routing;

namespace TourPlanner.API.Controllers;

/// <summary>
/// Backend proxy for the external map services. The Angular frontend must NOT
/// call Nominatim / OpenRouteService directly (assignment requirement).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class RoutingController : ControllerBase
{
    private readonly IRoutingService _routing;
    private readonly ILogger<RoutingController> _logger;

    public RoutingController(IRoutingService routing, ILogger<RoutingController> logger)
    {
        _routing = routing;
        _logger = logger;
    }

    /// <summary>Autocomplete: multiple hits for a partial address.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<GeocodeResultDto>>> Search(
        [FromQuery] string q,
        [FromQuery] int limit = 6,
        CancellationToken ct = default)
    {
        var hits = await _routing.SearchAsync(q ?? string.Empty, limit, ct);
        return Ok(hits.Select(h => new GeocodeResultDto(h.DisplayName, h.Lat, h.Lng)).ToList());
    }

    /// <summary>Single best geocode hit for an address.</summary>
    [HttpGet("geocode")]
    public async Task<ActionResult<GeocodeResultDto?>> Geocode(
        [FromQuery] string q,
        CancellationToken ct = default)
    {
        var hit = await _routing.GeocodeOneAsync(q ?? string.Empty, ct);
        if (hit is null) return NotFound();
        return new GeocodeResultDto(hit.DisplayName, hit.Lat, hit.Lng);
    }

    /// <summary>Reverse-geocode a point to a human-readable address.</summary>
    [HttpGet("reverse")]
    public async Task<ActionResult<object>> Reverse(
        [FromQuery] double lat,
        [FromQuery] double lng,
        CancellationToken ct = default)
    {
        var label = await _routing.ReverseGeocodeAsync(lat, lng, ct);
        return new { displayName = label };
    }

    /// <summary>Compute distance + duration + geometry between two points.</summary>
    [HttpPost("route")]
    public async Task<ActionResult<RouteResultDto>> Route(
        [FromBody] RouteRequestDto req,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var route = await _routing.RouteAsync(
            new Coord(req.From.Lat, req.From.Lng),
            new Coord(req.To.Lat, req.To.Lng),
            req.TransportType,
            ct);

        if (route is null)
        {
            _logger.LogWarning(
                "No route found (transport={Transport})", req.TransportType);
            return NotFound(new { error = "No route found for the given points." });
        }

        var dto = new RouteResultDto(
            route.DistanceKm,
            route.DurationMinutes,
            route.DurationLabel,
            route.Path.Select(p => new CoordinateDto { Lat = p.Lat, Lng = p.Lng }).ToList());
        return Ok(dto);
    }
}
