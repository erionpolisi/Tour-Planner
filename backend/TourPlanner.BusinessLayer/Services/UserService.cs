using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Dtos.Users;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Mappers;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository users,
        IPasswordHasher<User> hasher,
        ILogger<UserService> logger)
    {
        _users = users;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        var existing = await _users.GetByEmailAsync(dto.Email);
        if (existing is not null)
        {
            _logger.LogWarning("Registration attempt with already-used email");
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = string.Empty, // overwritten below — required init for record
            Avatar = null,
            CreatedAt = DateTime.UtcNow,
        };
        // Hash the password (PasswordHasher generates and embeds a fresh salt automatically).
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        await _users.AddAsync(user);
        _logger.LogInformation("Registered new user {UserId}", user.Id);
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> LoginAsync(LoginDto dto)
    {
        var user = await _users.GetByEmailAsync(dto.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown email");
            // Same message for "unknown email" and "wrong password" to avoid user enumeration.
            throw new ValidationException("Invalid credentials.");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
            throw new ValidationException("Invalid credentials.");
        }

        // If the hash format is outdated (e.g. iteration count bumped), rehash silently.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            await _users.UpdateAsync(user);
            _logger.LogInformation("Password hash upgraded for user {UserId}", user.Id);
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return UserMapper.ToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id)
            ?? throw new NotFoundException($"User {id} not found.");
        return UserMapper.ToDto(user);
    }
}
