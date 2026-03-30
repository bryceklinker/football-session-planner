using FootballPlanner.Domain.Entities;
using FootballPlanner.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Focus> Focuses => Set<Focus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhaseConfiguration());
        modelBuilder.ApplyConfiguration(new FocusConfiguration());
    }
}
