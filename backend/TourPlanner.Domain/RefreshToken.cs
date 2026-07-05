namespace TourPlanner.Domain;

/// <summary>
/// A refresh-token record — one row per issued session token.
/// The plain token is only handed to the client once; we store just the SHA-256
/// hash so a DB dump does not compromise live sessions.
///
/// Rotation on refresh: when a token is used, it's marked revoked and a fresh
/// one is issued; <see cref="ReplacedByHash"/> links to the successor so we
/// can detect and revoke an entire chain if an already-used token is
/// presented again (indicates theft).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    /// <summary>Owning user.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 of the plain token, hex-encoded lowercase.</summary>
    public required string TokenHash { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Non-null once revoked (logout, rotation, or reuse detection).</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>SHA-256 of the successor token — only set on rotation.</summary>
    public string? ReplacedByHash { get; set; }

    /// <summary>Client IP that issued the token (for audit — trimmed to /24 or v6 /64).</summary>
    public string? CreatedByIp { get; set; }
}
