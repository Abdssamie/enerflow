using Enerflow.Infrastructure.Persistence;
using Enerflow.Worker.Consumers;
using Enerflow.Worker.Extensions;
using Enerflow.Simulation.Flowsheet.Compounds;
using Enerflow.Simulation.Flowsheet.PropertyPackages;
using Enerflow.Simulation.Flowsheet.Streams;
using Enerflow.Simulation.Flowsheet.FlashAlgorithms;
using Enerflow.Simulation.Flowsheet.UnitOperations;
using Enerflow.Simulation.Flowsheet.Connections;
using Enerflow.Worker.Solvers;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure NewId to use Process ID for uniqueness across multiple instances on same host
NewId.SetProcessIdProvider(new MassTransit.NewIdProviders.CurrentProcessIdProvider());

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

// Register flowsheet managers
builder.Services.AddSingleton<ICompoundManager, CompoundManager>();
builder.Services.AddSingleton<IPropertyPackageManager, PropertyPackageManager>();
builder.Services.AddSingleton<IMaterialStreamFactory, MaterialStreamFactory>();
builder.Services.AddSingleton<IEnergyStreamFactory, EnergyStreamFactory>();
builder.Services.AddSingleton<IUnitOperationFactory, UnitOperationFactory>();
builder.Services.AddSingleton<IFlashAlgorithmManager, FlashAlgorithmManager>();

// Register DWSIM Automation (Singleton due to initialization overhead)
builder.Services.AddSingleton<DWSIM.Automation.AutomationInterface, DWSIM.Automation.Automation3>();

// Register Builders
builder.Services.AddScoped<Enerflow.Worker.Builders.IFlowsheetBuilder, Enerflow.Worker.Builders.DWSIMFlowsheetBuilder>();

// Register Simulation Layer Configurators
builder.Services.AddScoped<IUnitOperationConfigurator, UnitOperationConfigurator>();
builder.Services.AddScoped<IConnectionFactory, ConnectionFactory>();

// Register Solvers
builder.Services.AddScoped<IResultCollector, ResultCollector>();
builder.Services.AddScoped<ISimulationSolver, DWSIMSolver>();

// Register Validation
builder.Services.AddScoped<Enerflow.Worker.Validation.IFlowsheetValidator, Enerflow.Worker.Validation.FlowsheetValidator>();

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

// CRITICAL: Enable DWSIM Automation Mode (Headless)
// This prevents UI popups and enables optimizations for non-interactive use.
DWSIM.GlobalSettings.Settings.AutomationMode = true;
logger.LogInformation("DWSIM Automation Mode enabled");

logger.LogInformation("Listening for SimulationJob messages on PostgreSQL transport");
logger.LogInformation("Database: {ConnectionString}",
    dbConnectionString.Split(';').FirstOrDefault(s => s.StartsWith("Database=")) ?? "configured");

await host.RunAsync();
