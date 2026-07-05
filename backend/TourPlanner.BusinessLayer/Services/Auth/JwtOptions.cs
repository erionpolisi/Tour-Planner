namespace TourPlanner.BusinessLayer.Services.Auth;

/// <summary>
/// Configuration for the JWT bearer tokens issued by this API.
/// The signing key is a secret and MUST come from user-secrets in Development
/// or a proper secret store in production. It is validated at startup and
/// must be at least 32 bytes (256 bits) to be usable with HMAC-SHA256.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer — the identity of this server.</summary>
    public string Issuer { get; set; } = "TourPlanner.API";

    /// <summary>Intended audience — who the token is for.</summary>
    public string Audience { get; set; } = "TourPlannerWeb";

    /// <summary>
    /// Base64-encoded HMAC signing key.
    /// MUST be ≥ 32 bytes decoded (256 bits) for HS256.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access-token lifetime. Kept short — refresh tokens carry the session.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Refresh-token lifetime — the effective session duration.</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(7);
}
