using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

public class TourLogRepository : ITourLogRepository
{
    private const int DefaultSearchLimit = 50;
    private const int MaxSearchLimit = 200;

    private readonly TourPlannerDbContext _db;
    private readonly ILogger<TourLogRepository> _logger;

    public TourLogRepository(TourPlannerDbContext db, ILogger<TourLogRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<TourLog>> GetAllAsync(Guid ownerId)
    {
        _logger.LogInformation("Loading all tour logs for user {UserId}", ownerId);
        return await _db.TourLogs
            .AsNoTracking()
            .Include(l => l.Tour)
            .Where(l => l.Tour!.UserId == ownerId)
            .ToListAsync();
    }

    public async Task<List<TourLog>> GetByTourIdAsync(Guid ownerId, Guid tourId)
    {
        _logger.LogInformation("Loading logs for tour {TourId} (user {UserId})", tourId, ownerId);
        return await _db.TourLogs
            .AsNoTracking()
            .Include(l => l.Tour)
            .Where(l => l.TourId == tourId && l.Tour!.UserId == ownerId)
            .OrderByDescending(l => l.LoggedAt)
            .ToListAsync();
    }

    public async Task<TourLog?> GetByIdAsync(Guid ownerId, Guid id)
    {
        _logger.LogInformation("Loading tour log {LogId} for user {UserId}", id, ownerId);
        return await _db.TourLogs
            .Include(l => l.Tour)
            .FirstOrDefaultAsync(l => l.Id == id && l.Tour!.UserId == ownerId);
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

    public async Task<List<TourLog>> SearchAsync(
        Guid ownerId,
        string query,
        int limit,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<TourLog>();
        }

        var effectiveLimit = limit switch
        {
            <= 0 => DefaultSearchLimit,
            > MaxSearchLimit => MaxSearchLimit,
            _ => limit,
        };

        _logger.LogInformation(
            "Full-text search for logs: user={UserId} len={QueryLength} limit={Limit}",
            ownerId,
            query.Length,
            effectiveLimit);

        return await _db.TourLogs
            .AsNoTracking()
            .Include(l => l.Tour)
            .Where(l =>
                l.Tour != null &&
                l.Tour.UserId == ownerId &&
                EF.Property<NpgsqlTsVector>(
                        l,
                        TourPlannerDbContext.SearchVectorColumn)
                    .Matches(EF.Functions.WebSearchToTsQuery("simple", query)))
            .OrderByDescending(l => l.LoggedAt)
            .Take(effectiveLimit)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid id)
    {
        var log = await _db.TourLogs
            .Include(l => l.Tour)
            .FirstOrDefaultAsync(l => l.Id == id && l.Tour!.UserId == ownerId);
        if (log is null)
        {
            _logger.LogWarning(
                "Tried to delete tour log {LogId} for user {UserId}, but it was not found or not owned",
                id, ownerId);
            return false;
        }

        _logger.LogInformation("Deleting tour log {LogId}", id);
        _db.TourLogs.Remove(log);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<TourLog>> GetForTourAsync(Guid tourId)
    {
        // No owner filter — service-layer callers use this for stats recomputation
        // AFTER they've already verified tour ownership.
        return await _db.TourLogs
            .AsNoTracking()
            .Where(l => l.TourId == tourId)
            .ToListAsync();
    }
}
