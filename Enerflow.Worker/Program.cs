using Enerflow.Domain.Interfaces;
using Enerflow.Infrastructure.Persistence;
using Enerflow.Worker.Consumers;
using Enerflow.Worker.Extensions;
using Enerflow.Simulation.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure PostgreSQL connection
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not set in configuration");

// Register Entity Framework DbContext
builder.Services.AddDbContext<EnerflowDbContext>(options =>
{
    options.UseNpgsql(dbConnectionString);
});

// Configure PostgreSQL as the MassTransit message transport
builder.Services.ConfigurePostgresTransport(dbConnectionString);

// Register Simulation Services
builder.Services.AddScoped<UnitOperationFactory>();
builder.Services.AddScoped<ISimulationService, SimulationService>();

builder.Services.AddMassTransit(x =>
{
    // Register the consumer with its definition to enforce concurrency limits
    x.AddConsumer<SimulationJobConsumer, SimulationJobConsumerDefinition>();

    x.SetKebabCaseEndpointNameFormatter();

    x.UsingPostgres((context, cfg) =>
    {
        cfg.AutoStart = true;

        // Use System.Text.Json serialization (matches API configuration)
        cfg.ConfigureJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            return options;
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Configure MassTransit host options
builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = true;
        options.StartTimeout = TimeSpan.FromSeconds(30);
        options.StopTimeout = TimeSpan.FromSeconds(30);
    });

// Configure host shutdown options for graceful shutdown
builder.Services.AddOptions<HostOptions>()
    .Configure(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(60);
    });

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Enerflow.Worker");
logger.LogInformation("Enerflow Worker starting...");
logger.LogInformation("Listening for SimulationJob messages on PostgreSQL transport");
logger.LogInformation("Database: {ConnectionString}",
    dbConnectionString.Split(';').FirstOrDefault(s => s.StartsWith("Database=")) ?? "configured");

await host.RunAsync();
