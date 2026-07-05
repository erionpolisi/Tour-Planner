using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

/// <summary>
/// Business-layer result of a full-text search over tours and their logs.
/// Same shape as the DAL's <c>TourSearchHit</c> but re-declared here so the
/// business layer's public contract doesn't leak DAL types to API consumers.
/// </summary>
public sealed record TourSearchResult(
    Tour Tour,
    bool MatchedInTour,
    IReadOnlyList<TourLog> MatchedLogs);
