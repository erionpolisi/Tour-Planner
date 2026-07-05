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
}
