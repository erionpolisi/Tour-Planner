using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

/// <summary>
/// Business-layer contract for tour-log operations. Works exclusively with
/// domain entities — DTOs live in the API layer and are converted there.
/// Returned entities have their <see cref="TourLog.Tour"/> navigation loaded
/// so the API mapper can read the tour name without a second round-trip.
///
/// Every method takes an <c>ownerId</c> so callers cannot cross ownership
/// boundaries — the log's parent tour must belong to the user.
/// </summary>
public interface ITourLogService
{
    Task<List<TourLog>> GetAllAsync(Guid ownerId);

    /// <summary>Returns logs for a specific tour. Throws NotFoundException if the tour doesn't exist for the given owner.</summary>
    Task<List<TourLog>> GetByTourIdAsync(Guid ownerId, Guid tourId);

    Task<TourLog> GetByIdAsync(Guid ownerId, Guid id);

    /// <summary>Persist a new log. The parent tour must exist and be owned by <paramref name="ownerId"/>.</summary>
    Task<TourLog> CreateAsync(Guid ownerId, TourLog log);

    /// <summary>Load the log, apply changes, persist.</summary>
    Task<TourLog> UpdateAsync(Guid ownerId, Guid id, Action<TourLog> applyChanges);

    Task<List<TourLog>> SearchAsync(
        Guid ownerId,
        string query,
        int limit,
        CancellationToken ct = default);

    Task DeleteAsync(Guid ownerId, Guid id);
}
