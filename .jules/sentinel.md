## 2024-01-22 - Found Hardcoded Secrets in Config and Code
**Vulnerability:** Found `enerflow_password` hardcoded in `Enerflow.Worker/appsettings.json`, `DesignTimeDbContextFactory.cs`, and Transport Extensions.
**Learning:** Developers likely used these for convenience during local dev but committed them to the repo. The `appsettings.json` in Worker was particularly risky as it's a runtime config file.
**Prevention:** Ensure `appsettings.json` never contains secrets. Use `appsettings.Development.json` (gitignored) or User Secrets for local dev. Enforce strict code reviews or pre-commit hooks to scan for known secret patterns.
