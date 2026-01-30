## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Rate Limiting Bypass behind Proxy
**Vulnerability:** The `RateLimitingMiddleware` in `Enerflow.API` relied on `context.Connection.RemoteIpAddress` without `ForwardedHeadersMiddleware` configured, causing all requests behind a proxy to share the same IP.
**Learning:** In containerized/cloud environments, the application never sees the real client IP directly. Trusting the proxy headers is mandatory for any IP-based logic (Rate Limiting, Auditing).
**Prevention:** Always configure `ForwardedHeadersOptions` with `ForwardedHeaders.All` and clear `KnownNetworks`/`KnownProxies` (or set them correctly) in the startup sequence.

## 2025-02-18 - Missing Input Validation on DTOs
**Vulnerability:** API DTOs (e.g., `CreateSimulationRequest`) lacked validation attributes, allowing unbounded strings and physically impossible values (negative temperature) to be processed.
**Learning:** Even with strictly typed languages like C#, `required` only enforces presence, not content validity. This can lead to DoS (huge strings) or logic errors deeper in the simulation engine.
**Prevention:** Always use `System.ComponentModel.DataAnnotations` (`[StringLength]`, `[Range]`) on public DTOs.
