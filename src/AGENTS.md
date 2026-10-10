# Repository Instructions

Before doing substantive work under this directory:

1. Read `../.github/copilot-instructions.md` in full and follow it as the
   authoritative repository guidance.
2. Read `../.github/skills/README.md` and identify every skill whose stated use
   matches the task.
3. Read each matching skill's `SKILL.md` in full before planning or making
   changes. For every code change, always load
   `../.github/skills/code-style/SKILL.md` in addition to any task-specific
   skills.
4. Follow any references, scripts, assets, or supporting instructions selected
   by those skills when they are relevant to the task.
5. Re-evaluate the skill selection whenever the task's scope changes.

Do not treat the Copilot instructions or matching skills as optional. If a
skill conflicts with `../.github/copilot-instructions.md`, the Copilot
instructions take precedence.

Read [Patterns and Practices](../docs/guidelines/patterns-and-practices.md) when
changing a hierarchy. Classes supplied only for inheritance are abstract and
named `...Base`; concrete implementations need genuine direct consumers and
usable behavior. Put each generic `where` constraint on its own aligned line,
with the opening brace at the end of the final constraint, as specified in
[Code Styling](../docs/guidelines/code-styling.md#generic-constraints).
