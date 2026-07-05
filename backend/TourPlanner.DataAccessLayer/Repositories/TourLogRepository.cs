using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

public class TourLogRepository : ITourLogRepository
{
    private readonly TourPlannerDbContext _db;
    private readonly ILogger<TourLogRepository> _logger;

    public TourLogRepository(TourPlannerDbContext db, ILogger<TourLogRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<TourLog>> GetAllAsync()
    {
        _logger.LogInformation("Loading all tour logs");
        return await _db.TourLogs
            .AsNoTracking()
            .Include(l => l.Tour)
            .ToListAsync();
    }

    public async Task<List<TourLog>> GetByTourIdAsync(Guid tourId)
    {
        _logger.LogInformation("Loading logs for tour {TourId}", tourId);
        return await _db.TourLogs
            .AsNoTracking()
            .Include(l => l.Tour)
            .Where(l => l.TourId == tourId)
            .OrderByDescending(l => l.LoggedAt)
            .ToListAsync();
    }

    public async Task<TourLog?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Loading tour log {LogId}", id);
        return await _db.TourLogs
            .Include(l => l.Tour)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task AddAsync(TourLog log)
    {
        _logger.LogInformation("Adding new tour log for tour {TourId}", log.TourId);
        _db.TourLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(TourLog log)
    {
        _logger.LogInformation("Updating tour log {LogId}", log.Id);
        _db.TourLogs.Update(log);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var log = await _db.TourLogs.FindAsync(id);
        if (log is null)
        {
            _logger.LogWarning("Tried to delete tour log {LogId}, but it was not found", id);
            return false;
        }

        _logger.LogInformation("Deleting tour log {LogId}", id);
        _db.TourLogs.Remove(log);
        await _db.SaveChangesAsync();
        return true;
    }
}
