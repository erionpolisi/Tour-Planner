using System.ComponentModel.DataAnnotations;

namespace TourPlanner.BusinessLayer.Dtos.Users;

public class LoginDto
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
