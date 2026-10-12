# Agent Guidance Storage and Discovery

Keep repository rules and skills in one tracked master store. Client entrypoints
and local filesystem aliases expose that store through the names each client
recognizes; they must not become independently maintained copies of its rules.

## Canonical guidance

The master store is the repository-root `agents/` directory:

- [Master entry point](../../../agents/README.md) defines required reading and precedence.
- [Shared instructions](../../../agents/instructions.md) define repository workflow.
- [Source](../../../agents/scopes/source.md), [test](../../../agents/scopes/tests.md) and [demo/utility](../../../agents/scopes/utils.md) scopes select workflow and references for their repository-root directories.
- [Skill catalog](../../../agents/skills/README.md) selects the relevant canonical skills and resources.

Root and scoped `AGENTS.md` files are small tracked pointers that explicitly
require reading their canonical target in full and following its required links.
Copilot's tracked entrypoints also point directly to the master store. These
reads work before local alias setup; native discovery does not replace them.

The user's explicit instructions take precedence over repository guidance;
shared workflow instructions and selected guidelines take precedence over skills. Selecting a skill does not
expand the authorized task or authorize additional dependencies.

## Local discovery aliases

`.agents` and `.claude` are ordinary directories whose only repository-provided
entry is a `skills` alias. On Windows, those two skill aliases and
`.github/skills` are directory junctions to the real `agents/skills` directory.
On non-Windows platforms, the three skill aliases are relative symbolic links. The containers
remain available for client-specific settings; only their `skills` subtree is shared.

There are eight instruction file aliases: `CLAUDE.md` and `GEMINI.md` beside
each `AGENTS.md` in the repository root, `src`, `tests` and `utils`. Windows uses
file hard links; non-Windows platforms use relative symbolic links. Each file exposes exactly
the content of its adjacent `AGENTS.md`, including its canonical read-in-full
pointer. Do not maintain separate imports or client-specific rule bodies.

The [setup script](../../../scripts/setup-agent-skills.ps1) defines those three directory aliases and eight instruction aliases. Its Windows checks compare actual file identities, rather than treating an equal-content copy as a hard link; its non-Windows checks resolve symbolic-link targets. It validates the complete plan before changing aliases, and removes only an alias entry when repairing a managed link.

Aliases and the Windows identity state at `agents/.local-alias-state.json` are
[ignored local files](../../../.gitignore). Git does not run setup automatically when a repository is
cloned. Run setup manually for each clone or worktree, and rerun it after moving
the checkout or atomically replacing an `AGENTS.md` file. Windows hard links
can otherwise retain the replaced file. Setup recognizes its managed aliases
and refuses to overwrite independent files; its check mode is read-only.

For the commands, prerequisites, platform details and client verification,
follow [Agent Guidance](../../agent-guidance.md#local-alias-setup).

## Skills and supporting resources

Keep every skill's full instructions, references, scripts and assets beneath
its canonical directory in `agents/skills`. All three discovery aliases expose
the same directory tree; do not generate skill metadata copies or duplicate
supporting resources for each client. Edits to the real files are immediately
visible through those aliases.

Resolve skill resources relative to their canonical directory and read the
selected sources in full. Write links so they also resolve when a client reads
through a native skill alias. For references outside the shared skill tree,
use a path to the real repository target rather than assuming the alias's parent
is `agents/`. Keep the master catalog and tracked entrypoint pointers usable
without alias setup, and verify the actual client session loads the relevant
canonical guidance.

When adding, changing or moving guidance, follow the [maintenance workflow](../../../agents/instructions.md#maintain-skills-and-patterns). Keep the skill catalog, [pattern task index](../patterns-and-practices.md), incoming links and solution items aligned with the canonical layout; the native aliases expose that layout without copied instruction bodies.
