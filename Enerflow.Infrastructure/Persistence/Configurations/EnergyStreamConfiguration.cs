using Enerflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enerflow.Infrastructure.Persistence.Configurations;

public class EnergyStreamConfiguration : IEntityTypeConfiguration<EnergyStream>
{
    public void Configure(EntityTypeBuilder<EnergyStream> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Position)
            .HasColumnType("jsonb");
    }
}
