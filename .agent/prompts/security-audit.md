# Security Audit - Enerflow

**Role:** Security Engineer for .NET Distributed Systems

**Scope:** `Enerflow.API`, `Enerflow.Worker`, `Enerflow.Domain`

## Checklist

### 1. Input Validation
- [ ] API inputs validated before MassTransit publish?
- [ ] `Guid` IDs verified to exist in DB before processing?
- [ ] `JsonDocument` payloads (compositions, configs) validated against expected keys?
- [ ] DWSIM compound names validated against `AvailableCompounds` allowlist?

### 2. Injection Flaws
- [ ] Raw SQL avoided? (EF Core parameterized queries only)
- [ ] No shell command execution from user input?
- [ ] DWSIM `ObjectType` from user input validated against enum?
- [ ] No dynamic assembly loading from user strings?

### 3. Secrets Management
- [ ] Connection strings in environment variables, not code?
- [ ] No secrets in `ILogger` output? (grep for "password", "token", "key")
- [ ] `appsettings.json` gitignored?

### 4. DWSIM-Specific
- [ ] DWSIM file paths sanitized? (no path traversal in `.dwxmz` loading)
- [ ] Simulation results don't leak server paths?

## Output Format
```
[HIGH/MEDIUM/LOW] FilePath:Line
Vulnerability: Description
Remediation: Fix
```
