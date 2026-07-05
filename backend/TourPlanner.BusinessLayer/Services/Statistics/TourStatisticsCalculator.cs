using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Statistics;

/// <summary>
/// Pure-function calculator that reduces the current user's tours (with their
/// logs eager-loaded) into a single <see cref="TourStatistics"/> record.
///
/// Kept intentionally small — the aggregate is the "unique feature": it is
/// the only place in the application where cross-tour analytics happen, so
/// the logic lives in one static, easily-testable location.
/// </summary>
public static class TourStatisticsCalculator
{
    /// <summary>
    /// Empty statistics — the shape returned when the user has no tours yet.
    /// Exposed so callers can compare against it without magic literals.
    /// </summary>
    public static readonly TourStatistics Empty = new(
        TotalTours: 0,
        TotalLogs: 0,
        TotalDistanceKm: 0,
        TotalDurationMinutes: 0,
        AverageRating: 0,
        MostUsedTransportType: null,
        MostPopularTourName: null,
        HighestRatedTourName: null);

    public static TourStatistics Compute(IReadOnlyList<Tour> tours)
    {
        if (tours is null || tours.Count == 0) return Empty;

        var allLogs = tours.SelectMany(t => t.Logs).ToList();

        // Distance / duration sums use the same unit conversion as TourMapper
        // (meters → km, seconds → minutes) so what the UI displays matches.
        var totalDistanceKm = Math.Round(tours.Sum(t => t.Distance) / 1000.0, 2);
        var totalDurationMinutes = tours.Sum(t => t.Duration) / 60;

        var averageRating = allLogs.Count == 0
            ? 0.0
            : Math.Round(allLogs.Average(l => l.Rating), 2);

        // Most-used transport type: count tours per transport, take the winner.
        // Ties are broken deterministically by enum ordinal so tests are stable.
        var mostUsedTransport = tours
            .GroupBy(t => t.TransportType)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First()
            .Key
            .ToString()
            .ToLowerInvariant();

        // Most-popular tour: highest Popularity, ties broken by name for stability.
        var mostPopular = tours
            .OrderByDescending(t => t.Popularity)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .First();

        // Highest-rated tour: consider only tours that actually have logs.
        var toursWithLogs = tours.Where(t => t.Logs.Count > 0).ToList();
        string? highestRatedName = null;
        if (toursWithLogs.Count > 0)
        {
            highestRatedName = toursWithLogs
                .OrderByDescending(t => t.Logs.Average(l => l.Rating))
                .ThenBy(t => t.Name, StringComparer.Ordinal)
                .First()
                .Name;
        }

        return new TourStatistics(
            TotalTours: tours.Count,
            TotalLogs: allLogs.Count,
            TotalDistanceKm: totalDistanceKm,
            TotalDurationMinutes: totalDurationMinutes,
            AverageRating: averageRating,
            MostUsedTransportType: mostUsedTransport,
            MostPopularTourName: mostPopular.Name,
            HighestRatedTourName: highestRatedName);
    }
}
