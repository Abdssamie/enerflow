## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Hardcoded Password Fallback in Transport Extensions
**Vulnerability:** `Enerflow.API` and `Enerflow.Worker` contained hardcoded password ("enerflow_password") fallbacks in `PostgresTransportExtensions.cs` which would be used if the connection string lacked a password.
**Learning:** Library extension methods should not assume default credentials, as this encourages insecure configuration and can mask configuration errors.
**Prevention:** Enforce strict configuration validation; if credentials are required but missing, fail fast rather than falling back to insecure defaults.
