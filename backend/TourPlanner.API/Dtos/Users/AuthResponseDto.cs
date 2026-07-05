namespace TourPlanner.API.Dtos.Users;

/// <summary>
/// Response returned by /register, /login and /refresh.
/// The client stores <see cref="AccessToken"/> in memory and attaches it as
/// <c>Authorization: Bearer</c>; <see cref="RefreshToken"/> is kept in
/// localStorage / sessionStorage and exchanged for a new pair when the access
/// token expires.
/// </summary>
public sealed record AuthResponseDto(
    UserDto User,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
