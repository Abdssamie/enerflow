## 2026-01-19 - [Missing Security Headers in Frontend]
**Vulnerability:** The Next.js frontend application was serving responses without critical security headers like `Strict-Transport-Security`, `Content-Security-Policy`, `X-Frame-Options`, and `X-Content-Type-Options`.
**Learning:** Default Next.js configurations do not include these security headers. They must be explicitly configured in `next.config.ts` or via middleware. Relying on defaults leaves the application vulnerable to XSS, Clickjacking, and MIME sniffing attacks.
**Prevention:** Always audit `next.config.ts` or the deployment platform's configuration (e.g., Vercel, Nginx) to ensure security headers are actively enforced. Use a checklist (like OWASP Secure Headers) during project setup.
