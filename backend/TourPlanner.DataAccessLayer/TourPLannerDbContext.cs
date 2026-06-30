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
}