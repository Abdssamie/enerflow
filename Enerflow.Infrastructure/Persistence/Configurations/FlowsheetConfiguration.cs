using Enerflow.Domain.Entities;
using Enerflow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enerflow.Infrastructure.Persistence.Configurations;

public class FlowsheetConfiguration : IEntityTypeConfiguration<Flowsheet>
{
    public void Configure(EntityTypeBuilder<Flowsheet> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Description)
            .HasMaxLength(1000);

        // Store PropertyPackage as JSONB
        builder.Property(f => f.DefaultPropertyPackage)
            .HasColumnType("jsonb");

        // Store Compounds list as JSONB
        builder.Property(f => f.Compounds)
            .HasColumnType("jsonb");

        // Aggregates
        builder.HasMany(f => f.UnitOperations)
            .WithOne()
            .HasForeignKey("FlowsheetId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.MaterialStreams)
            .WithOne()
            .HasForeignKey("FlowsheetId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.EnergyStreams)
            .WithOne()
            .HasForeignKey("FlowsheetId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
