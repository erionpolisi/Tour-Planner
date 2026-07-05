using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// One row of a full-text search result.
/// Kept as a plain record in the DAL so the business layer doesn't need to
/// take a dependency on the internal EF <see cref="Tour"/> / <see cref="TourLog"/>
/// entities beyond what it already does.
/// </summary>
/// <param name="Tour">Matching tour (fully loaded, tracked or not — caller decides).</param>
/// <param name="MatchedInTour">True when the tour's own text matched the query.</param>
/// <param name="MatchedLogs">Only the logs whose text matched. May be empty.</param>
public sealed record TourSearchHit(
    Tour Tour,
    bool MatchedInTour,
    IReadOnlyList<TourLog> MatchedLogs);
