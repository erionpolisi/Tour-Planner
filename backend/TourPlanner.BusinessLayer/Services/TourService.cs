using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Dtos.Tours;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Mappers;
using TourPlanner.DataAccessLayer.Repositories;

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

    public async Task<List<TourDto>> GetAllAsync()
    {
        var entities = await _tours.GetAllAsync();
        return entities.Select(TourMapper.ToDto).ToList();
    }

    public async Task<TourDto> GetByIdAsync(Guid id)
    {
        var entity = await _tours.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour {id} not found.");
        return TourMapper.ToDto(entity);
    }

    public async Task<TourDto> CreateAsync(CreateTourDto dto)
    {
        Domain.Tour entity;
        try
        {
            entity = TourMapper.FromCreateDto(dto);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _tours.AddAsync(entity);
        _logger.LogInformation("Created tour {TourId} ({Name})", entity.Id, entity.Name);
        return TourMapper.ToDto(entity);
    }

    public async Task<TourDto> UpdateAsync(Guid id, UpdateTourDto dto)
    {
        var entity = await _tours.GetByIdAsync(id)
            ?? throw new NotFoundException($"Tour {id} not found.");

        try
        {
            TourMapper.ApplyUpdate(entity, dto);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _tours.UpdateAsync(entity);
        _logger.LogInformation("Updated tour {TourId}", id);
        return TourMapper.ToDto(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var deleted = await _tours.DeleteAsync(id);
        if (!deleted) throw new NotFoundException($"Tour {id} not found.");
        _logger.LogInformation("Deleted tour {TourId}", id);
    }
}
