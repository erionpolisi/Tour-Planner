using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Auth;

/// <summary>
/// Refresh-token session service.
///
/// Token lifecycle:
///  * <see cref="IssueAsync"/> — mint 32 bytes of CSPRNG entropy, base64url-encode,
///    persist only the SHA-256 hash.
///  * <see cref="RotateAsync"/> — verify + revoke the presented token, issue a fresh one,
///    link them via <see cref="RefreshToken.ReplacedByHash"/>. Presenting an already-
///    revoked token that is not yet expired is treated as leakage: we revoke every
///    active session for that user.
///  * <see cref="RevokeAsync"/> — logout; silently no-ops if the token is unknown.
/// </summary>
public sealed class AuthSessionService : IAuthSessionService
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly JwtOptions _opt;
    private readonly ILogger<AuthSessionService> _logger;

    public AuthSessionService(
        IRefreshTokenRepository tokens,
        IUserRepository users,
        IOptions<JwtOptions> options,
        ILogger<AuthSessionService> logger)
    {
        _tokens = tokens;
        _users = users;
        _opt = options.Value;
        _logger = logger;
    }

    public async Task<(string RefreshToken, DateTime ExpiresAtUtc)> IssueAsync(
        Guid userId, string? clientIp = null, CancellationToken ct = default)
    {
        var (plain, hash) = GenerateToken();
        var record = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(_opt.RefreshTokenLifetime),
            CreatedByIp = clientIp,
        };
        await _tokens.AddAsync(record, ct);
        return (plain, record.ExpiresAtUtc);
    }

    public async Task<(User User, string RefreshToken, DateTime ExpiresAtUtc)> RotateAsync(
        string refreshToken, string? clientIp = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ValidationException("Refresh token is required.");

        var hash = Hash(refreshToken);
        var record = await _tokens.GetByHashAsync(hash, ct)
            ?? throw new ValidationException("Unknown refresh token.");

        // Reuse detection: if a previously-rotated token comes back, someone has a copy
        // they shouldn't. Kill every active session for this user.
        if (record.RevokedAtUtc is not null)
        {
            _logger.LogWarning(
                "Refresh-token reuse detected for user {UserId} — revoking all sessions",
                record.UserId);
            await _tokens.RevokeAllActiveForUserAsync(record.UserId, ct);
            throw new ValidationException("Refresh token has already been used.");
        }

        if (record.ExpiresAtUtc <= DateTime.UtcNow)
            throw new ValidationException("Refresh token has expired.");

        var user = await _users.GetByIdAsync(record.UserId)
            ?? throw new ValidationException("Owning user no longer exists.");

        // Rotate: revoke old, mint new, link them.
        var (plain, newHash) = GenerateToken();
        record.RevokedAtUtc = DateTime.UtcNow;
        record.ReplacedByHash = newHash;
        await _tokens.UpdateAsync(record, ct);

        var successor = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(_opt.RefreshTokenLifetime),
            CreatedByIp = clientIp,
        };
        await _tokens.AddAsync(successor, ct);

        return (user, plain, successor.ExpiresAtUtc);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var hash = Hash(refreshToken);
        var record = await _tokens.GetByHashAsync(hash, ct);
        if (record is null || record.RevokedAtUtc is not null) return;

        record.RevokedAtUtc = DateTime.UtcNow;
        await _tokens.UpdateAsync(record, ct);
        _logger.LogInformation("Refresh token revoked for user {UserId}", record.UserId);
    }

    // -----------------------------------------------------------------
    // helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// 256-bit CSPRNG token, base64url-encoded (43 URL-safe chars, no padding).
    /// </summary>
    private static (string Plain, string Hash) GenerateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        var plain = Base64UrlEncode(bytes);
        return (plain, Hash(plain));
    }

    private static string Hash(string value)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value), hash);
        return Convert.ToHexStringLower(hash);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data) =>
        Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
