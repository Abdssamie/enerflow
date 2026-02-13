## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Insecure Default Password Fallbacks
**Vulnerability:** `Enerflow.API` and `Enerflow.Worker` contain hardcoded default passwords ("enerflow_password") in `PostgresTransportExtensions.cs`.
**Learning:** These defaults are intended to simplify local development (matching docker-compose defaults) but introduce a risk of production deployments using weak defaults if configuration is missing.
**Prevention:** Remove the null-coalescing default values in code. Enforce explicit configuration via `appsettings.Development.json` or environment variables, causing the application to fail securely (crash) if credentials are missing.
