## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Rate Limiting Bypass via Proxy IP
**Vulnerability:** `RateLimitingMiddleware` used `context.Connection.RemoteIpAddress` without `ForwardedHeadersMiddleware` being configured or enabled.
**Learning:** In containerized environments (Docker/K8s), the app sees the proxy's IP, not the client's. This effectively made the rate limiter global (limiting the proxy IP) rather than per-client, or allowed clients to bypass limits if the proxy rotated IPs.
**Prevention:** Always configure `ForwardedHeadersOptions` with `KnownIPNetworks.Clear()` and `ForwardedHeaders.All` in `Program.cs` for containerized apps, and ensure `app.UseForwardedHeaders()` is called early in the pipeline.
