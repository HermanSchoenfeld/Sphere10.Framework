# Repository Instructions

Before doing substantive work in this repository:

1. Read `.github/copilot-instructions.md` in full and follow it as the
   authoritative repository guidance.
2. Read `.github/skills/README.md` and identify every skill whose stated use
   matches the task.
3. Read each matching skill's `SKILL.md` in full before planning or making
   changes. For every code change, always load
   `.github/skills/code-style/SKILL.md` in addition to any task-specific skills.
4. Follow any references, scripts, assets, or supporting instructions selected
   by those skills when they are relevant to the task.
5. Re-evaluate the skill selection whenever the task's scope changes.

Do not treat the Copilot instructions or matching skills as optional. If a
skill conflicts with `.github/copilot-instructions.md`, the Copilot
instructions take precedence.

## Eat your own dog food

This applies to all development in this repository. The purpose of the
framework is to provide reusable code, patterns and architecture, and its own
development must prefer those foundations.

- Before implementing anything, find and understand the existing code,
  abstractions, utilities, patterns and architecture that address the problem.
  Reuse them rather than creating parallel implementations or local substitutes.
- Extend or compose the existing framework where it falls short. Add a new
  abstraction only when the existing architecture cannot reasonably support the
  requirement; explain that gap and keep the addition consistent with the framework.
- When adding or improving a capability, adopt it in the relevant repository
  consumers in the same change. Supporting code alone is not a completed
  implementation.
- Trace the real startup, registration and user-facing execution paths. Update
  existing applications, demos and examples to consume the new APIs.
- Replace the superseded manual configuration or duplicated logic in those
  consumers. Do not leave the old path active alongside an unused replacement.
- Keep compatibility code only where it has an identified consumer or purpose,
  and clearly distinguish it from the recommended, actively demonstrated path.
- Verify the integration through the actual consumer's configuration and
  behavior, in addition to testing the supporting API in isolation.
- Update documentation to match the code that runs. Before claiming completion,
  audit the whole change for new capabilities that were added but never adopted.
