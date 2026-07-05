using TourPlanner.API.Dtos.TourLogs;

namespace TourPlanner.API.Dtos.Tours;

/// <summary>
/// One entry in a full-text search response.
/// The tour is always populated; <see cref="MatchedLogs"/> is only populated
/// with logs that themselves matched the query (may be empty when only the
/// tour's own text matched).
/// </summary>
public class TourSearchResultDto
{
    public TourDto Tour { get; set; } = new();

    /// <summary>True when the tour's own text (name / description / from / to / ...) matched.</summary>
    public bool MatchedInTour { get; set; }

    /// <summary>Logs whose text also matched the query. Ordered newest first.</summary>
    public List<TourLogDto> MatchedLogs { get; set; } = new();
}
