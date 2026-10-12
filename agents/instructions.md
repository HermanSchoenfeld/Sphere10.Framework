# Shared Coding Agent Instructions

These are the authoritative workflow instructions for agents working in the
Sphere10 Framework repository, including Codex, Claude Code and GitHub Copilot.
Implementation rules belong in the referenced guidelines and topic files.

## Required guidance loading

Before planning or changing files:

1. Read [the master entry point](README.md) and these instructions in full.
2. Read the applicable scope: [source](scopes/source.md) for `src/**`,
   [tests](scopes/tests.md) for `tests/**`, and [demos and utilities](scopes/utils.md)
   for `utils/**`. These paths are relative to the Framework repository root.
3. Read [the skill catalog](skills/README.md), then every matching `SKILL.md`
   in full and the supporting references it selects. Native skill discovery
   does not mean those bodies have already been read.
4. For code changes and C# examples, read [code-style](skills/code-style/SKILL.md)
   and [Code Styling](../docs/guidelines/code-styling.md). Read the
   [Patterns and Practices index](../docs/guidelines/patterns-and-practices.md)
   and every topic whose selection criteria apply to the task. Read
   [3-Tier Architecture](../docs/guidelines/3-tier-architecture.md) when changing
   architectural layers. Select additional topics as the scope changes.
5. Read affected project READMEs and the relevant guides in the
   [documentation index](../docs/README.md).

Read each source once unless it changes; a source already loaded in full does
not need another read. Resolve links relative to the containing canonical file,
not the shell's working directory or a client alias. Direct canonical reads work
before local alias setup. For discovery and maintenance, read
[the agent guidance structure](../docs/guidelines/patterns/agent-guidance.md).

The user's explicit instructions take precedence. These workflow instructions
remain authoritative over skills; coding and implementation rules live in their
referenced guidelines. A skill does not expand the authorized task or authorize
new dependencies. Keep client entrypoints as pointers or aliases to the central
store.

## Investigate and implement

- Understand the existing method, class, library and real consumer paths before
  changing them. Apply the selected practices and blend changes into the codebase.
- Keep the user's requested scope and previously accepted constraints in view.
  Identify callers, registrations, examples and documentation affected by the work.
- Update affected consumers with the capability they use; check the real startup
  and user-facing paths as well as the supporting abstraction.
- Preserve unrelated working-tree changes. Keep a file's Git history when moving
  it and update its callers, relative paths, references and solution items.

## Validate and document

- Run the checks appropriate to the actual change. For test work, read the
  [testing practices](../docs/guidelines/patterns/testing.md) and
  [unit-testing skill](skills/unit-testing/SKILL.md).
- Report the checks actually executed and any material unresolved limitation.
  Source inspection is not evidence that a test passed.
- Update affected project READMEs when public API, behavior or usage changes.
  Keep documentation examples aligned with the configuration and code that run.
- Audit the complete change for stale references, omitted consumers, duplicate
  guidance bodies and capabilities that were added but never adopted.
- Keep both solutions' Agent Guidance and Misc items current when
  tracked documentation, guidance or script files are added, moved or removed.

## Maintain skills and patterns

When adding, changing, renaming or retiring guidance, complete this workflow:

1. Read [the guidance structure](../docs/guidelines/patterns/agent-guidance.md),
   [skill catalog](skills/README.md) and
   [pattern index](../docs/guidelines/patterns-and-practices.md). Inspect the
   existing skills and topics and their consumers; extend the existing owner
   when it already covers the guidance.
   Verify implementation rules and examples against current framework APIs and
   actual consumer paths before making them authoritative.
2. Put reusable implementation rules in a focused file under
   `docs/guidelines/patterns/`. Keep `patterns-and-practices.md` as the task index.
   Keep formatting, naming and source conventions in `code-styling.md`, and
   workflow in this file. Maintain each rule body in one canonical location.
3. Create repository skills only at `agents/skills/<skill-name>/SKILL.md`.
   Use a lowercase hyphenated name matching the directory and valid YAML
   frontmatter with `name` and a `description` that identifies when the skill
   applies. Keep the skill's task procedure concise and link the relevant
   pattern topics rather than copying their rule bodies. Store supporting
   references, scripts and assets beside the skill only when they are needed.
4. State when each topic or supporting reference must be read. Keep detailed
   material in the selected topic or reference so agents load the parts their
   task needs. Use portable relative links that work from the canonical store
   and through the native skill aliases; do not use workstation-specific paths.
5. Register skills in `agents/skills/README.md` with their selection criteria,
   and pattern topics in `docs/guidelines/patterns-and-practices.md` with their
   task criteria. Update affected skills, documentation and entrypoint links
   so the new guidance is discoverable and superseded references are removed.
6. Add or update the canonical files and supporting resources in both solutions'
   Agent Guidance or Misc folders. Keep project READMEs in their real projects
   and the root README only in Misc. Preserve file history and existing
   rules and examples when extracting or moving guidance.
   Keep client entrypoints as pointers and the local discovery paths as aliases;
   never add copied skill trees or independently maintained client rule bodies.
7. Before finishing, validate changed skill frontmatter and naming, local links,
   anchors and path casing, topic/catalog coverage, and both solution files.
   Check incoming references as well as outgoing links after moves or removals.
   Run `pwsh -File ./scripts/setup-agent-skills.ps1 -Check` from the Framework
   root; if aliases need repair, follow the existing setup procedure and rerun
   the check. Verify aliases identify the canonical files and remain ignored by
   Git. Run any new or changed supporting scripts with meaningful fixtures and
   confirm the guidance can be reached from its intended entrypoint.

Changing guidance does not expand the task's scope, add authorization or change
the user's explicit preferences. Report the maintenance checks actually run.

## Committing

**NEVER run `git commit` or another command that creates commits unless the user
explicitly asks in the current message.** This overrides workflow, scenario and
skill defaults, including upgrade-agent strategies. Leave changes uncommitted
until that request.

When committing is requested:

- Prefer granular commits per logical task. Group related files that serve the
  same purpose; do not use one commit per file mechanically.
- Stage only files relevant to that logical change and preserve unrelated work.
- Commit separate logical changes in dependency order.
- Use imperative titles and a detail body describing the concrete changes:

  ```text
  Short summarative title of overall change

  - specific logical change 1
  - specific logical change 2
  ```

- Do not amend or force-push unless explicitly asked to.
