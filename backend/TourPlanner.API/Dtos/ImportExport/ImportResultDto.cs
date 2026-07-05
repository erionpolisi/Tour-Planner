namespace TourPlanner.API.Dtos.ImportExport;

/// <summary>
/// Summary of an import operation. <see cref="Errors"/> holds one entry per
/// tour that failed to import — the operation is best-effort: valid tours
/// are still saved even when some entries fail.
/// </summary>
public class ImportResultDto
{
    /// <summary>How many tours were persisted (each with its logs).</summary>
    public int Imported { get; set; }

    /// <summary>Total tours in the input file.</summary>
    public int Total { get; set; }

    /// <summary>Per-tour errors. Key = tour index in the input (0-based), value = reason.</summary>
    public List<ImportErrorDto> Errors { get; set; } = new();
}

public class ImportErrorDto
{
    /// <summary>Zero-based index of the tour in the input file's <c>tours</c> array.</summary>
    public int Index { get; set; }

    /// <summary>Best-effort display name for the failed tour (may be empty).</summary>
    public string TourName { get; set; } = string.Empty;

    /// <summary>Human-readable reason for the failure.</summary>
    public string Message { get; set; } = string.Empty;
}
