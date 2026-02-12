## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-23 - Rate Limiting Bypass in Containerized API
**Vulnerability:** The rate limiting middleware (`RateLimitingMiddleware`) relied on `RemoteIpAddress` without `ForwardedHeadersMiddleware`, causing it to rate limit the reverse proxy IP instead of individual clients in containerized environments.
**Learning:** Middleware order and configuration are critical for security features like rate limiting. Simply adding a rate limiter is insufficient if the underlying IP resolution is incorrect.
**Prevention:** Always configure `ForwardedHeadersOptions` with cleared `KnownNetworks`/`KnownProxies` for containerized apps behind dynamic load balancers, and place `UseForwardedHeaders` at the start of the pipeline.
