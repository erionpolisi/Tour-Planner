using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITourRepository"/>.
/// All public methods log at Information level so we can trace DB access.
/// </summary>
public class TourRepository : ITourRepository
{
    private const int DefaultSearchLimit = 50;
    private const int MaxSearchLimit = 200;

    private readonly TourPlannerDbContext _db;
    private readonly ILogger<TourRepository> _logger;

    public TourRepository(TourPlannerDbContext db, ILogger<TourRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Tour>> GetAllAsync(Guid ownerId)
    {
        _logger.LogInformation("Loading all tours for user {UserId}", ownerId);
        // AsNoTracking → EF doesn't track these entities for change-detection.
        // Faster + uses less memory when we only want to READ data.
        return await _db.Tours
            .AsNoTracking()
            .Where(t => t.UserId == ownerId)
            .ToListAsync();
    }

    public async Task<Tour?> GetByIdAsync(Guid ownerId, Guid id)
    {
        _logger.LogInformation("Loading tour {TourId} for user {UserId}", id, ownerId);
        return await _db.Tours
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == ownerId);
    }

    public async Task AddAsync(Tour tour)
    {
        _logger.LogInformation("Adding new tour {TourName} for user {UserId}", tour.Name, tour.UserId);
        _db.Tours.Add(tour);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Tour tour)
    {
        _logger.LogInformation("Updating tour {TourId}", tour.Id);
        _db.Tours.Update(tour);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid id)
    {
        // FirstOrDefaultAsync (not FindAsync) so the WHERE ownerId filter is applied.
        var tour = await _db.Tours.FirstOrDefaultAsync(t => t.Id == id && t.UserId == ownerId);
        if (tour is null)
        {
            _logger.LogWarning(
                "Tried to delete tour {TourId} for user {UserId}, but it was not found or not owned",
                id, ownerId);
            return false;
        }

        _logger.LogInformation("Deleting tour {TourId}", id);
        _db.Tours.Remove(tour);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<Tour>> GetAllWithLogsAsync(Guid ownerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading all tours with logs for user {UserId} (export)", ownerId);
        // AsNoTracking is safe here: caller only serializes the result.
        return await _db.Tours
            .AsNoTracking()
            .Where(t => t.UserId == ownerId)
            .Include(t => t.Logs.OrderBy(l => l.LoggedAt))
            .ToListAsync(ct);
    }

    public async Task<List<TourSearchHit>> SearchAsync(Guid ownerId, string query, int limit, CancellationToken ct = default)
    {
        // Callers (TourService) guarantee non-empty; be defensive anyway.
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<TourSearchHit>();
        }

        var effectiveLimit = limit switch
        {
            <= 0 => DefaultSearchLimit,
            > MaxSearchLimit => MaxSearchLimit,
            _ => limit,
        };

        _logger.LogInformation(
            "Full-text search for tours: user={UserId} len={QueryLength} limit={Limit}",
            ownerId, query.Length, effectiveLimit);

        // Web-search style: supports "quoted phrases", -negation, and word OR word.
        // NOTE: `EF.Functions.WebSearchToTsQuery` and `EF.Property<...>` MUST appear
        // inline inside the expression tree. Capturing them in a local would force
        // client-side evaluation and blow up at runtime with
        // "the query has switched to client-evaluation".
        // The `query` string is bound as a parameter, so this is injection-safe.

        var results = await _db.Tours
            .AsNoTracking()
            .Where(t => t.UserId == ownerId
                && (EF.Property<NpgsqlTsVector>(t, TourPlannerDbContext.SearchVectorColumn)
                        .Matches(EF.Functions.WebSearchToTsQuery("simple", query))
                    || t.Logs.Any(l =>
                        EF.Property<NpgsqlTsVector>(l, TourPlannerDbContext.SearchVectorColumn)
                            .Matches(EF.Functions.WebSearchToTsQuery("simple", query)))))
            .Select(t => new
            {
                Tour = t,
                MatchedInTour = EF.Property<NpgsqlTsVector>(t, TourPlannerDbContext.SearchVectorColumn)
                    .Matches(EF.Functions.WebSearchToTsQuery("simple", query)),
                MatchedLogs = t.Logs
                    .Where(l =>
                        EF.Property<NpgsqlTsVector>(l, TourPlannerDbContext.SearchVectorColumn)
                            .Matches(EF.Functions.WebSearchToTsQuery("simple", query)))
                    .OrderByDescending(l => l.LoggedAt)
                    .ToList(),
            })
            .Take(effectiveLimit)
            .ToListAsync(ct);

        return results
            .Select(r => new TourSearchHit(r.Tour, r.MatchedInTour, r.MatchedLogs))
            .ToList();
    }
}