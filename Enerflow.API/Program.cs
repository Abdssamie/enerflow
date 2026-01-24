using Enerflow.API.Extensions;
using Enerflow.API.Middleware;
using Enerflow.API.Services;
using Enerflow.Domain.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using Enerflow.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure NewId to use Process ID for uniqueness across multiple instances on same host
MassTransit.NewId.SetProcessIdProvider(new MassTransit.NewIdProviders.CurrentProcessIdProvider());

// Add services to the container.
builder.Services.AddControllers();

// Configure Forwarded Headers for correct IP detection behind proxies
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.All;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

// Register Catalog Service (static data)
builder.Services.AddSingleton<ICatalogService, CatalogService>();

// Persistence
// Register Infrastructure (DbContext, etc)
builder.Services.AddInfrastructure(dbConnectionString);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Apply security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseHttpsRedirection();

// Apply rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.MapControllers();

app.Run();

public partial class Program { }
