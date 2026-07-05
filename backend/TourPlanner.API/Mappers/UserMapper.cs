using TourPlanner.API.Dtos.Users;
using TourPlanner.Domain;

namespace TourPlanner.API.Mappers;

public static class UserMapper
{
    /// <summary>Maps a user entity to its public DTO — PasswordHash is intentionally omitted.</summary>
    public static UserDto ToDto(User entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Email = entity.Email,
        Avatar = entity.Avatar,
        CreatedAt = entity.CreatedAt,
    };
}
