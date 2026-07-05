using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// Contract for accessing tours in the database.
/// The business layer depends on this interface, not on the concrete
/// implementation — that's what makes the code testable and the DAL swappable.
/// </summary>
public interface ITourRepository
{
    Task<List<Tour>> GetAllAsync();
    Task<Tour?> GetByIdAsync(Guid id);
    Task AddAsync(Tour tour);
    Task UpdateAsync(Tour tour);
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// PostgreSQL full-text search over tours and their logs. Returns each
    /// matching tour together with the specific logs that also matched
    /// (may be empty when only the tour's own text matched).
    /// </summary>
    /// <param name="query">User-supplied search string. Never null/empty when called from the service.</param>
    /// <param name="limit">Maximum number of tours to return. Values &lt;= 0 fall back to 50.</param>
    /// <param name="ct">Cancellation token propagated from the HTTP request.</param>
    Task<List<TourSearchHit>> SearchAsync(string query, int limit, CancellationToken ct = default);
}
