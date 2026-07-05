using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using TourPlanner.BusinessLayer.Services.Stats;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.ImportExport;

/// <summary>
/// Best-effort bulk import + export of tours (with their logs).
///
/// Import contract:
///   * Every tour and every log gets a fresh <see cref="Guid"/>, so callers
///     cannot forge IDs or accidentally overwrite existing rows.
///   * Each tour is validated in isolation. When one fails the others are
///     still saved and the failure is reported in <see cref="ImportSummary.Errors"/>.
///
/// Export contract:
///   * Returns every tour with its logs eager-loaded, ready to serialize.
/// </summary>
public sealed class TourImportExportService : ITourImportExportService
{
    private readonly ITourRepository _tours;
    private readonly ILogger<TourImportExportService> _logger;

    public TourImportExportService(ITourRepository tours, ILogger<TourImportExportService> logger)
    {
        _tours = tours;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Tour>> ExportAllAsync(Guid ownerId, CancellationToken ct = default)
    {
        var tours = await _tours.GetAllWithLogsAsync(ownerId, ct);
        _logger.LogInformation(
            "Export bundle prepared for user {UserId}: {TourCount} tour(s), {LogCount} log(s)",
            ownerId, tours.Count, tours.Sum(t => t.Logs.Count));
        return tours;
    }

    public async Task<ImportSummary> ImportAsync(Guid ownerId, IReadOnlyList<Tour> tours, CancellationToken ct = default)
    {
        if (tours is null || tours.Count == 0)
        {
            _logger.LogInformation("Import called with no tours");
            return new ImportSummary(0, 0, Array.Empty<ImportFailure>());
        }

        var failures = new List<ImportFailure>();
        var imported = 0;

        for (var i = 0; i < tours.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var incoming = tours[i];
            try
            {
                ValidateForImport(incoming);
                AssignFreshIdentity(incoming);
                // Force ownership so a caller cannot inject tours "belonging to" someone else.
                incoming.UserId = ownerId;
                // Precompute stats so the freshly-imported row has correct popularity / child-friendliness.
                incoming.Popularity = TourStatsCalculator.Popularity(incoming.Logs);
                incoming.ChildFriendliness = TourStatsCalculator.ChildFriendlinessScore(
                    incoming.Distance, incoming.Logs);

                await _tours.AddAsync(incoming);
                imported++;
                _logger.LogInformation(
                    "Imported tour #{Index} \"{Name}\" ({LogCount} log(s)) for user {UserId}",
                    i, incoming.Name, incoming.Logs.Count, ownerId);
            }
            catch (Exception ex) when (ex is ValidationException or ArgumentException)
            {
                _logger.LogWarning(ex,
                    "Skipped tour #{Index} \"{Name}\": {Message}",
                    i, incoming?.Name ?? string.Empty, ex.Message);
                failures.Add(new ImportFailure(i, incoming?.Name ?? string.Empty, ex.Message));
            }
        }

        _logger.LogInformation(
            "Import finished: {Imported}/{Total} succeeded, {Failed} failed",
            imported, tours.Count, failures.Count);

        return new ImportSummary(imported, tours.Count, failures);
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    /// <summary>
    /// Basic post-mapping validation. The API layer already runs DataAnnotations
    /// on the DTOs; this catches anything a caller might have bypassed
    /// (invalid enum values become <see cref="TransportType.Unknown"/> at the
    /// mapper level rather than throwing, so re-check here).
    /// </summary>
    private static void ValidateForImport(Tour t)
    {
        if (t is null) throw new ValidationException("Tour is null.");
        if (string.IsNullOrWhiteSpace(t.Name)) throw new ValidationException("Tour name is required.");
        if (string.IsNullOrWhiteSpace(t.From)) throw new ValidationException("Tour 'from' is required.");
        if (string.IsNullOrWhiteSpace(t.To)) throw new ValidationException("Tour 'to' is required.");
        if (t.TransportType == TransportType.Unknown)
            throw new ValidationException("Tour transport type is invalid or missing.");
        if (t.Distance < 0) throw new ValidationException("Tour distance must not be negative.");
        if (t.Duration < 0) throw new ValidationException("Tour duration must not be negative.");

        foreach (var log in t.Logs)
        {
            if (log.Difficulty == Difficulty.Unknown)
                throw new ValidationException("Log difficulty is invalid or missing.");
            if (log.Rating < 1 || log.Rating > 5)
                throw new ValidationException("Log rating must be between 1 and 5.");
            if (log.TotalDistance < 0)
                throw new ValidationException("Log total distance must not be negative.");
            if (log.Duration < TimeSpan.Zero)
                throw new ValidationException("Log duration must not be negative.");
        }
    }

    /// <summary>
    /// Overwrite any caller-supplied IDs with fresh <see cref="Guid.NewGuid"/> values,
    /// and re-link each log to the parent so EF Core's cascade insert wires up correctly.
    /// </summary>
    private static void AssignFreshIdentity(Tour t)
    {
        t.Id = Guid.NewGuid();
        foreach (var log in t.Logs)
        {
            log.Id = Guid.NewGuid();
            log.TourId = t.Id;
            log.Tour = null;   // let EF populate from the parent's Add(...)
        }
    }
}
