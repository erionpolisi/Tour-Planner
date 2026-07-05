using TourPlanner.API.Dtos.ImportExport;
using TourPlanner.Domain;

namespace TourPlanner.API.Mappers;

/// <summary>
/// Translates between the wire-level import/export DTOs (km / minutes,
/// enum-as-string) and the domain entities (meters / seconds / TimeSpan,
/// enum values). Kept separate from <see cref="TourMapper"/> because the
/// import file format is a versioned, backwards-compatible contract while
/// the REST DTOs can change freely.
/// </summary>
public static class TourImportExportMapper
{
    // -----------------------------------------------------------------
    // EXPORT: domain → DTO
    // -----------------------------------------------------------------

    public static TourExportBundleDto ToBundle(IEnumerable<Tour> tours) => new()
    {
        Version = 1,
        ExportedAt = DateTime.UtcNow,
        Producer = "TourPlanner",
        Tours = tours.Select(ToExportItem).ToList(),
    };

    public static TourExportItemDto ToExportItem(Tour t) => new()
    {
        Name = t.Name,
        Description = t.Description,
        From = t.From,
        To = t.To,
        TransportType = t.TransportType.ToString().ToLowerInvariant(),
        Distance = Math.Round(t.Distance / 1000.0, 3),  // meters → km
        Duration = t.Duration / 60,                      // seconds → minutes
        Status = t.Status.ToString().ToLowerInvariant(),
        Color = t.Color,
        ImageUrl = t.ImageUrl,
        Logs = t.Logs.Select(ToExportItem).ToList(),
    };

    public static TourLogExportItemDto ToExportItem(TourLog log) => new()
    {
        LoggedAt = log.LoggedAt,
        Comment = log.Comment,
        Difficulty = log.Difficulty.ToString().ToLowerInvariant(),
        TotalDistance = Math.Round(log.TotalDistance / 1000.0, 3),  // m → km
        Duration = (int)log.Duration.TotalMinutes,                   // TimeSpan → minutes
        Rating = log.Rating,
    };

    // -----------------------------------------------------------------
    // IMPORT: DTO → domain
    // -----------------------------------------------------------------

    public static List<Tour> ToDomain(TourExportBundleDto bundle) =>
        bundle.Tours.Select(ToDomain).ToList();

    public static Tour ToDomain(TourExportItemDto dto)
    {
        var tour = new Tour
        {
            Id = Guid.Empty,                     // reassigned by the service
            Name = dto.Name ?? string.Empty,
            Description = dto.Description,
            From = dto.From ?? string.Empty,
            To = dto.To ?? string.Empty,
            TransportType = ParseTransportType(dto.TransportType),
            Distance = dto.Distance * 1000.0,    // km → meters
            Duration = dto.Duration * 60,        // minutes → seconds
            Status = ParseStatus(dto.Status),
            Color = dto.Color,
            ImageUrl = dto.ImageUrl,
        };
        tour.Logs = dto.Logs.Select(l => ToDomain(l, tour.Id)).ToList();
        return tour;
    }

    public static TourLog ToDomain(TourLogExportItemDto dto, Guid tourId) => new()
    {
        Id = Guid.Empty,                        // reassigned by the service
        TourId = tourId,
        LoggedAt = dto.LoggedAt.Kind == DateTimeKind.Utc
            ? dto.LoggedAt
            : dto.LoggedAt.ToUniversalTime(),
        Comment = dto.Comment,
        Difficulty = ParseDifficulty(dto.Difficulty),
        TotalDistance = dto.TotalDistance * 1000.0,
        Duration = TimeSpan.FromMinutes(dto.Duration),
        Rating = dto.Rating,
    };

    // -----------------------------------------------------------------
    // ImportResult mapping
    // -----------------------------------------------------------------

    public static ImportResultDto ToDto(BusinessLayer.Services.ImportExport.ImportSummary summary) => new()
    {
        Imported = summary.Imported,
        Total = summary.Total,
        Errors = summary.Errors
            .Select(e => new ImportErrorDto
            {
                Index = e.Index,
                TourName = e.TourName,
                Message = e.Message,
            })
            .ToList(),
    };

    // -----------------------------------------------------------------
    // Parsers — invalid values map to Unknown, so the service can flag them.
    // -----------------------------------------------------------------

    private static TransportType ParseTransportType(string? value) =>
        Enum.TryParse<TransportType>(value, ignoreCase: true, out var result)
            ? result
            : TransportType.Unknown;

    private static TourStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return TourStatus.Planned;
        return Enum.TryParse<TourStatus>(value, ignoreCase: true, out var result) && result != TourStatus.Unknown
            ? result
            : TourStatus.Planned;
    }

    private static Difficulty ParseDifficulty(string? value) =>
        Enum.TryParse<Difficulty>(value, ignoreCase: true, out var result)
            ? result
            : Difficulty.Unknown;
}
