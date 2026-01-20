# Sentinel's Journal

## 2025-02-18 - [Hardcoded Secrets in Worker Config]
**Vulnerability:** Found database credentials hardcoded in `Enerflow.Worker/appsettings.json`.
**Learning:** Worker services often get less scrutiny than public APIs, leading to "lazy" config practices where secrets are committed.
**Prevention:** Enforce `.gitignore` for `appsettings.Development.json` and ensure CI pipelines validate that `appsettings.json` contains no secrets.
