using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class SessionActivityKeyPointConfiguration : IEntityTypeConfiguration<SessionActivityKeyPoint>
{
    public void Configure(EntityTypeBuilder<SessionActivityKeyPoint> builder)
    {
        builder.ToTable("SessionActivityKeyPoints");
        builder.HasKey(kp => kp.Id);
        builder.Property(kp => kp.SessionActivityId).IsRequired();
        builder.Property(kp => kp.Order).IsRequired();
        builder.Property(kp => kp.Text).IsRequired().HasMaxLength(500);
    }
}
