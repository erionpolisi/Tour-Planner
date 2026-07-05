using System.ComponentModel.DataAnnotations;

namespace TourPlanner.API.Dtos.ImportExport;

/// <summary>
/// Wire format for import / export. Versioned so future breaking changes to the
/// shape can be detected and either upgraded or rejected. Kept intentionally
/// separate from <see cref="Tours.TourDto"/> / <see cref="Tours.CreateTourDto"/>
/// so REST DTOs and the export file format can evolve independently.
///
/// Distances are in kilometers and durations in minutes to match the display
/// units the rest of the API uses. The service converts to entity units (m / s / TimeSpan)
/// on the way in.
/// </summary>
public class TourExportBundleDto
{
    /// <summary>Bump this when the on-disk shape changes in a non-backwards-compatible way.</summary>
    public int Version { get; set; } = 1;

    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The application that produced the file (informational only).</summary>
    public string Producer { get; set; } = "TourPlanner";

    [Required]
    public List<TourExportItemDto> Tours { get; set; } = new();
}

/// <summary>One tour + all of its logs in export form.</summary>
public class TourExportItemDto
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string From { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string To { get; set; } = string.Empty;

    /// <summary>"walking" | "cycling" | "driving"</summary>
    [Required]
    public string TransportType { get; set; } = string.Empty;

    /// <summary>Kilometers.</summary>
    [Range(0, 100_000)]
    public double Distance { get; set; }

    /// <summary>Minutes.</summary>
    [Range(0, int.MaxValue)]
    public int Duration { get; set; }

    /// <summary>"planned" | "completed" (defaults to "planned" on import if missing).</summary>
    public string? Status { get; set; }

    public string? Color { get; set; }
    public string? ImageUrl { get; set; }

    public List<TourLogExportItemDto> Logs { get; set; } = new();
}

/// <summary>One tour log in export form. Same units as <see cref="TourLogs.TourLogDto"/>.</summary>
public class TourLogExportItemDto
{
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    [StringLength(2000)]
    public string? Comment { get; set; }

    /// <summary>"easy" | "medium" | "hard"</summary>
    [Required]
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>Kilometers.</summary>
    [Range(0, 100_000)]
    public double TotalDistance { get; set; }

    /// <summary>Minutes.</summary>
    [Range(0, int.MaxValue)]
    public int Duration { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }
}
