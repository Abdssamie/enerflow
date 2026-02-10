## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Missing Validation on Large JSON Imports
**Vulnerability:** The `ImportSimulation` endpoint accepted unbounded `SimulationExportDto` payloads, allowing potential DoS via large lists or invalid data.
**Learning:** DTOs defined in the Controller layer (instead of Domain) were missed by standard domain validation checks. Reusing "Export" DTOs for "Import" without adding validation metadata is a common oversight.
**Prevention:** Always add `[Length]` or `[MaxLength]` attributes to collection properties in DTOs used for ingestion, even if they are primarily for export.
