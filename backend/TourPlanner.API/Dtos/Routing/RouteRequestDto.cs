using System.ComponentModel.DataAnnotations;

namespace TourPlanner.API.Dtos.Routing;

/// <summary>Body of POST /api/routing/route.</summary>
public sealed class RouteRequestDto
{
    [Required] public CoordinateDto From { get; set; } = null!;
    [Required] public CoordinateDto To { get; set; } = null!;

    /// <summary>One of: driving, cycling, walking.</summary>
    [Required] public string TransportType { get; set; } = "driving";
}

public sealed class CoordinateDto
{
    [Range(-90, 90)] public double Lat { get; set; }
    [Range(-180, 180)] public double Lng { get; set; }
}
