using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TourPlannerDbContext _db;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(TourPlannerDbContext db, ILogger<UserRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<User>> GetAllAsync()
    {
        _logger.LogInformation("Loading all users");
        return await _db.Users.AsNoTracking().ToListAsync();
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Loading user {UserId}", id);
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("Looking up user by email");
        // Email is unique by DB constraint, so SingleOrDefault is appropriate.
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddAsync(User user)
    {
        _logger.LogInformation("Adding new user {Email}", user.Email);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _logger.LogInformation("Updating user {UserId}", user.Id);
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            _logger.LogWarning("Tried to delete user {UserId}, but it was not found", id);
            return false;
        }

        _logger.LogInformation("Deleting user {UserId}", id);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return true;
    }
}
