using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos.Tours;

/// <summary>
/// Payload for PUT /api/tours/{id}.
/// Id comes from the URL, not the body.
/// Status is mutable here (e.g. user marks tour as "completed").
/// </summary>
public class UpdateTourDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required, StringLength(200)]
    public string From { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string To { get; set; } = string.Empty;

    [Required]
    public string TransportType { get; set; } = string.Empty;

    [Range(0, 100_000)]
    public double Distance { get; set; }

    [Range(0, 100_000)]
    public int Duration { get; set; }

    /// <summary>"planned" | "completed"</summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Color { get; set; }

    [StringLength(2000)]
    public string? ImageUrl { get; set; }
}
