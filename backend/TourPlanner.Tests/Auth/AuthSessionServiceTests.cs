using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services.Auth;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Auth;

[TestFixture]
public class AuthSessionServiceTests
{
    private Mock<IRefreshTokenRepository> _tokens = null!;
    private Mock<IUserRepository> _users = null!;
    private AuthSessionService _service = null!;

    private static readonly JwtOptions Opt = new()
    {
        Issuer = "test",
        Audience = "test",
        SigningKey = new string('a', 44), // unused here
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
        RefreshTokenLifetime = TimeSpan.FromDays(7),
    };

    [SetUp]
    public void SetUp()
    {
        _tokens = new Mock<IRefreshTokenRepository>(MockBehavior.Strict);
        _users = new Mock<IUserRepository>(MockBehavior.Strict);
        _service = new AuthSessionService(
            _tokens.Object,
            _users.Object,
            Options.Create(Opt),
            NullLogger<AuthSessionService>.Instance);
    }

    private static User SampleUser(Guid id) => new()
    {
        Id = id,
        Name = "Ada",
        Email = "ada@example.com",
        PasswordHash = "x",
        CreatedAt = DateTime.UtcNow,
    };

    // -------------------------------------------------------------------
    //  IssueAsync
    // -------------------------------------------------------------------

    [Test]
    public async Task IssueAsync_PersistsHashedToken_AndReturnsPlainToken()
    {
        var userId = Guid.NewGuid();
        RefreshToken? captured = null;
        _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        var (plain, exp) = await _service.IssueAsync(userId, clientIp: "127.0.0.1");

        Assert.That(plain, Is.Not.Null.And.Not.Empty);
        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.UserId, Is.EqualTo(userId));
        Assert.That(captured.CreatedByIp, Is.EqualTo("127.0.0.1"));
        Assert.That(captured.TokenHash, Has.Length.EqualTo(64)); // sha256 hex
        Assert.That(captured.TokenHash, Is.Not.EqualTo(plain));
        Assert.That(captured.RevokedAtUtc, Is.Null);
        Assert.That(exp, Is.EqualTo(DateTime.UtcNow.Add(Opt.RefreshTokenLifetime))
            .Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public async Task IssueAsync_ProducesDifferentTokensEachCall()
    {
        _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var (a, _) = await _service.IssueAsync(Guid.NewGuid());
        var (b, _) = await _service.IssueAsync(Guid.NewGuid());

        Assert.That(a, Is.Not.EqualTo(b));
    }

    // -------------------------------------------------------------------
    //  RotateAsync
    // -------------------------------------------------------------------

    [Test]
    public void RotateAsync_MissingToken_ThrowsValidation()
    {
        Assert.That(async () => await _service.RotateAsync(""),
            Throws.TypeOf<ValidationException>().With.Message.Contains("required"));
    }

    [Test]
    public void RotateAsync_UnknownToken_ThrowsValidation()
    {
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        Assert.That(async () => await _service.RotateAsync("does-not-exist"),
            Throws.TypeOf<ValidationException>().With.Message.Contains("Unknown"));
    }

    [Test]
    public void RotateAsync_ExpiredToken_ThrowsValidation()
    {
        var record = MakeRecord(
            revoked: null,
            expires: DateTime.UtcNow.AddMinutes(-1));
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        Assert.That(async () => await _service.RotateAsync("expired"),
            Throws.TypeOf<ValidationException>().With.Message.Contains("expired"));
    }

    [Test]
    public void RotateAsync_RevokedToken_TriggersChainRevocation_AndThrows()
    {
        var userId = Guid.NewGuid();
        var record = MakeRecord(
            userId: userId,
            revoked: DateTime.UtcNow.AddMinutes(-5),
            expires: DateTime.UtcNow.AddDays(1));
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _tokens.Setup(t => t.RevokeAllActiveForUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        Assert.That(async () => await _service.RotateAsync("reused"),
            Throws.TypeOf<ValidationException>().With.Message.Contains("already been used"));

        _tokens.Verify(t => t.RevokeAllActiveForUserAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RotateAsync_ValidToken_RevokesOldIssuesNew_AndLinksThem()
    {
        var userId = Guid.NewGuid();
        var record = MakeRecord(
            userId: userId,
            revoked: null,
            expires: DateTime.UtcNow.AddDays(1));
        RefreshToken? successor = null;

        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _tokens.Setup(t => t.UpdateAsync(record, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _tokens.Setup(t => t.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((t, _) => successor = t)
            .Returns(Task.CompletedTask);
        _users.Setup(u => u.GetByIdAsync(userId))
            .ReturnsAsync(SampleUser(userId));

        var (user, plain, _) = await _service.RotateAsync("still-valid");

        Assert.That(user.Id, Is.EqualTo(userId));
        Assert.That(plain, Is.Not.Null.And.Not.Empty);
        Assert.That(record.RevokedAtUtc, Is.Not.Null);
        Assert.That(record.ReplacedByHash, Is.Not.Null.And.Not.Empty);
        Assert.That(successor, Is.Not.Null);
        Assert.That(successor!.TokenHash, Is.EqualTo(record.ReplacedByHash));
        Assert.That(successor.UserId, Is.EqualTo(userId));
        Assert.That(successor.RevokedAtUtc, Is.Null);
    }

    [Test]
    public void RotateAsync_ValidToken_ButUserVanished_ThrowsValidation()
    {
        var userId = Guid.NewGuid();
        var record = MakeRecord(userId: userId, revoked: null, expires: DateTime.UtcNow.AddDays(1));
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _users.Setup(u => u.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        Assert.That(async () => await _service.RotateAsync("orphaned"),
            Throws.TypeOf<ValidationException>().With.Message.Contains("no longer exists"));
    }

    // -------------------------------------------------------------------
    //  RevokeAsync
    // -------------------------------------------------------------------

    [Test]
    public async Task RevokeAsync_UnknownToken_NoOps()
    {
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await _service.RevokeAsync("nope");

        _tokens.Verify(t => t.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RevokeAsync_AlreadyRevoked_NoOps()
    {
        var record = MakeRecord(
            revoked: DateTime.UtcNow.AddHours(-1),
            expires: DateTime.UtcNow.AddDays(1));
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        await _service.RevokeAsync("already-dead");

        _tokens.Verify(t => t.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RevokeAsync_ActiveToken_MarksRevoked()
    {
        var record = MakeRecord(revoked: null, expires: DateTime.UtcNow.AddDays(1));
        _tokens.Setup(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);
        _tokens.Setup(t => t.UpdateAsync(record, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await _service.RevokeAsync("logout-me");

        Assert.That(record.RevokedAtUtc, Is.Not.Null);
        _tokens.Verify(t => t.UpdateAsync(record, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RevokeAsync_EmptyToken_NoOps()
    {
        await _service.RevokeAsync("");
        _tokens.Verify(t => t.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------
    //  helpers
    // -------------------------------------------------------------------

    private static RefreshToken MakeRecord(
        Guid? userId = null,
        DateTime? revoked = null,
        DateTime? expires = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        TokenHash = new string('a', 64),
        CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
        ExpiresAtUtc = expires ?? DateTime.UtcNow.AddDays(1),
        RevokedAtUtc = revoked,
    };
}
