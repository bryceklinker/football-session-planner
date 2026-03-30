using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class FocusConfiguration : IEntityTypeConfiguration<Focus>
{
    public void Configure(EntityTypeBuilder<Focus> builder)
    {
        builder.ToTable("Focuses");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
    }
}
