using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

/// <summary>
/// Business-layer contract for tour-log operations. Works exclusively with
/// domain entities — DTOs live in the API layer and are converted there.
/// Returned entities have their <see cref="TourLog.Tour"/> navigation loaded
/// so the API mapper can read the tour name without a second round-trip.
/// </summary>
public interface ITourLogService
{
    Task<List<TourLog>> GetAllAsync();

    /// <summary>Returns logs for a specific tour. Throws NotFoundException if the tour doesn't exist.</summary>
    Task<List<TourLog>> GetByTourIdAsync(Guid tourId);

    Task<TourLog> GetByIdAsync(Guid id);

    /// <summary>Persist a new log. The parent tour must exist.</summary>
    Task<TourLog> CreateAsync(TourLog log);

    /// <summary>Load the log, apply changes, persist.</summary>
    Task<TourLog> UpdateAsync(Guid id, Action<TourLog> applyChanges);

    Task DeleteAsync(Guid id);
}
