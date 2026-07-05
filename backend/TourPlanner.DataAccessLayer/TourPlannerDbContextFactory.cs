using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TourPlanner.DataAccessLayer;

/// <summary>
/// Used by <c>dotnet ef</c> tooling at design time. Bypasses the full app
/// startup (Program.cs) so migrations don't require JWT keys, API keys, etc.
/// Reads the connection string from appsettings + user-secrets of the API project.
/// </summary>
public sealed class TourPlannerDbContextFactory : IDesignTimeDbContextFactory<TourPlannerDbContext>
{
    public TourPlannerDbContext CreateDbContext(string[] args)
    {
        // ../TourPlanner.API relative to the DAL project directory.
        var apiProjectDir = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..", "TourPlanner.API"));

        var config = new ConfigurationBuilder()
            .SetBasePath(apiProjectDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(userSecretsId: "e3c15777-4835-43a5-9aba-9acffb5f75aa", reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. " +
                "Set it via user-secrets in TourPlanner.API.");

        var opts = new DbContextOptionsBuilder<TourPlannerDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TourPlannerDbContext(opts);
    }
}
