namespace TourPlanner.API.Dtos.Tours;

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

    // ---------------------------------------------------------------
    // Computed attributes — set by the server, ignored on write.
    // ---------------------------------------------------------------

    /// <summary>Raw log count (0..N).</summary>
    public int Popularity { get; set; }

    /// <summary>"not tried" | "some interest" | "popular" | "very popular".</summary>
    public string PopularityLabel { get; set; } = string.Empty;

    /// <summary>0..100, higher = friendlier for children.</summary>
    public int ChildFriendliness { get; set; }

    /// <summary>"not suitable for children" | "ok for children" | "great for children".</summary>
    public string ChildFriendlinessLabel { get; set; } = string.Empty;
}
