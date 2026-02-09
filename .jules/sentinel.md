## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Hardcoded Default Password Fallback in Code
**Vulnerability:** `PostgresTransportExtensions.cs` in both API and Worker projects contained a hardcoded fallback password (`enerflow_password`) if the connection string's password was missing.
**Learning:** Hardcoded fallbacks in utility classes can silently allow insecure configurations to pass in production if the environment variables are misconfigured, potentially exposing the system if the default password is guessed or used.
**Prevention:** Remove hardcoded defaults for sensitive parameters like passwords. Throw explicit exceptions when required configuration is missing, forcing the deployment to be configured correctly ("Fail Securely").
