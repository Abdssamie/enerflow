# APM v0.6 - Spec Distiller Guide (Token-Efficient)

This guide defines the "Spec Distiller" protocol, replacing the verbose multi-round discovery with a single-shot high-density requirement extraction.

## Objective
Convert raw user requirements into a structured, compact, and validated project specification in <1500 tokens.

## Spec Distiller Protocol (One-Shot)

When the user provides raw requirements, you must output a single response containing:
1. **Domain Analysis**: Identify 3-5 core technical domains.
2. **Architecture Blueprint**: High-level component relationship (text-only, compact).
3. **Implementation Plan (JSON)**: A compact JSON block containing the task sequence.

### JSON Spec Format
```json
{
  "project_name": "string",
  "phases": [
    {
      "id": "P1",
      "title": "Phase Title",
      "tasks": [
        {
          "id": "T1.1",
          "spec": "Dense task requirement (max 100 words)",
          "deps": ["T1.0"],
          "test_criteria": ["Property A", "Property B"],
          "est_tokens": 500
        }
      ]
    }
  ]
}
```

## Gap-Check Heuristics
Before outputting the spec, internally check for:
- **Ambiguous Tech Stack**: If missing, default to project's established stack or ask once.
- **External Dependencies**: Identify APIs/Auth needs.
- **State Management**: Note how data flows between tasks.

## Constraints
- **CONCISE**: No conversational filler.
- **DENSE**: Use technical shorthand where appropriate.
- **FOCUSED**: Only include requirements that affect implementation.
