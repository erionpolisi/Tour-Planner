namespace TourPlanner.Domain;

public class Tour
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the <see cref="User"/> that owns this tour. Assigned at creation time
    /// from the authenticated principal — never from the request body. Enforced
    /// non-null in the DB; every repository query filters by this column so a
    /// user can never see or modify another user's tours.
    /// </summary>
    public Guid UserId { get; set; }

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

    // ---------------------------------------------------------------
    // Computed attributes (re-derived from Logs on every log write)
    // ---------------------------------------------------------------

    /// <summary>
    /// Raw number of tour logs (0..N). Persisted so PostgreSQL full-text search
    /// can index it — kept in sync by <c>TourLogService</c> on every log CUD.
    /// </summary>
    public int Popularity { get; set; }

    /// <summary>
    /// 0..100 child-friendliness score, higher = more suitable for children.
    /// Combines average log difficulty, average log duration, and tour distance.
    /// See <c>TourStatsCalculator.ChildFriendlinessScore</c> for the formula.
    /// </summary>
    public int ChildFriendliness { get; set; }

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
