using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// Contract for accessing tour logs in the database.
/// Reads are scoped to an owner so a user can never see logs of tours
/// they don't own. Writes rely on the caller (<c>TourLogService</c>)
/// verifying tour ownership first.
/// </summary>
public interface ITourLogRepository
{
    /// <summary>Every log across every tour owned by <paramref name="ownerId"/>.</summary>
    Task<List<TourLog>> GetAllAsync(Guid ownerId);

    /// <summary>
    /// Logs for a specific tour — only if the tour is owned by <paramref name="ownerId"/>.
    /// Returns an empty list when the tour is not owned; callers should first
    /// check tour existence via <c>ITourRepository.GetByIdAsync</c> to differentiate
    /// "tour missing" from "tour exists but has no logs".
    /// </summary>
    Task<List<TourLog>> GetByTourIdAsync(Guid ownerId, Guid tourId);

    /// <summary>The log with the given id — only if the parent tour belongs to <paramref name="ownerId"/>.</summary>
    Task<TourLog?> GetByIdAsync(Guid ownerId, Guid id);

    Task AddAsync(TourLog log);
    Task UpdateAsync(TourLog log);

    /// <summary>Delete the given log — only if the parent tour belongs to <paramref name="ownerId"/>.</summary>
    Task<bool> DeleteAsync(Guid ownerId, Guid id);

    /// <summary>All logs for a tour (no owner check). Used by the service layer during stats recomputation.</summary>
    Task<List<TourLog>> GetForTourAsync(Guid tourId);
}
