using TourPlanner.BusinessLayer.Dtos.TourLogs;

namespace TourPlanner.BusinessLayer.Services;

public interface ITourLogService
{
    Task<List<TourLogDto>> GetAllAsync();
    Task<List<TourLogDto>> GetByTourIdAsync(Guid tourId);
    Task<TourLogDto> GetByIdAsync(Guid id);
    Task<TourLogDto> CreateAsync(CreateTourLogDto dto);
    Task<TourLogDto> UpdateAsync(Guid id, UpdateTourLogDto dto);
    Task DeleteAsync(Guid id);
}
