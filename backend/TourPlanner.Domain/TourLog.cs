namespace TourPlanner.Domain;

public class TourLog
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }

    /// <summary>UTC timestamp when this log was created. Convert to local time in the UI.</summary>
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    public string? Comment { get; set; }
    public Difficulty Difficulty { get; set; }

    /// <summary>Distance actually covered, in meters.</summary>
    public double TotalDistance { get; set; }

    public TimeSpan Duration { get; set; }
    public int Rating { get; set; }

    // Navigation property: a log belongs to one tour
    public Tour? Tour { get; set; }
}

public enum Difficulty
{
    Unknown = 0,
    Easy = 1,
    Medium = 2,
    Hard = 3
}
