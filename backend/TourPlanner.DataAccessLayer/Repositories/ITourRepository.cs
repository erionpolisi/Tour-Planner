using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// Contract for accessing tours in the database.
/// Every method takes an owner <see cref="Guid"/> so per-user data isolation
/// is enforced at the persistence boundary — a caller cannot accidentally
/// return another user's tours by forgetting a WHERE clause.
/// </summary>
public interface ITourRepository
{
    /// <summary>All tours owned by <paramref name="ownerId"/>.</summary>
    Task<List<Tour>> GetAllAsync(Guid ownerId);

    /// <summary>The tour with the given id — only if it belongs to <paramref name="ownerId"/>.</summary>
    Task<Tour?> GetByIdAsync(Guid ownerId, Guid id);

    /// <summary>
    /// Persist a new tour. The caller is expected to have set
    /// <see cref="Tour.UserId"/> to the owner already.
    /// </summary>
    Task AddAsync(Tour tour);

    Task UpdateAsync(Tour tour);

    /// <summary>Delete the given tour — only if it belongs to <paramref name="ownerId"/>.</summary>
    Task<bool> DeleteAsync(Guid ownerId, Guid id);

    /// <summary>Like <see cref="GetAllAsync"/> but eager-loads each tour's logs. Used by the export flow.</summary>
    Task<List<Tour>> GetAllWithLogsAsync(Guid ownerId, CancellationToken ct = default);

    /// <summary>
    /// PostgreSQL full-text search over the given user's tours + their logs.
    /// Returns each matching tour together with the specific logs that also
    /// matched (may be empty when only the tour's own text matched).
    /// </summary>
    Task<List<TourSearchHit>> SearchAsync(Guid ownerId, string query, int limit, CancellationToken ct = default);
}
