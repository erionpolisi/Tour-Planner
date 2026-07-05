using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer;

public class TourPlannerDbContext : DbContext
{
    /// <summary>
    /// Name of the shadow tsvector column used for PostgreSQL full-text search.
    /// Access via <c>EF.Property&lt;NpgsqlTsVector&gt;(entity, TourPlannerDbContext.SearchVectorColumn)</c>.
    /// </summary>
    public const string SearchVectorColumn = "SearchVector";

    public TourPlannerDbContext(DbContextOptions<TourPlannerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourLog> TourLogs => Set<TourLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store enums as strings in the database (more readable + robust against reordering)
        modelBuilder.Entity<Tour>()
            .Property(t => t.TransportType)
            .HasConversion<string>();

        modelBuilder.Entity<Tour>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<TourLog>()
            .Property(l => l.Difficulty)
            .HasConversion<string>();

        // --- Full-text search --------------------------------------------------
        // Shadow columns of type tsvector, computed by PostgreSQL from the source
        // columns and stored on disk. Indexed with GIN for fast @@ lookups.
        // The 'simple' text-search config keeps the indexing language-agnostic:
        // it lowercases and tokenizes but skips stemming / stopword removal,
        // which suits tour names, cities, and enum-as-text values best.
        //
        // The tour vector additionally includes the persisted "Popularity" and
        // "ChildFriendliness" columns as text so users can search for e.g.
        // "great for children" via the mapped label — see TourStatsCalculator
        // for the label buckets. Popularity is duplicated as its raw integer
        // and its bucket label to support both "5" and "very popular".

        modelBuilder.Entity<Tour>()
            .Property<NpgsqlTsVector>(SearchVectorColumn)
            .HasComputedColumnSql(
                """
                to_tsvector('simple',
                    coalesce("Name", '') || ' ' ||
                    coalesce("Description", '') || ' ' ||
                    coalesce("From", '') || ' ' ||
                    coalesce("To", '') || ' ' ||
                    coalesce("TransportType", '') || ' ' ||
                    coalesce("Status", '') || ' ' ||
                    "Popularity"::text || ' ' ||
                    "ChildFriendliness"::text || ' ' ||
                    (CASE
                        WHEN "Popularity" <= 0 THEN 'not tried'
                        WHEN "Popularity" <= 2 THEN 'some interest'
                        WHEN "Popularity" <= 5 THEN 'popular'
                        ELSE 'very popular'
                     END) || ' ' ||
                    (CASE
                        WHEN "ChildFriendliness" >= 67 THEN 'great for children'
                        WHEN "ChildFriendliness" >= 34 THEN 'ok for children'
                        ELSE 'not suitable for children'
                     END))
                """,
                stored: true);

        modelBuilder.Entity<Tour>()
            .HasIndex(SearchVectorColumn)
            .HasMethod("GIN");

        // Every tour belongs to exactly one user; deleting the user cascades to their tours.
        modelBuilder.Entity<Tour>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tour>()
            .HasIndex(t => t.UserId);

        modelBuilder.Entity<TourLog>()
            .Property<NpgsqlTsVector>(SearchVectorColumn)
            .HasComputedColumnSql(
                """
                to_tsvector('simple',
                    coalesce("Comment", '') || ' ' ||
                    coalesce("Difficulty", '') || ' ' ||
                    "Rating"::text)
                """,
                stored: true);

        modelBuilder.Entity<TourLog>()
            .HasIndex(SearchVectorColumn)
            .HasMethod("GIN");

        // A user's email must be unique
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Refresh tokens: unique hash + fast lookup by user
        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(t => t.TokenHash).IsUnique();
            b.HasIndex(t => t.UserId);
            b.Property(t => t.TokenHash).HasMaxLength(64); // sha256 hex = 64 chars
            b.Property(t => t.ReplacedByHash).HasMaxLength(64);
            b.Property(t => t.CreatedByIp).HasMaxLength(64);
        });
    }
}