using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

/// <summary>
/// Business-layer contract for user/auth operations. Works with the domain
/// <see cref="User"/> entity plus primitive credentials — DTOs live in the
/// API layer.
/// </summary>
public interface IUserService
{
    /// <summary>Create a new user account. Password is hashed inside the service.</summary>
    Task<User> RegisterAsync(string name, string email, string password);

    /// <summary>Verify credentials, return the user on success.</summary>
    Task<User> LoginAsync(string email, string password);

    Task<User> GetByIdAsync(Guid id);
}
