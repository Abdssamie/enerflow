using Enerflow.Domain.Common;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Enerflow.Infrastructure.Persistence;

/// <summary>
/// A value generator for EF Core that uses the sequential IdGenerator (NewId) 
/// to generate identifiers for Guid properties.
/// </summary>
public class SequentialGuidValueGenerator : ValueGenerator<Guid>
{
    public override bool GeneratesTemporaryValues => false;

    public override Guid Next(EntityEntry entry) => IdGenerator.NextGuid();
}
