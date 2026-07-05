using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Services.ImportExport;

/// <summary>
/// Business-layer contract for bulk tour import and export.
///
/// The interface deliberately works with domain entities. DTO shape lives in
/// the API layer so the export file format can evolve independently.
/// </summary>
public interface ITourImportExportService
{
    /// <summary>
    /// Load every tour together with its logs, ready to be serialized.
    /// </summary>
    Task<IReadOnlyList<Tour>> ExportAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Import a set of tours. Best-effort: valid tours are saved even when
    /// others in the same call fail. Each imported tour is assigned a fresh
    /// <see cref="Tour.Id"/> so a re-import never collides.
    /// </summary>
    /// <returns>
    /// Summary of the operation. <see cref="ImportSummary.Errors"/> contains
    /// the per-tour failures (index + display name + human message).
    /// </returns>
    Task<ImportSummary> ImportAsync(IReadOnlyList<Tour> tours, CancellationToken ct = default);
}

/// <summary>Aggregate result of an <see cref="ITourImportExportService.ImportAsync"/> call.</summary>
public sealed record ImportSummary(int Imported, int Total, IReadOnlyList<ImportFailure> Errors);

/// <summary>Details of one tour that could not be imported.</summary>
public sealed record ImportFailure(int Index, string TourName, string Message);
