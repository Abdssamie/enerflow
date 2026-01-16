using Enerflow.API.Extensions;
using Enerflow.API.Middleware;
using Enerflow.API.Services;
using Enerflow.Domain.Interfaces;
using MassTransit;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure NewId to use Process ID for uniqueness across multiple instances on same host
MassTransit.NewId.SetProcessIdProvider(new MassTransit.NewIdProviders.CurrentProcessIdProvider());

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Redis connection for rate limiting
var redisConfiguration = builder.Configuration["RedisConfiguration"]
    ?? throw new InvalidOperationException("RedisConfiguration is not set in configuration");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConfiguration);
    configuration.AbortOnConnectFail = false; // Allows app to start even if Redis is temporarily unavailable
    return ConnectionMultiplexer.Connect(configuration);
});

// Configure PostgreSQL connection for MassTransit transport
var dbConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not set in configuration");

// Configure PostgreSQL as the MassTransit message transport
builder.Services.ConfigurePostgresTransport(dbConnectionString);

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    x.UsingPostgres((context, cfg) =>
    {
        cfg.AutoStart = true;

        // Use System.Text.Json serialization
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
        options.StartTimeout = TimeSpan.FromSeconds(10);
        options.StopTimeout = TimeSpan.FromSeconds(30);
    });

// Register Job Producer service
builder.Services.AddScoped<IJobProducer, JobProducer>();

// Persistence

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Apply rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.MapControllers();

app.Run();
