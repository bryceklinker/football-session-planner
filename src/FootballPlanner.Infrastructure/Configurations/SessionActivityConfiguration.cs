using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class SessionActivityConfiguration : IEntityTypeConfiguration<SessionActivity>
{
    public void Configure(EntityTypeBuilder<SessionActivity> builder)
    {
        builder.ToTable("SessionActivities");
        builder.HasKey(sa => sa.Id);
        builder.Property(sa => sa.SessionId).IsRequired();
        builder.Property(sa => sa.ActivityId).IsRequired();
        builder.Property(sa => sa.PhaseId).IsRequired();
        builder.Property(sa => sa.FocusId).IsRequired();
        builder.Property(sa => sa.Duration).IsRequired();
        builder.Property(sa => sa.DisplayOrder).IsRequired();
        builder.Property(sa => sa.Notes).HasMaxLength(2000);
        builder.HasOne(sa => sa.Activity)
            .WithMany()
            .HasForeignKey(sa => sa.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(sa => sa.Phase)
            .WithMany()
            .HasForeignKey(sa => sa.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(sa => sa.Focus)
            .WithMany()
            .HasForeignKey(sa => sa.FocusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(sa => sa.KeyPoints)
            .WithOne()
            .HasForeignKey(kp => kp.SessionActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
