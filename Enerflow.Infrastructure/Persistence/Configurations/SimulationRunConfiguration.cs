using Enerflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Enerflow.Infrastructure.Persistence.Configurations;

public class SimulationRunConfiguration : IEntityTypeConfiguration<SimulationRun>
{
    public void Configure(EntityTypeBuilder<SimulationRun> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.InputJson)
            .HasColumnType("jsonb");

        builder.Property(r => r.OutputJson)
            .HasColumnType("jsonb");
            
        builder.HasIndex(r => r.FlowsheetId);
    }
}
