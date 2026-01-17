namespace Enerflow.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        // Basic CSP to prevent XSS, can be adjusted based on needs
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: https:; object-src 'none'; frame-ancestors 'none';";

        await _next(context);
    }
}
