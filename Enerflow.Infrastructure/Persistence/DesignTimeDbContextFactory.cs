using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Enerflow.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EnerflowDbContext>
{
    public EnerflowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EnerflowDbContext>();

        // Try to get connection string from environment variable first
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // Fallback for local development if env var not set
            // Requires ConnectionStrings__DefaultConnection environment variable or proper setup
            throw new InvalidOperationException("Connection string 'ConnectionStrings__DefaultConnection' not found in environment variables.");
        }

        optionsBuilder.UseNpgsql(connectionString);

        return new EnerflowDbContext(optionsBuilder.Options);
    }
}
