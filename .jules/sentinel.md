## 2025-02-18 - Hardcoded Database Credentials in Worker
**Vulnerability:** Found a hardcoded PostgreSQL connection string with password in `Enerflow.Worker/appsettings.json`.
**Learning:** `AGENTS.md` claimed `appsettings.json` was gitignored, leading to a false sense of security. Developers likely committed it to share configuration, inadvertently sharing secrets.
**Prevention:** Explicitly ignore `appsettings.Development.json`, provide a template file for local setup, and strip secrets from the shared `appsettings.json`.

## 2025-02-18 - Missing Frontend Security Headers
**Vulnerability:** The Next.js frontend (`web/`) lacked standard security headers (HSTS, X-Frame-Options, etc.), relying on defaults which are insufficient for modern security.
**Learning:** Even if the API has security middleware, the frontend served independently needs its own security configuration.
**Prevention:** Explicitly configure headers in `next.config.ts` for all frontend projects.
