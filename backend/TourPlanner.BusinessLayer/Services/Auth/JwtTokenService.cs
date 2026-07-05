using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.Auth;

/// <summary>
/// Issues short-lived (15-minute) HMAC-SHA256-signed JWT access tokens.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    private readonly SigningCredentials _signingCredentials;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _opt = options.Value;

        var keyBytes = DecodeKey(_opt.SigningKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must decode to at least 32 bytes (256 bits) for HMAC-SHA256. " +
                "Generate one with: [Convert]::ToBase64String((1..48 | %{Get-Random -Max 256})) " +
                "and set it via `dotnet user-secrets set \"Jwt:SigningKey\" \"<key>\"`.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(_opt.AccessTokenLifetime);
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _signingCredentials);

        return new AccessToken(_handler.WriteToken(token), jti, expires);
    }

    /// <summary>
    /// Accept either a base64 string or a plain UTF-8 string as the key.
    /// Base64 is preferred (compact, round-trip-safe); the plain-string fallback
    /// makes dev setup easier.
    /// </summary>
    private static byte[] DecodeKey(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is not configured. Set it via user-secrets: " +
                "`dotnet user-secrets set \"Jwt:SigningKey\" \"<base64-key>\"`.");
        }

        try { return Convert.FromBase64String(raw); }
        catch (FormatException) { return Encoding.UTF8.GetBytes(raw); }
    }
}
