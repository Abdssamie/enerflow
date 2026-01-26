## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Rate Limiting Bypass behind Proxy
**Vulnerability:** The `RateLimitingMiddleware` in `Enerflow.API` relied on `context.Connection.RemoteIpAddress` without `ForwardedHeadersMiddleware` configured, causing all requests behind a proxy to share the same IP.
**Learning:** In containerized/cloud environments, the application never sees the real client IP directly. Trusting the proxy headers is mandatory for any IP-based logic (Rate Limiting, Auditing).
**Prevention:** Always configure `ForwardedHeadersOptions` with `ForwardedHeaders.All` and clear `KnownNetworks`/`KnownProxies` (or set them correctly) in the startup sequence.

## 2025-02-18 - Incorrect Range Validation for Doubles
**Vulnerability:** `[Range(0, 1000000)]` on a `double` property used the `int` constructor, causing validation to behave unexpectedly or fail to catch invalid double inputs correctly (or just be confusing).
**Learning:** `RangeAttribute` constructors are sensitive to literal types. Passing integer literals invokes integer comparison logic, which may not be compatible or correct for floating-point properties.
**Prevention:** Always use `d` suffix (e.g., `0d`, `100d`) or explicit `typeof(double)` overload when validating floating-point values to ensure the correct validator is used.
