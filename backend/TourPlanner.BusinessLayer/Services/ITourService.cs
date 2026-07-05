using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

/// <summary>
/// Business-layer contract for tour operations.
/// Works exclusively with domain entities — DTOs live in the API layer and are
/// converted there. Business errors are signalled through the exceptions in
/// <see cref="TourPlanner.BusinessLayer.Exceptions"/>.
/// </summary>
public interface ITourService
{
    Task<List<Tour>> GetAllAsync();
    Task<Tour> GetByIdAsync(Guid id);

    /// <summary>Persist a new tour. The passed entity must already be fully valid.</summary>
    Task<Tour> CreateAsync(Tour tour);

    /// <summary>
    /// Load the tour, apply <paramref name="applyChanges"/>, persist.
    /// The caller (usually a controller-side mapper) mutates the tracked entity
    /// so we don't need a full DTO here — only the diff logic.
    /// </summary>
    Task<Tour> UpdateAsync(Guid id, Action<Tour> applyChanges);

    Task DeleteAsync(Guid id);

    /// <summary>
    /// Full-text search across tours and their logs. Empty or whitespace-only
    /// queries return an empty list without hitting the database.
    /// </summary>
    /// <param name="query">Free-text query. Supports web-search syntax (quotes, OR, -negation).</param>
    /// <param name="limit">Maximum tours to return. Values &lt;= 0 use a sensible default; extreme values are capped by the repository.</param>
    /// <param name="ct">Cancellation token propagated from the HTTP request.</param>
    Task<List<TourSearchResult>> SearchAsync(string query, int limit, CancellationToken ct = default);
}
