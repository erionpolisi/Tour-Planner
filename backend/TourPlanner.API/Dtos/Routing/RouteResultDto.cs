namespace TourPlanner.API.Dtos.Routing;

/// <summary>Result of a routing calculation between two points.</summary>
public sealed record RouteResultDto(
    double DistanceKm,
    int DurationMinutes,
    string DurationLabel,
    IReadOnlyList<CoordinateDto> Path);
