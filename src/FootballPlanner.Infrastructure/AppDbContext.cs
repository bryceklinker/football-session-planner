using FootballPlanner.Domain.Entities;
using FootballPlanner.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Focus> Focuses => Set<Focus>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionActivity> SessionActivities => Set<SessionActivity>();
    public DbSet<SessionActivityKeyPoint> SessionActivityKeyPoints => Set<SessionActivityKeyPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhaseConfiguration());
        modelBuilder.ApplyConfiguration(new FocusConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
    }
}
