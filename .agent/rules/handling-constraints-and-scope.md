---
trigger: always_on
---

When you find out that you are stuck, you can't implement something, you hit a constraint (e.g., test framework limitations, database provider mismatches), and you think that the easiest way to proceed is by using a non-production approach (like hacking production code to support test quirks), then you are wrong. 

**NEVER do that.** 

**Strictly Forbidden:**
- Modifying production `DbContext` or services to support EF Core InMemory provider quirks (e.g., adding `if (Database.IsInMemory())` blocks).
- Bypassing architecture rules to "just make it work" or "just make tests pass".
- Compromising type safety or domain integrity for convenience.

**Required Action:**
Always come back to the user when you are faced with such situations. Explain the constraint clearly. User feedback is the solution to every hurdle you face. It is better to stop and ask than to commit corrupt or hacky code.