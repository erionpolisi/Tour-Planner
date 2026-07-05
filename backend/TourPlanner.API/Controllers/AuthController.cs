using Microsoft.AspNetCore.Mvc;
using TourPlanner.API.Dtos.Users;
using TourPlanner.API.Mappers;
using TourPlanner.BusinessLayer.Services;

namespace TourPlanner.API.Controllers;

/// <summary>
/// Auth endpoints — register & login.
/// Returns a <see cref="UserDto"/> on success; the password hash is never exposed.
///
/// NOTE: No real session/JWT yet. The frontend stores the user in memory after
/// login and the auth-guard reads that. Real tokens will replace this later.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService users, ILogger<AuthController> logger)
    {
        _users = users;
        _logger = logger;
    }

    /// <summary>POST /api/auth/register — create a new account.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto dto)
    {
        var user = await _users.RegisterAsync(dto.Name, dto.Email, dto.Password);
        _logger.LogInformation("User registered: {UserId} ({Email})", user.Id, user.Email);
        return CreatedAtAction(nameof(GetMe), new { id = user.Id }, UserMapper.ToDto(user));
    }

    /// <summary>POST /api/auth/login — verify credentials, return profile.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _users.LoginAsync(dto.Email, dto.Password);
        _logger.LogInformation("User logged in: {UserId}", user.Id);
        return Ok(UserMapper.ToDto(user));
    }

    /// <summary>GET /api/auth/{id} — fetch a user profile (placeholder for /me).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetMe(Guid id)
    {
        var user = await _users.GetByIdAsync(id);
        return Ok(UserMapper.ToDto(user));
    }
}
