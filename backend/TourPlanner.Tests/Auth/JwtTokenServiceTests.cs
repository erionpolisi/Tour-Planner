using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Services.Auth;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Auth;

[TestFixture]
public class JwtTokenServiceTests
{
    private const string TestKeyBase64 =
        // 48 bytes of arbitrary but stable content — used only for these tests.
        "MDEyMzQ1Njc4OWFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6QUJDREVGR0hJSktM";

    private static JwtOptions DefaultOptions() => new()
    {
        Issuer = "TourPlanner.Test",
        Audience = "TourPlannerWeb.Test",
        SigningKey = TestKeyBase64,
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(7),
    };

    private static JwtTokenService CreateService(JwtOptions? opt = null) =>
        new(Options.Create(opt ?? DefaultOptions()));

    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Ada Lovelace",
        Email = "ada@example.com",
        PasswordHash = "not-a-real-hash",
        CreatedAt = DateTime.UtcNow,
    };

    [Test]
    public void Ctor_MissingKey_Throws()
    {
        var opt = DefaultOptions();
        opt.SigningKey = "";
        Assert.That(() => CreateService(opt),
            Throws.TypeOf<InvalidOperationException>()
                  .With.Message.Contains("Jwt:SigningKey"));
    }

    [Test]
    public void Ctor_ShortKey_Throws()
    {
        var opt = DefaultOptions();
        opt.SigningKey = "shortkey"; // 8 UTF-8 bytes
        Assert.That(() => CreateService(opt),
            Throws.TypeOf<InvalidOperationException>()
                  .With.Message.Contains("32 bytes"));
    }

    [Test]
    public void CreateAccessToken_ReturnsSignedJwt_WithExpectedClaims()
    {
        var svc = CreateService();
        var user = SampleUser();

        var token = svc.CreateAccessToken(user);

        Assert.That(token.Value, Is.Not.Null.And.Not.Empty);
        Assert.That(token.TokenId, Has.Length.EqualTo(32)); // Guid.ToString("N")
        Assert.That(token.ExpiresAtUtc,
            Is.EqualTo(DateTime.UtcNow.AddMinutes(15)).Within(TimeSpan.FromSeconds(5)));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);
        Assert.That(jwt.Header.Alg, Is.EqualTo("HS256"));
        Assert.That(jwt.Issuer, Is.EqualTo("TourPlanner.Test"));
        Assert.That(jwt.Audiences.Single(), Is.EqualTo("TourPlannerWeb.Test"));
        Assert.That(jwt.Subject, Is.EqualTo(user.Id.ToString()));
        Assert.That(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value,
            Is.EqualTo("ada@example.com"));
        Assert.That(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Name).Value,
            Is.EqualTo("Ada Lovelace"));
        Assert.That(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Jti), Is.True);
        Assert.That(jwt.Claims.Any(c => c.Type == JwtRegisteredClaimNames.Iat), Is.True);
    }

    [Test]
    public void CreateAccessToken_SignatureValidatesWithSameKey()
    {
        var svc = CreateService();
        var token = svc.CreateAccessToken(SampleUser());

        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TourPlanner.Test",
            ValidateAudience = true,
            ValidAudience = "TourPlannerWeb.Test",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(TestKeyBase64)),
            ClockSkew = TimeSpan.Zero,
        };

        Assert.That(() => handler.ValidateToken(token.Value, parameters, out _),
            Throws.Nothing);
    }

    [Test]
    public void CreateAccessToken_TwoCalls_ProduceDifferentJtis()
    {
        var svc = CreateService();
        var user = SampleUser();

        var a = svc.CreateAccessToken(user);
        var b = svc.CreateAccessToken(user);

        Assert.That(a.TokenId, Is.Not.EqualTo(b.TokenId));
        Assert.That(a.Value, Is.Not.EqualTo(b.Value));
    }

    [Test]
    public void CreateAccessToken_HonorsCustomLifetime()
    {
        var opt = DefaultOptions();
        opt.AccessTokenLifetime = TimeSpan.FromMinutes(5);
        var svc = CreateService(opt);

        var token = svc.CreateAccessToken(SampleUser());

        Assert.That(token.ExpiresAtUtc,
            Is.EqualTo(DateTime.UtcNow.AddMinutes(5)).Within(TimeSpan.FromSeconds(5)));
    }
}
