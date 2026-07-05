using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services.Stats;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services;

public class TourService : ITourService
{
    private readonly ITourRepository _tours;
    private readonly ILogger<TourService> _logger;

    public TourService(ITourRepository tours, ILogger<TourService> logger)
    {
        _tours = tours;
        _logger = logger;
    }

    public Task<List<Tour>> GetAllAsync(Guid ownerId) => _tours.GetAllAsync(ownerId);

    public async Task<Tour> GetByIdAsync(Guid ownerId, Guid id) =>
        await _tours.GetByIdAsync(ownerId, id)
            ?? throw new NotFoundException($"Tour {id} not found.");

    public async Task<Tour> CreateAsync(Guid ownerId, Tour tour)
    {
        // Force ownership from the authenticated principal — never trust the request body.
        tour.UserId = ownerId;
        // Initialise the computed stats using the calculator's neutral defaults
        // (no logs → 0 popularity, distance-driven baseline child-friendliness).
        // TourLogService keeps them in sync as logs are added.
        tour.Popularity = TourStatsCalculator.Popularity(tour.Logs);
        tour.ChildFriendliness = TourStatsCalculator.ChildFriendlinessScore(tour.Distance, tour.Logs);
        await _tours.AddAsync(tour);
        _logger.LogInformation("Created tour {TourId} ({Name}) for user {UserId}", tour.Id, tour.Name, ownerId);
        return tour;
    }

    public async Task<Tour> UpdateAsync(Guid ownerId, Guid id, Action<Tour> applyChanges)
    {
        var entity = await _tours.GetByIdAsync(ownerId, id)
            ?? throw new NotFoundException($"Tour {id} not found.");

        applyChanges(entity);
        // Belt-and-braces: even if applyChanges mutated UserId (it shouldn't),
        // the DB row still belongs to the original owner.
        entity.UserId = ownerId;

        await _tours.UpdateAsync(entity);
        _logger.LogInformation("Updated tour {TourId} for user {UserId}", id, ownerId);
        return entity;
    }

    public async Task DeleteAsync(Guid ownerId, Guid id)
    {
        var deleted = await _tours.DeleteAsync(ownerId, id);
        if (!deleted) throw new NotFoundException($"Tour {id} not found.");
        _logger.LogInformation("Deleted tour {TourId} for user {UserId}", id, ownerId);
    }

    public async Task<List<TourSearchResult>> SearchAsync(
        Guid ownerId, string query, int limit, CancellationToken ct = default)
    {
        // Trim + short-circuit empty queries so the DB doesn't see a pointless
        // roundtrip. This is also what the API layer relies on when validating.
        var trimmed = query?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            _logger.LogInformation("Search called with empty query — returning empty list");
            return new List<TourSearchResult>();
        }

        var hits = await _tours.SearchAsync(ownerId, trimmed, limit, ct);
        _logger.LogInformation(
            "Search returned {Count} hit(s) for user {UserId} (query length {QueryLength})",
            hits.Count, ownerId, trimmed.Length);

        return hits
            .Select(h => new TourSearchResult(h.Tour, h.MatchedInTour, h.MatchedLogs))
            .ToList();
    }
}
