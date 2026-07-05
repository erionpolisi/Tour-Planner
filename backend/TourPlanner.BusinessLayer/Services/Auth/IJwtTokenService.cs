using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Auth;

/// <summary>
/// Result of issuing a JWT access token: the signed token itself plus its
/// unique id (jti — useful for logging / revocation lists) and expiry.
/// </summary>
public sealed record AccessToken(string Value, string TokenId, DateTime ExpiresAtUtc);

/// <summary>
/// Signs and issues JWT access tokens.
/// Validation of incoming tokens is done by ASP.NET Core's JwtBearer middleware
/// (configured in Program.cs) — this service only issues them.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Issue a fresh access token for the given user.</summary>
    AccessToken CreateAccessToken(User user);
}
