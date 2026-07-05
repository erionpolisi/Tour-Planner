using System.ComponentModel.DataAnnotations;

namespace TourPlanner.API.Dtos.Users;

public class RegisterDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Plain-text password. Will be hashed in the service layer; never stored as-is.</summary>
    [Required, StringLength(200, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
