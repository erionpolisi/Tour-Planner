using TourPlanner.API.Dtos.Tours;
using TourPlanner.BusinessLayer.Services;

namespace TourPlanner.API.Mappers;

/// <summary>
/// Converts a business-layer <see cref="TourSearchResult"/> into the API DTO.
/// Delegates the individual tour and log mappings to the existing mappers so
/// unit conversion and enum handling stay in one place.
/// </summary>
public static class TourSearchResultMapper
{
    public static TourSearchResultDto ToDto(TourSearchResult result) => new()
    {
        Tour = TourMapper.ToDto(result.Tour),
        MatchedInTour = result.MatchedInTour,
        MatchedLogs = result.MatchedLogs
            .Select(log =>
            {
                // The repository loaded logs via a projection, so their .Tour
                // navigation is null. Stitch the tour reference in by hand so
                // TourLogMapper can still surface TourName.
                log.Tour ??= result.Tour;
                return TourLogMapper.ToDto(log);
            })
            .ToList(),
    };
}
