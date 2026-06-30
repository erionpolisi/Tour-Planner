namespace TourPlanner.BusinessLayer.Dtos.TourLogs;

/// <summary>
/// TourLog as returned by the API.
/// Duration is in minutes (frontend formats via formatDuration()).
/// </summary>
public class TourLogDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string TourName { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; }            // UTC, ISO-8601 in JSON
    public string? Comment { get; set; }
    public string Difficulty { get; set; } = string.Empty;  // "easy" | "medium" | "hard"
    public double TotalDistance { get; set; }         // km
    public int Duration { get; set; }                 // minutes
    public int Rating { get; set; }
}
