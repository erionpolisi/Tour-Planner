using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

/// <summary>
/// Business-layer contract for tour operations.
/// Works exclusively with domain entities — DTOs live in the API layer and are
/// converted there. Business errors are signalled through the exceptions in
/// <see cref="TourPlanner.BusinessLayer.Exceptions"/>.
///
/// Every method takes an <c>ownerId</c> so per-user data isolation is enforced
/// consistently — the caller (controllers) reads it from the JWT <c>sub</c> claim.
/// </summary>
public interface ITourService
{
    Task<List<Tour>> GetAllAsync(Guid ownerId);
    Task<Tour> GetByIdAsync(Guid ownerId, Guid id);

    /// <summary>
    /// Persist a new tour. The service sets <see cref="Tour.UserId"/> from
    /// <paramref name="ownerId"/> — request bodies cannot forge ownership.
    /// </summary>
    Task<Tour> CreateAsync(Guid ownerId, Tour tour);

    /// <summary>
    /// Load the tour, apply <paramref name="applyChanges"/>, persist.
    /// Only tours owned by <paramref name="ownerId"/> can be modified.
    /// </summary>
    Task<Tour> UpdateAsync(Guid ownerId, Guid id, Action<Tour> applyChanges);

    Task DeleteAsync(Guid ownerId, Guid id);

    /// <summary>
    /// Full-text search across the given user's tours and their logs. Empty or
    /// whitespace-only queries return an empty list without hitting the database.
    /// </summary>
    Task<List<TourSearchResult>> SearchAsync(Guid ownerId, string query, int limit, CancellationToken ct = default);
}
