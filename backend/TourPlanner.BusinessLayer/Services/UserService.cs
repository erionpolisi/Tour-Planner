using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services.Auth;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IPasswordPolicy _passwordPolicy;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository users,
        IPasswordHasher<User> hasher,
        IPasswordPolicy passwordPolicy,
        ILogger<UserService> logger)
    {
        _users = users;
        _hasher = hasher;
        _passwordPolicy = passwordPolicy;
        _logger = logger;
    }

    public async Task<User> RegisterAsync(string name, string email, string password)
    {
        // NIST SP 800-63B: check the password against a common-passwords list
        // *before* creating the account, so we don't leak whether the email exists.
        var policyError = _passwordPolicy.Validate(password, email: email, name: name);
        if (policyError is not null)
        {
            _logger.LogWarning("Registration rejected by password policy");
            throw new ValidationException(policyError);
        }

        var existing = await _users.GetByEmailAsync(email);
        if (existing is not null)
        {
            _logger.LogWarning("Registration attempt with already-used email");
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = string.Empty, // overwritten below — required init for record
            Avatar = null,
            CreatedAt = DateTime.UtcNow,
        };
        // Hash the password (PasswordHasher generates and embeds a fresh salt automatically).
        user.PasswordHash = _hasher.HashPassword(user, password);

        await _users.AddAsync(user);
        _logger.LogInformation("Registered new user {UserId}", user.Id);
        return user;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
        var user = await _users.GetByEmailAsync(email);
        if (user is null)
        {
            _logger.LogWarning("Login failed: unknown email");
            // Same message for "unknown email" and "wrong password" to avoid user enumeration.
            throw new ValidationException("Invalid credentials.");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
            throw new ValidationException("Invalid credentials.");
        }

        // If the hash format is outdated (e.g. iteration count bumped), rehash silently.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
            await _users.UpdateAsync(user);
            _logger.LogInformation("Password hash upgraded for user {UserId}", user.Id);
        }

        _logger.LogInformation("User {UserId} logged in", user.Id);
        return user;
    }

    public async Task<User> GetByIdAsync(Guid id) =>
        await _users.GetByIdAsync(id)
            ?? throw new NotFoundException($"User {id} not found.");
}
