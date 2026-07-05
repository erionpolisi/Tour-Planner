using System.ComponentModel.DataAnnotations;

namespace TourPlanner.API.Dtos.Tours;

/// <summary>
/// Payload for POST /api/tours.
/// No Id (DB generates), no Status (defaults to "planned").
/// </summary>
public class CreateTourDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, StringLength(200)]
    public string From { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string To { get; set; } = string.Empty;

    /// <summary>"walking" | "cycling" | "driving"</summary>
    [Required]
    public string TransportType { get; set; } = string.Empty;

    /// <summary>Distance in kilometers.</summary>
    [Range(0, 100_000)]
    public double Distance { get; set; }

    /// <summary>Estimated duration in minutes.</summary>
    [Range(0, 100_000)]
    public int Duration { get; set; }

    [StringLength(100)]
    public string? Color { get; set; }

    [StringLength(2000)]
    public string? ImageUrl { get; set; }
}
