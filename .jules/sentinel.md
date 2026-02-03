## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Missing Forwarded Headers in API
**Vulnerability:** Rate limiting relied on `RemoteIpAddress` without `ForwardedHeadersMiddleware`. In containerized environments, this resolves to the internal proxy IP, causing all users to share the same rate limit bucket (DoS risk) or bypassing IP-based bans.
**Learning:** `RateLimitingMiddleware` effectively becomes a global throttle instead of per-user throttle when running behind a proxy without forwarded headers.
**Prevention:** Ensure `app.UseForwardedHeaders()` is called early in the pipeline (before rate limiting) and `ForwardedHeadersOptions` are configured to trust upstream proxies in Docker/K8s environments.
