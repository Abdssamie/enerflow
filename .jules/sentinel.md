## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Rate Limiting Bypass behind Proxy
**Vulnerability:** The `RateLimitingMiddleware` in `Enerflow.API` relied on `context.Connection.RemoteIpAddress` without `ForwardedHeadersMiddleware` configured, causing all requests behind a proxy to share the same IP.
**Learning:** In containerized/cloud environments, the application never sees the real client IP directly. Trusting the proxy headers is mandatory for any IP-based logic (Rate Limiting, Auditing).
**Prevention:** Always configure `ForwardedHeadersOptions` with `ForwardedHeaders.All` and clear `KnownNetworks`/`KnownProxies` (or set them correctly) in the startup sequence.

## 2025-02-18 - Hardcoded Design Time Credentials
**Vulnerability:** Found a hardcoded connection string with password in `DesignTimeDbContextFactory.cs`.
**Learning:** Developers often hardcode credentials in `IDesignTimeDbContextFactory` to make `dotnet ef` tools work easily, bypassing the `IConfiguration` flow used in the runtime app. This creates a hidden credential leak.
**Prevention:** Always implement `ConfigurationBuilder` in `DesignTimeDbContextFactory` to mirror the runtime configuration resolution (appsettings + env vars) and strictly avoid hardcoded fallbacks.
