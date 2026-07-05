using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services.Stats;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

public class TourLogService : ITourLogService
{
    private readonly ITourLogRepository _logs;
    private readonly ITourRepository _tours;
    private readonly ILogger<TourLogService> _logger;

    public TourLogService(
        ITourLogRepository logs,
        ITourRepository tours,
        ILogger<TourLogService> logger)
    {
        _logs = logs;
        _tours = tours;
        _logger = logger;
    }

    public Task<List<TourLog>> GetAllAsync(Guid ownerId) => _logs.GetAllAsync(ownerId);

    public async Task<List<TourLog>> GetByTourIdAsync(Guid ownerId, Guid tourId)
    {
        // Verify the parent tour exists AND is owned by the caller — otherwise
        // an unowned/missing tour would just return an empty list, which
        // masks the difference between "tour not found" and "no logs yet".
        _ = await _tours.GetByIdAsync(ownerId, tourId)
            ?? throw new NotFoundException($"Tour {tourId} not found.");

        return await _logs.GetByTourIdAsync(ownerId, tourId);
    }

    public async Task<TourLog> GetByIdAsync(Guid ownerId, Guid id) =>
        await _logs.GetByIdAsync(ownerId, id)
            ?? throw new NotFoundException($"Tour log {id} not found.");

    public async Task<TourLog> CreateAsync(Guid ownerId, TourLog log)
    {
        // Make sure the parent tour exists AND is owned by the caller before
        // creating a log for it.
        var tour = await _tours.GetByIdAsync(ownerId, log.TourId)
            ?? throw new NotFoundException($"Tour {log.TourId} not found.");

        await _logs.AddAsync(log);
        _logger.LogInformation("Created tour log {LogId} for tour {TourId}", log.Id, log.TourId);

        await RecomputeTourStatsAsync(tour);

        // Reload so the Tour navigation is populated for the mapper on the way out.
        return await _logs.GetByIdAsync(ownerId, log.Id) ?? log;
    }

    public async Task<TourLog> UpdateAsync(Guid ownerId, Guid id, Action<TourLog> applyChanges)
    {
        var entity = await _logs.GetByIdAsync(ownerId, id)
            ?? throw new NotFoundException($"Tour log {id} not found.");

        applyChanges(entity);

        await _logs.UpdateAsync(entity);
        _logger.LogInformation("Updated tour log {LogId}", id);

        // Difficulty / duration may have changed → recompute child-friendliness.
        if (entity.Tour is not null)
        {
            await RecomputeTourStatsAsync(entity.Tour);
        }

        return entity;
    }

    public async Task DeleteAsync(Guid ownerId, Guid id)
    {
        // Load first so we know which tour to recompute after the delete.
        var log = await _logs.GetByIdAsync(ownerId, id)
            ?? throw new NotFoundException($"Tour log {id} not found.");

        var deleted = await _logs.DeleteAsync(ownerId, id);
        if (!deleted) throw new NotFoundException($"Tour log {id} not found.");
        _logger.LogInformation("Deleted tour log {LogId}", id);

        if (log.Tour is not null)
        {
            await RecomputeTourStatsAsync(log.Tour);
        }
    }

    // -----------------------------------------------------------------
    // Stats maintenance
    // -----------------------------------------------------------------

    /// <summary>
    /// Reads every log for the given tour and updates the tour's persisted
    /// popularity and child-friendliness columns. Called after every log
    /// create / update / delete so both columns — and the tsvector that
    /// indexes them — stay in sync with reality.
    /// </summary>
    private async Task RecomputeTourStatsAsync(Tour tour)
    {
        var logs = await _logs.GetForTourAsync(tour.Id);
        tour.Popularity = TourStatsCalculator.Popularity(logs);
        tour.ChildFriendliness = TourStatsCalculator.ChildFriendlinessScore(tour.Distance, logs);
        await _tours.UpdateAsync(tour);
        _logger.LogInformation(
            "Refreshed tour stats for {TourId}: popularity={Popularity} childFriendliness={ChildFriendliness}",
            tour.Id, tour.Popularity, tour.ChildFriendliness);
    }
}
