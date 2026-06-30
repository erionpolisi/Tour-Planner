using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// Contract for accessing tour logs in the database.
/// </summary>
public interface ITourLogRepository
{
    Task<List<TourLog>> GetAllAsync();
    Task<List<TourLog>> GetByTourIdAsync(Guid tourId);
    Task<TourLog?> GetByIdAsync(Guid id);
    Task AddAsync(TourLog log);
    Task UpdateAsync(TourLog log);
    Task<bool> DeleteAsync(Guid id);
}
