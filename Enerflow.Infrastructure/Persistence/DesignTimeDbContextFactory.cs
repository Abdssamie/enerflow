using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace Enerflow.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EnerflowDbContext>
{
    public EnerflowDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EnerflowDbContext>();

        // Default development connection string matching .env.example
        var connectionString = "Host=localhost;Port=5433;Database=enerflow_db;Username=enerflow;Password=enerflow_password;";

        optionsBuilder.UseNpgsql(connectionString);

        return new EnerflowDbContext(optionsBuilder.Options);
    }
}
