using Enerflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enerflow.Infrastructure.Persistence.Configurations;

public class UnitOperationConfiguration : IEntityTypeConfiguration<UnitOperation>
{
    public void Configure(EntityTypeBuilder<UnitOperation> builder)
    {
        builder.HasKey(u => u.Id);
        
        builder.Property(u => u.Position)
            .HasColumnType("jsonb"); // Store Coordinates as JSONB

        builder.Property(u => u.Parameters)
            .HasColumnType("jsonb");

        builder.Property(u => u.InputConnections)
            .HasColumnType("jsonb");

        builder.Property(u => u.OutputConnections)
            .HasColumnType("jsonb");
    }
}
