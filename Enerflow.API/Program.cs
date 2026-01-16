using Enerflow.API.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

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
