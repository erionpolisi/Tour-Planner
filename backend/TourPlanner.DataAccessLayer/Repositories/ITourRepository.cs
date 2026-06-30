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
}
