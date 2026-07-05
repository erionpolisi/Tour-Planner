using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using TourPlanner.API.Dtos.Users;
using TourPlanner.API.Mappers;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.BusinessLayer.Services.Auth;

namespace TourPlanner.API.Controllers;

/// <summary>
/// Auth endpoints — register, login, refresh, logout, and /me.
///
/// Returns an <see cref="AuthResponseDto"/> on register/login/refresh:
/// short-lived JWT access token + long-lived refresh token for the session.
/// The password hash is never exposed.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IAuthSessionService _sessions;
    private readonly IJwtTokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserService users,
        IAuthSessionService sessions,
        IJwtTokenService tokens,
        ILogger<AuthController> logger)
    {
        _users = users;
        _sessions = sessions;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>POST /api/auth/register — create a new account and start a session.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        var user = await _users.RegisterAsync(dto.Name, dto.Email, dto.Password);
        _logger.LogInformation("User registered: {UserId}", user.Id);

        var response = await IssueSessionAsync(user);
        return CreatedAtAction(nameof(Me), routeValues: null, value: response);
    }

    /// <summary>POST /api/auth/login — verify credentials, start a session.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _users.LoginAsync(dto.Email, dto.Password);
        _logger.LogInformation("User logged in: {UserId}", user.Id);

        var response = await IssueSessionAsync(user);
        return Ok(response);
    }

    /// <summary>POST /api/auth/refresh — rotate the refresh token, issue a fresh access token.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
    {
        try
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (user, refreshToken, refreshExpires) =
                await _sessions.RotateAsync(dto.RefreshToken, clientIp);
            var access = _tokens.CreateAccessToken(user);

            return Ok(new AuthResponseDto(
                UserMapper.ToDto(user),
                access.Value,
                access.ExpiresAtUtc,
                refreshToken,
                refreshExpires));
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning("Refresh failed: {Reason}", ex.Message);
            return Unauthorized(new { error = "Invalid refresh token." });
        }
    }

    /// <summary>POST /api/auth/logout — revoke the current refresh token.</summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto? dto)
    {
        if (!string.IsNullOrWhiteSpace(dto?.RefreshToken))
        {
            await _sessions.RevokeAsync(dto.RefreshToken);
        }
        return NoContent();
    }

    /// <summary>GET /api/auth/me — profile of the authenticated user.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await _users.GetByIdAsync(userId);
        return Ok(UserMapper.ToDto(user));
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateUserDto dto)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _users.UpdateAsync(
            userId,
            dto.Name,
            dto.Email,
            dto.Password);

        return Ok(UserMapper.ToDto(user));
    }

    private async Task<AuthResponseDto> IssueSessionAsync(TourPlanner.Domain.User user)
    {
        var access = _tokens.CreateAccessToken(user);
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (refreshToken, refreshExpires) = await _sessions.IssueAsync(user.Id, clientIp);

        return new AuthResponseDto(
            UserMapper.ToDto(user),
            access.Value,
            access.ExpiresAtUtc,
            refreshToken,
            refreshExpires);
    }
}
