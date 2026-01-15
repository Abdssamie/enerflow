using Enerflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enerflow.Infrastructure.Persistence.Configurations;

public class MaterialStreamConfiguration : IEntityTypeConfiguration<MaterialStream>
{
    public void Configure(EntityTypeBuilder<MaterialStream> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Position)
            .HasColumnType("jsonb");

        builder.Property(s => s.Composition)
            .HasColumnType("jsonb");
    }
}
