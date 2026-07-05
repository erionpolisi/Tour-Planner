using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Auth;

/// <summary>
/// Manages refresh-token sessions — the long-lived side of authentication.
///
/// A "session" here is a chain of refresh tokens: on each successful refresh
/// the previous token is revoked and a fresh one is issued. Presenting an
/// already-revoked token triggers a chain-wide revocation for the user's
/// active sessions (assume the token was leaked).
/// </summary>
public interface IAuthSessionService
{
    /// <summary>Issue a brand-new refresh token for the given user.</summary>
    Task<(string RefreshToken, DateTime ExpiresAtUtc)> IssueAsync(
        Guid userId, string? clientIp = null, CancellationToken ct = default);

    /// <summary>
    /// Rotate a valid refresh token: revoke it, issue and return a successor.
    /// Throws <see cref="TourPlanner.BusinessLayer.Exceptions.ValidationException"/>
    /// on unknown / expired / revoked tokens.
    /// </summary>
    Task<(User User, string RefreshToken, DateTime ExpiresAtUtc)> RotateAsync(
        string refreshToken, string? clientIp = null, CancellationToken ct = default);

    /// <summary>Revoke a refresh token (logout). Silently no-ops on unknown tokens.</summary>
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
}
