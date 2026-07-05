using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TourPlanner.DataAccessLayer.Interceptors;

/// <summary>
/// EF Core interceptor that logs every insert, update and delete just before it
/// is flushed to the database. Runs once per SaveChangesAsync, so a single
/// service call that touches multiple entities produces multiple log entries.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ILogger<AuditSaveChangesInterceptor> _logger;

    public AuditSaveChangesInterceptor(ILogger<AuditSaveChangesInterceptor> logger)
    {
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        LogPendingChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        LogPendingChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void LogPendingChanges(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    _logger.LogInformation(
                        "DB INSERT {Entity} {Id}",
                        entry.Metadata.ClrType.Name,
                        GetPrimaryKey(entry));
                    break;

                case EntityState.Modified:
                    _logger.LogInformation(
                        "DB UPDATE {Entity} {Id} changed=[{Changes}]",
                        entry.Metadata.ClrType.Name,
                        GetPrimaryKey(entry),
                        DescribeChanges(entry));
                    break;

                case EntityState.Deleted:
                    _logger.LogInformation(
                        "DB DELETE {Entity} {Id}",
                        entry.Metadata.ClrType.Name,
                        GetPrimaryKey(entry));
                    break;
            }
        }
    }

    private static object? GetPrimaryKey(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk is null) return null;

        var values = pk.Properties
            .Select(p => entry.Property(p.Name).CurrentValue)
            .ToArray();

        return values.Length == 1 ? values[0] : string.Join(",", values);
    }

    private static string DescribeChanges(EntityEntry entry)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var prop in entry.Properties)
        {
            if (!prop.IsModified) continue;
            // Never log password hashes or anything else marked sensitive by name.
            if (LooksSensitive(prop.Metadata.Name)) continue;

            if (!first) sb.Append(", ");
            sb.Append(prop.Metadata.Name);
            first = false;
        }
        return sb.ToString();
    }

    private static bool LooksSensitive(string propertyName) =>
        propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase);
}
