using StackExchange.Redis;

namespace Enerflow.API.Middleware;

/// <summary>
/// Redis-backed rate limiting middleware using fixed window algorithm.
/// Limits requests to 10 per minute per IP address.
/// Uses distributed Redis counters to support horizontal scaling.
/// Implements atomic increment+expire using Lua script to prevent race conditions.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    private const int MaxRequests = 15;
    private const int WindowSeconds = 60;
    private const string RateLimitKeyPrefix = "ratelimit:";

    /// <summary>
    /// Lua script to atomically increment counter and set TTL if it's a new key.
    /// Returns [current_count, ttl_seconds] in a single Redis operation.
    /// This prevents the race condition where a key could exist without expiration.
    /// </summary>
    private static readonly LuaScript RateLimitScript = LuaScript.Prepare(@"
        local current = redis.call('INCR', @key)
        if current == 1 then
            redis.call('EXPIRE', @key, @expiry)
        end
        local ttl = redis.call('TTL', @key)
        return {current, ttl}
    ");

    public RateLimitingMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var redisKey = $"{RateLimitKeyPrefix}{clientId}";

        try
        {
            var db = _redis.GetDatabase();

            var scriptResult = await db.ScriptEvaluateAsync(RateLimitScript, new
            {
                key = (RedisKey)redisKey,
                expiry = WindowSeconds
            });

            // Handle potential null result from Redis script
            if (scriptResult.IsNull)
            {
                _logger.LogWarning("Redis script returned invalid result for client {ClientId}; failing open.", clientId);
                await _next(context);
                return;
            }

            var result = (RedisResult[])scriptResult!;
            var currentCount = (long)result[0];
            var ttlSeconds = (long)result[1];

            var resetTime = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds).ToUnixTimeSeconds();

            if (currentCount > MaxRequests)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for client {ClientId}. Count: {Count}/{Max}",
                    clientId, currentCount, MaxRequests);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = ttlSeconds.ToString();
                AddRateLimitHeaders(context.Response, 0, resetTime);

                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = $"Too many requests. Maximum {MaxRequests} requests per minute allowed.",
                    retryAfter = ttlSeconds
                });
                return;
            }

            // Add headers to the successful response
            var remaining = MaxRequests - currentCount;
            context.Response.OnStarting(() =>
            {
                AddRateLimitHeaders(context.Response, remaining, resetTime);
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            // Fail open: allow request to proceed if Redis is unavailable
            // This prevents Redis outages from taking down the entire API
            _logger.LogError(ex, "Rate limiting error for client {ClientId}; failing open.", clientId);
        }

        await _next(context);
    }

    /// <summary>
    /// Adds standard rate limit headers to the response.
    /// </summary>
    private void AddRateLimitHeaders(HttpResponse response, long remaining, long reset)
    {
        response.Headers["X-RateLimit-Limit"] = MaxRequests.ToString();
        response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
        response.Headers["X-RateLimit-Reset"] = reset.ToString();
    }
}
