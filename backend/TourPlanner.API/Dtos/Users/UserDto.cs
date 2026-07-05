namespace TourPlanner.API.Dtos.Users;

/// <summary>
/// Public user profile.
/// PasswordHash is intentionally NEVER exposed to the API.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public DateTime CreatedAt { get; set; }
}
