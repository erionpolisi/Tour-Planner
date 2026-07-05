using System.ComponentModel.DataAnnotations;

namespace TourPlanner.API.Dtos.Users;

/// <summary>Body of POST /api/auth/refresh and POST /api/auth/logout.</summary>
public sealed class RefreshRequestDto
{
    [Required, StringLength(200, MinimumLength = 16)]
    public string RefreshToken { get; set; } = string.Empty;
}
