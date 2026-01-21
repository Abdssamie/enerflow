using Enerflow.API.Extensions;
using Enerflow.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Enerflow.Worker.Consumers;
using Enerflow.Simulation.Services;
using Enerflow.Simulation.Flowsheet.Compounds;
using Enerflow.Simulation.Flowsheet.PropertyPackages;
using Enerflow.Simulation.Flowsheet.Streams;
using Enerflow.Simulation.Flowsheet.FlashAlgorithms;
using Enerflow.Simulation.Flowsheet.UnitOperations;
using Enerflow.Domain.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Npgsql;

namespace Enerflow.Tests.Functional;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-bookworm")
        .Build();

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Ensure database is created and migrated
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnerflowDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => 
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.UseSetting("RedisConfiguration", "localhost:6379,abortConnect=false");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("RateLimit:MaxRequests", "1000");
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EnerflowDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add DbContext pointing to the container with Dynamic JSON enabled
            services.AddDbContext<EnerflowDbContext>(options =>
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString());
                dataSourceBuilder.EnableDynamicJson();
                var dataSource = dataSourceBuilder.Build();
                options.UseNpgsql(dataSource);
            });

            // Re-configure MassTransit to use the container and include Worker consumers
            // First, remove existing MassTransit services
            var massTransitDescriptors = services.Where(d => 
                d.ServiceType.Namespace != null && 
                (d.ServiceType.Namespace.StartsWith("MassTransit") || 
                 d.ServiceType.Name.Contains("MassTransit"))).ToList();
            
            foreach (var d in massTransitDescriptors)
            {
                services.Remove(d);
            }

            // Configure PostgreSQL Transport for MassTransit
            services.ConfigurePostgresTransport(_dbContainer.GetConnectionString());

            // Add MassTransit with both API (Producer) and Worker (Consumer) configuration
            services.AddMassTransit(x =>
            {
                // Register Worker consumer
                x.AddConsumer<SimulationJobConsumer, SimulationJobConsumerDefinition>();

                x.SetKebabCaseEndpointNameFormatter();

                x.UsingPostgres((context, cfg) =>
                {
                    cfg.AutoStart = true;

                    cfg.ConfigureJsonSerializerOptions(options =>
                    {
                        options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                        return options;
                    });

                    cfg.ConfigureEndpoints(context);
                });
            });

            // Register Worker-specific services needed by the SimulationJobConsumer
            services.TryAddSingleton<ICompoundManager, CompoundManager>();
            services.TryAddSingleton<IPropertyPackageManager, PropertyPackageManager>();
            services.TryAddSingleton<IMaterialStreamFactory, MaterialStreamFactory>();
            services.TryAddSingleton<IEnergyStreamFactory, EnergyStreamFactory>();
            services.TryAddSingleton<IUnitOperationFactory, UnitOperationFactory>();
            services.TryAddSingleton<IFlashAlgorithmManager, FlashAlgorithmManager>();
            services.TryAddScoped<ISimulationService, SimulationService>();
        });
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}
