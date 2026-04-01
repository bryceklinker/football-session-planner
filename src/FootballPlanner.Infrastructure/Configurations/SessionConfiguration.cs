using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.HasMany(s => s.Activities)
            .WithOne()
            .HasForeignKey(sa => sa.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
