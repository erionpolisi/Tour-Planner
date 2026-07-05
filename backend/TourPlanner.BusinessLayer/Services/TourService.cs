using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

public class TourService : ITourService
{
    private readonly ITourRepository _tours;
    private readonly ILogger<TourService> _logger;

    public TourService(ITourRepository tours, ILogger<TourService> logger)
    {
        _tours = tours;
        _logger = logger;
    }

    public Task<List<Tour>> GetAllAsync() => _tours.GetAllAsync();

    public async Task<Tour> GetByIdAsync(Guid id) =>
        await _tours.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour {id} not found.");

    public async Task<Tour> CreateAsync(Tour tour)
    {
        await _tours.AddAsync(tour);
        _logger.LogInformation("Created tour {TourId} ({Name})", tour.Id, tour.Name);
        return tour;
    }

    public async Task<Tour> UpdateAsync(Guid id, Action<Tour> applyChanges)
    {
        var entity = await _tours.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour {id} not found.");

        applyChanges(entity);

        await _tours.UpdateAsync(entity);
        _logger.LogInformation("Updated tour {TourId}", id);
        return entity;
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _tours.DeleteAsync(id);
        if (!deleted) throw new NotFoundException($"Tour {id} not found.");
        _logger.LogInformation("Deleted tour {TourId}", id);
    }
}
