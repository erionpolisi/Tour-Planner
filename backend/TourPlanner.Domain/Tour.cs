namespace TourPlanner.Domain;

public class Tour
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public required TransportType TransportType { get; set; }

    /// <summary>Distance in meters (as returned by OpenRouteService).</summary>
    public double Distance { get; set; }

    /// <summary>Estimated travel time in seconds.</summary>
    public int Duration { get; set; }

    public TourStatus Status { get; set; }
    public string? Color { get; set; }
    public string? ImageUrl { get; set; }

    // Navigation property: a tour has many logs
    public List<TourLog> Logs { get; set; } = new();
}

public enum TransportType
{
    Unknown = 0,
    Walking = 1,
    Cycling = 2,
    Driving = 3
}

public enum TourStatus
{
    Unknown = 0,
    Planned = 1,
    Completed = 2
}
