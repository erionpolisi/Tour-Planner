using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// Contract for accessing users in the database.
/// </summary>
public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid id);
}
