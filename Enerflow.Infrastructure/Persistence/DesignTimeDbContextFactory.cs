using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Enerflow.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EnerflowDbContext>
{
    public EnerflowDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Get connection string from configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Fallback for when running via dotnet ef from a different directory (optional safety net)
        if (string.IsNullOrEmpty(connectionString))
        {
             connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                ?? Environment.GetEnvironmentVariable("DefaultConnection");
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find connection string 'DefaultConnection'. Please set it in appsettings.json, appsettings.Development.json, or environment variables.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<EnerflowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new EnerflowDbContext(optionsBuilder.Options);
    }
}
