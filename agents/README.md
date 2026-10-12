# Repository Agent Guidance

This is the master entry point for coding agents. Repository paths such as
`src/**`, `tests/**` and `utils/**` are relative to the Framework repository root.

Before substantive work:

1. Read [the shared workflow instructions](instructions.md) in full.
2. Read the applicable [source](scopes/source.md), [test](scopes/tests.md) or
   [demo and utility](scopes/utils.md) scope in full, even when your client does
   not load nested instructions automatically.
3. Read [the skill catalog](skills/README.md), then each matching `SKILL.md` in
   full and the supporting sources it selects.
4. For code changes and C# examples, read
   [code-style](skills/code-style/SKILL.md),
   [Code Styling](../docs/guidelines/code-styling.md) and the
   [Patterns and Practices index](../docs/guidelines/patterns-and-practices.md).
   Read every topic selected by the actual task, including
   [Framework Reuse and Adoption](../docs/guidelines/patterns/framework-reuse.md)
   for framework implementation or changes to its consumers. Read
   [3-Tier Architecture](../docs/guidelines/3-tier-architecture.md) when changing layers.
5. Read affected project READMEs and the relevant
   [documentation guides](../docs/README.md). Re-evaluate the topic and skill
   selection as the scope changes.

Read each required source once unless it changes. Resolve Markdown links from
the canonical file containing them. Instructions already loaded in full do not
need another read. Required guidance and matching skills are not optional.

The user's explicit instructions take precedence. Shared workflow instructions
remain authoritative over skills; implementation rules live in the referenced
coding guide and pattern topics. Selecting a skill does not expand the task or
authorize new dependencies.

Read [Agent Guidance Structure](../docs/guidelines/patterns/agent-guidance.md) when
changing instructions, skills, aliases or discovery. Client entrypoints lead to
this master store; maintain the canonical sources and their links.
