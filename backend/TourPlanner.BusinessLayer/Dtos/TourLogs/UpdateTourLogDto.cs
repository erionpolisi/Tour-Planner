using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos.TourLogs;

/// <summary>
/// Payload for PUT /api/logs/{id}.
/// TourId is intentionally NOT here — a log doesn't move between tours.
/// </summary>
public class UpdateTourLogDto
{
    [Required]
    public DateTime LoggedAt { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    [Required]
    public string Difficulty { get; set; } = string.Empty;

    [Range(0, 100_000)]
    public double TotalDistance { get; set; }

    [Range(0, 100_000)]
    public int Duration { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }
}
