using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Exceptions;
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

    public Task<List<TourLog>> GetAllAsync() => _logs.GetAllAsync();

    public async Task<List<TourLog>> GetByTourIdAsync(Guid tourId)
    {
        // Verify the parent tour exists, so callers get 404 (not an empty list)
        // when they hit a non-existent tour id.
        _ = await _tours.GetByIdAsync(tourId)
            ?? throw new NotFoundException($"Tour {tourId} not found.");

        return await _logs.GetByTourIdAsync(tourId);
    }

    public async Task<TourLog> GetByIdAsync(Guid id) =>
        await _logs.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour log {id} not found.");

    public async Task<TourLog> CreateAsync(TourLog log)
    {
        // Make sure the parent tour exists before creating a log for it.
        _ = await _tours.GetByIdAsync(log.TourId)
            ?? throw new NotFoundException($"Tour {log.TourId} not found.");

        await _logs.AddAsync(log);
        _logger.LogInformation("Created tour log {LogId} for tour {TourId}", log.Id, log.TourId);

        // Reload so the Tour navigation is populated for the mapper on the way out.
        return await _logs.GetByIdAsync(log.Id) ?? log;
    }

    public async Task<TourLog> UpdateAsync(Guid id, Action<TourLog> applyChanges)
    {
        var entity = await _logs.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour log {id} not found.");

        applyChanges(entity);

        await _logs.UpdateAsync(entity);
        _logger.LogInformation("Updated tour log {LogId}", id);
        return entity;
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _logs.DeleteAsync(id);
        if (!deleted) throw new NotFoundException($"Tour log {id} not found.");
        _logger.LogInformation("Deleted tour log {LogId}", id);
    }
}
