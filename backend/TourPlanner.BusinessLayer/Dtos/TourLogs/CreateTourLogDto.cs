using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos.TourLogs;

public class CreateTourLogDto
{
    [Required]
    public Guid TourId { get; set; }

    /// <summary>UTC timestamp of when the tour was performed.</summary>
    [Required]
    public DateTime LoggedAt { get; set; }

    [StringLength(2000)]
    public string? Comment { get; set; }

    /// <summary>"easy" | "medium" | "hard"</summary>
    [Required]
    public string Difficulty { get; set; } = string.Empty;

    [Range(0, 100_000)]
    public double TotalDistance { get; set; }

    /// <summary>Duration in minutes.</summary>
    [Range(0, 100_000)]
    public int Duration { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }
}
