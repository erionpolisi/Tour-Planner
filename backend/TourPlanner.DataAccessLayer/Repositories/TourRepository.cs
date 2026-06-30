using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITourRepository"/>.
/// All public methods log at Information level so we can trace DB access.
/// </summary>
public class TourRepository : ITourRepository
{
    private readonly TourPlannerDbContext _db;
    private readonly ILogger<TourRepository> _logger;

    public TourRepository(TourPlannerDbContext db, ILogger<TourRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Tour>> GetAllAsync()
    {
        _logger.LogInformation("Loading all tours");
        // AsNoTracking → EF doesn't track these entities for change-detection.
        // Faster + uses less memory when we only want to READ data.
        return await _db.Tours.AsNoTracking().ToListAsync();
    }

    public async Task<Tour?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Loading tour {TourId}", id);
        return await _db.Tours.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(Tour tour)
    {
        _logger.LogInformation("Adding new tour {TourName}", tour.Name);
        _db.Tours.Add(tour);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tour tour)
    {
        _logger.LogInformation("Updating tour {TourId}", tour.Id);
        _db.Tours.Update(tour);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tour = await _db.Tours.FindAsync(id);
        if (tour is null)
        {
            _logger.LogWarning("Tried to delete tour {TourId}, but it was not found", id);
            return false;
        }

        _logger.LogInformation("Deleting tour {TourId}", id);
        _db.Tours.Remove(tour);
        await _db.SaveChangesAsync();
        return true;
    }
}
