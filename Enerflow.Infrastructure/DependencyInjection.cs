using Enerflow.Infrastructure.Persistence;
using Enerflow.Infrastructure.Persistence.Repositories;
using Enerflow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Enerflow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<FlowsheetDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(FlowsheetDbContext).Assembly.FullName)));

        services.AddScoped<IFlowsheetRepository, PgFlowsheetRepository>();

        return services;
    }
}
