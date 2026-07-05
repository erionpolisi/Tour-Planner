using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>Contract for storing and retrieving refresh-token session records.</summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Revoke every still-active token for a user (chain-wide kill switch).</summary>
    Task RevokeAllActiveForUserAsync(Guid userId, CancellationToken ct = default);
}
