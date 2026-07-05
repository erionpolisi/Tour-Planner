namespace TourPlanner.BusinessLayer.Services.Routing;

/// <summary>A single point in WGS84 coordinates.</summary>
public sealed record Coord(double Lat, double Lng);

/// <summary>A geocoding hit — a resolved place with its coordinates.</summary>
public sealed record GeocodeHit(string DisplayName, double Lat, double Lng);

/// <summary>Result of a routing calculation between two points.</summary>
public sealed record RouteInfo(
    double DistanceKm,
    int DurationMinutes,
    string DurationLabel,
    IReadOnlyList<Coord> Path);

/// <summary>
/// Facade over the external map / routing providers.
/// Geocoding uses Nominatim (free, no key), routing uses OpenRouteService (needs a key).
/// The controller layer is the only thing that should call this.
/// </summary>
public interface IRoutingService
{
    Task<IReadOnlyList<GeocodeHit>> SearchAsync(
        string query, int limit, CancellationToken ct = default);

    Task<GeocodeHit?> GeocodeOneAsync(
        string query, CancellationToken ct = default);

    Task<string> ReverseGeocodeAsync(
        double lat, double lng, CancellationToken ct = default);

    Task<RouteInfo?> RouteAsync(
        Coord from, Coord to, string transportType, CancellationToken ct = default);
}
