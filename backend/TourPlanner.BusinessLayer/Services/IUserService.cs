using TourPlanner.BusinessLayer.Dtos.Users;

namespace TourPlanner.BusinessLayer.Services;

public interface IUserService
{
    Task<UserDto> RegisterAsync(RegisterDto dto);
    Task<UserDto> LoginAsync(LoginDto dto);
    Task<UserDto> GetByIdAsync(Guid id);
}
