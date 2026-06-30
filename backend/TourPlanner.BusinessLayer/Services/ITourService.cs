using TourPlanner.BusinessLayer.Dtos.Tours;

namespace TourPlanner.BusinessLayer.Services;

public interface ITourService
{
    Task<List<TourDto>> GetAllAsync();
    Task<TourDto> GetByIdAsync(Guid id);
    Task<TourDto> CreateAsync(CreateTourDto dto);
    Task<TourDto> UpdateAsync(Guid id, UpdateTourDto dto);
    Task DeleteAsync(Guid id);
}
