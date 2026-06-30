namespace TourPlanner.BusinessLayer.Dtos.Tours;

/// <summary>
/// Tour as returned by the API.
/// Distance is in kilometers, duration in minutes — frontend-friendly units.
/// </summary>
public class TourDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;  // "walking" | "cycling" | "driving"
    public double Distance { get; set; }                        // km
    public int Duration { get; set; }                           // minutes
    public string Status { get; set; } = string.Empty;          // "planned" | "completed"
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }
}
