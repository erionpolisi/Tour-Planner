using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Dtos.TourLogs;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Mappers;
using TourPlanner.DataAccessLayer.Repositories;

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

    public async Task<List<TourLogDto>> GetAllAsync()
    {
        var entities = await _logs.GetAllAsync();
        return await MapWithTourNamesAsync(entities);
    }

    public async Task<List<TourLogDto>> GetByTourIdAsync(Guid tourId)
    {
        // Verify the parent tour exists, so callers get 404 (not an empty list)
        // when they hit a non-existent tour id.
        var tour = await _tours.GetByIdAsync(tourId)
            ?? throw new NotFoundException($"Tour {tourId} not found.");

        var entities = await _logs.GetByTourIdAsync(tourId);
        return entities.Select(e => TourLogMapper.ToDto(e, tour.Name)).ToList();
    }

    public async Task<TourLogDto> GetByIdAsync(Guid id)
    {
        var entity = await _logs.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour log {id} not found.");
        var tour = await _tours.GetByIdAsync(entity.TourId);
        return TourLogMapper.ToDto(entity, tour?.Name ?? "");
    }

    public async Task<TourLogDto> CreateAsync(CreateTourLogDto dto)
    {
        // Make sure the parent tour exists before creating a log for it.
        var tour = await _tours.GetByIdAsync(dto.TourId)
            ?? throw new NotFoundException($"Tour {dto.TourId} not found.");

        TourPlanner.Domain.TourLog entity;
        try
        {
            entity = TourLogMapper.FromCreateDto(dto);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _logs.AddAsync(entity);
        _logger.LogInformation("Created tour log {LogId} for tour {TourId}", entity.Id, entity.TourId);
        return TourLogMapper.ToDto(entity, tour.Name);
    }

    public async Task<TourLogDto> UpdateAsync(Guid id, UpdateTourLogDto dto)
    {
        var entity = await _logs.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour log {id} not found.");

        try
        {
            TourLogMapper.ApplyUpdate(entity, dto);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _logs.UpdateAsync(entity);
        var tour = await _tours.GetByIdAsync(entity.TourId);
        _logger.LogInformation("Updated tour log {LogId}", id);
        return TourLogMapper.ToDto(entity, tour?.Name ?? "");
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _logs.DeleteAsync(id);
        if (!deleted) throw new NotFoundException($"Tour log {id} not found.");
        _logger.LogInformation("Deleted tour log {LogId}", id);
    }

    /// <summary>
    /// Helper: load all tours referenced by the given logs in one query,
    /// then map them. Avoids the N+1 problem for the "all logs" endpoint.
    /// </summary>
    private async Task<List<TourLogDto>> MapWithTourNamesAsync(List<TourPlanner.Domain.TourLog> entities)
    {
        if (entities.Count == 0) return new List<TourLogDto>();

        var tourIds = entities.Select(l => l.TourId).Distinct().ToList();
        var tours = new Dictionary<Guid, string>();
        foreach (var tid in tourIds)
        {
            var t = await _tours.GetByIdAsync(tid);
            if (t is not null) tours[tid] = t.Name;
        }

        return entities
            .Select(e => TourLogMapper.ToDto(e, tours.GetValueOrDefault(e.TourId, "")))
            .ToList();
    }
}
