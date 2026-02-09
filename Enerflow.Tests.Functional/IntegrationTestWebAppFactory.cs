using Enerflow.API.Extensions;
using Enerflow.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Enerflow.Worker.Consumers;
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
using Testcontainers.Redis;
using Npgsql;
using Enerflow.Worker.Solvers;
using Enerflow.Worker.Mappers;
using Enerflow.Worker.Builders;
using Enerflow.Worker.Validation;

namespace Enerflow.Tests.Functional;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18-bookworm")
        .Build();
    
    private readonly RedisContainer _redisContainer = new RedisBuilder("redis:alpine")
        .Build();

    public async Task InitializeAsync()
    {
        // Start both containers in parallel
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _redisContainer.StartAsync());
        
        // Now create the database schema using raw SQL
        // This bypasses EnsureCreatedAsync which doesn't work when other tables exist
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnerflowDbContext>();
        
        // Generate the creation script from the model and execute it
        var script = dbContext.Database.GenerateCreateScript();
        await dbContext.Database.ExecuteSqlRawAsync(script);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => 
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        builder.UseSetting("RedisConfiguration", _redisContainer.GetConnectionString());
        builder.UseSetting("ConnectionStrings:DefaultConnection", _dbContainer.GetConnectionString());
        builder.UseSetting("RateLimit:MaxRequests", "1000");
        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EnerflowDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add DbContext pointing to the container with Dynamic JSON enabled
            // Use 'public' schema to avoid conflicts with MassTransit's 'transport' schema
            services.AddDbContext<EnerflowDbContext>(options =>
            {
                var dataSourceBuilder = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString());
                dataSourceBuilder.EnableDynamicJson();
                var dataSource = dataSourceBuilder.Build();
                options.UseNpgsql(dataSource, npgsqlOptions => 
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
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
            
            // Register DWSIM Automation (Singleton due to initialization overhead)
            // Register as concrete type since DWSIMFlowsheetBuilder expects the concrete class
            services.TryAddSingleton<DWSIM.Automation.Automation3>();
            
            // Register Builders
            services.TryAddScoped<IFlowsheetBuilder, DWSIMFlowsheetBuilder>();
            
            // Register Mappers
            services.TryAddScoped<IStreamMapper, StreamMapper>();
            services.TryAddScoped<IUnitOperationMapper, UnitOperationMapper>();
            services.TryAddScoped<IConnectionMapper, ConnectionMapper>();
            
            // Register Solvers
            services.TryAddScoped<IResultCollector, ResultCollector>();
            services.TryAddScoped<ISimulationSolver, DWSIMSolver>();
            
            // Register Validation
            services.TryAddScoped<IFlowsheetValidator, FlowsheetValidator>();
            
            // Configure MassTransit host options to ensure bus is fully started before tests run
            services.AddOptions<MassTransitHostOptions>()
                .Configure(options =>
                {
                    options.WaitUntilStarted = true;
                    options.StartTimeout = TimeSpan.FromSeconds(30);
                    options.StopTimeout = TimeSpan.FromSeconds(30);
                });
        });
    }

    public new async Task DisposeAsync()
    {
        // Stop both containers in parallel
        await Task.WhenAll(
            _dbContainer.StopAsync(),
            _redisContainer.StopAsync());
    }
}
