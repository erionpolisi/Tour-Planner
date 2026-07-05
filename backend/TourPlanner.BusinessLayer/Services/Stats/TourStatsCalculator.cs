using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Stats;

/// <summary>
/// Pure functions that derive the tour-level statistics required by the
/// assignment: <em>popularity</em> (based on the number of logs) and
/// <em>child-friendliness</em> (based on log difficulty, log duration, and
/// tour distance).
///
/// Kept as static helpers because:
///   * the math is stateless,
///   * unit tests never need to mock anything, and
///   * both callers (persistence-side re-computation and read-side display)
///     use the same code path.
/// </summary>
public static class TourStatsCalculator
{
    // Thresholds for the numeric-to-bucket helpers, exposed as constants so
    // both the calculator and the tests reference the same values.
    public const int GreatChildFriendlinessThreshold = 67;
    public const int OkChildFriendlinessThreshold = 34;

    /// <summary>
    /// Popularity as-defined by the assignment: raw number of logs.
    /// Preserved as an <see cref="int"/> so PostgreSQL can index it directly.
    /// </summary>
    public static int Popularity(IReadOnlyCollection<TourLog> logs) =>
        logs?.Count ?? 0;

    /// <summary>
    /// Human-readable label for a popularity count. Kept here so the API
    /// and any future consumer agree on the buckets.
    /// </summary>
    public static string PopularityLabel(int popularity) => popularity switch
    {
        <= 0 => "not tried",
        <= 2 => "some interest",
        <= 5 => "popular",
        _    => "very popular",
    };

    /// <summary>
    /// Child-friendliness score in [0..100]. Higher = more suitable for children.
    ///
    /// Composition (weights sum to 1.0):
    ///   * 40% — difficulty component. Averages the numeric difficulty of every
    ///           log (easy=1, medium=2, hard=3), maps to 0..1 via 1 - (avg-1)/2.
    ///   * 30% — average log duration. Shorter is better. Anything ≤ 1 h scores
    ///           1.0; anything ≥ 6 h scores 0.0; linear in between.
    ///   * 30% — planned tour distance (kilometres). Shorter is better.
    ///           Anything ≤ 5 km scores 1.0; anything ≥ 50 km scores 0.0;
    ///           linear in between.
    ///
    /// When the tour has no logs the difficulty and duration components are
    /// treated as neutral (0.5) so the score is still meaningful — otherwise
    /// a brand-new tour would always look hostile to children.
    /// </summary>
    public static int ChildFriendlinessScore(
        double tourDistanceMeters,
        IReadOnlyCollection<TourLog> logs)
    {
        var difficultyComponent = AverageDifficultyComponent(logs);
        var durationComponent = AverageDurationComponent(logs);
        var distanceComponent = DistanceComponent(tourDistanceMeters);

        var raw =
            (0.40 * difficultyComponent) +
            (0.30 * durationComponent) +
            (0.30 * distanceComponent);

        return (int)Math.Round(Math.Clamp(raw, 0.0, 1.0) * 100.0);
    }

    /// <summary>Categorical label for a numeric child-friendliness score.</summary>
    public static string ChildFriendlinessLabel(int score) => score switch
    {
        >= GreatChildFriendlinessThreshold => "great for children",
        >= OkChildFriendlinessThreshold    => "ok for children",
        _                                  => "not suitable for children",
    };

    // -----------------------------------------------------------------
    // Components (all normalised to 0..1, higher = friendlier)
    // -----------------------------------------------------------------

    private static double AverageDifficultyComponent(IReadOnlyCollection<TourLog> logs)
    {
        if (logs is null || logs.Count == 0) return 0.5;
        var avg = logs.Average(l => DifficultyToNumeric(l.Difficulty));
        // Map [1..3] → [1..0]; easy=1.0, medium=0.5, hard=0.0
        return 1.0 - ((avg - 1.0) / 2.0);
    }

    private static double AverageDurationComponent(IReadOnlyCollection<TourLog> logs)
    {
        if (logs is null || logs.Count == 0) return 0.5;
        var avgMinutes = logs.Average(l => l.Duration.TotalMinutes);
        // [60 min .. 360 min] → [1.0 .. 0.0], clamped.
        return LinearScore(avgMinutes, easy: 60.0, hard: 360.0);
    }

    private static double DistanceComponent(double distanceMeters)
    {
        var km = distanceMeters / 1000.0;
        // [5 km .. 50 km] → [1.0 .. 0.0], clamped.
        return LinearScore(km, easy: 5.0, hard: 50.0);
    }

    /// <summary>
    /// Linear mapping: value ≤ <paramref name="easy"/> → 1.0,
    /// value ≥ <paramref name="hard"/> → 0.0, linear in between.
    /// </summary>
    private static double LinearScore(double value, double easy, double hard)
    {
        if (value <= easy) return 1.0;
        if (value >= hard) return 0.0;
        return 1.0 - ((value - easy) / (hard - easy));
    }

    private static int DifficultyToNumeric(Difficulty d) => d switch
    {
        Difficulty.Easy   => 1,
        Difficulty.Medium => 2,
        Difficulty.Hard   => 3,
        _                 => 2, // Unknown → treat as medium so it never crashes the calc
    };
}
