using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly TourPlannerDbContext _db;
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(TourPlannerDbContext db, ILogger<RefreshTokenRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default) =>
        _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        _logger.LogInformation("Issuing refresh token for user {UserId}", token.UserId);
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var active = await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
            .ToListAsync(ct);

        if (active.Count == 0) return;

        _logger.LogWarning("Revoking {Count} active sessions for user {UserId}", active.Count, userId);
        foreach (var t in active) t.RevokedAtUtc = now;
        await _db.SaveChangesAsync(ct);
    }
}
