using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Statistics;

/// <summary>
/// Aggregate statistics across every tour owned by a single user.
/// Pure record — computed lazily by <see cref="TourStatisticsCalculator"/>,
/// never persisted.
/// </summary>
public sealed record TourStatistics(
    int TotalTours,
    int TotalLogs,
    /// <summary>Sum of every tour's planned distance, in kilometres.</summary>
    double TotalDistanceKm,
    /// <summary>Sum of every tour's planned duration, in minutes.</summary>
    int TotalDurationMinutes,
    /// <summary>Average rating across every log (0..5). 0 when there are no logs.</summary>
    double AverageRating,
    /// <summary>Which transport type appears on the most tours (or null when there are no tours).</summary>
    string? MostUsedTransportType,
    /// <summary>Name of the tour with the highest <see cref="Tour.Popularity"/>. Null when there are no tours.</summary>
    string? MostPopularTourName,
    /// <summary>Name of the tour whose logs have the highest average rating. Null when no logs.</summary>
    string? HighestRatedTourName);
