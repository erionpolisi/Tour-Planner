using Microsoft.EntityFrameworkCore;
using TourPlanner.Domain;

namespace TourPlanner.DataAccessLayer;

public class TourPlannerDbContext : DbContext
{
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