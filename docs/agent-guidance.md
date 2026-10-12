# Agent Guidance

The Framework repository keeps its agent instructions and skills in `agents/`.
Small client entrypoints require reading the canonical entry point, workflow
instructions and applicable scope in full, then the selected skills and pattern
topics. Local filesystem aliases expose the same files through the names supported by each client;
there are no separate client rule bodies or generated skill copies. Open the
repository at `Framework` when possible.

## Master store

| Source | Purpose |
|---|---|
| [Master entry point](../agents/README.md) | Required workflow, scope, skill and guideline reading sequence. |
| [Shared instructions](../agents/instructions.md) | Workflow, investigation, validation, documentation and commit rules. |
| [Skill catalog](../agents/skills/README.md) | Select every skill relevant to the task, then read its canonical `SKILL.md` and selected resources. |
| [Source scope](../agents/scopes/source.md), [test scope](../agents/scopes/tests.md), [demo and utility scope](../agents/scopes/utils.md) | Directory context and guide selection for repository-root paths `src/**`, `tests/**` and `utils/**`. |
| [Code Styling](guidelines/code-styling.md) | Complete formatting and naming guidance. |
| [Patterns and Practices](guidelines/patterns-and-practices.md) | Task index for selecting the relevant pattern guides. |
| [Agent guidance pattern](guidelines/patterns/agent-guidance.md) | Master instruction storage, client pointers and local aliases. |
| [3-Tier Architecture](guidelines/3-tier-architecture.md) | Architectural layer responsibilities. |
| [Documentation index](README.md) | Relevant project and framework guides. |

The user's explicit instructions take precedence. Shared workflow instructions
and selected guidelines take precedence over skills. A native skill listing exposes its name and description;
it does not mean the skill body or supporting references have already been read.
The master entry point requires those reads when they apply. Resolve a skill's
links and resources relative to its real directory under `agents/skills`, even
when a client discovered it through an alias.

Select the pattern pages relevant to the change through the task index and the matching skill references. Read those pages in full; the other pattern topics remain available for tasks that need them.

## Local alias setup

Use PowerShell 7.2 or later on Windows or Linux. From the Framework repository root, run:

```powershell
pwsh -File ./scripts/setup-agent-skills.ps1
pwsh -File ./scripts/setup-agent-skills.ps1 -Check
```

The [setup script](../scripts/setup-agent-skills.ps1) creates three directory
aliases to the same real `agents/skills` directory:

| Local directory alias | Real directory |
|---|---|
| `.github/skills` | `agents/skills` |
| `.claude/skills` | `agents/skills` |
| `.agents/skills` | `agents/skills` |

It also creates **eight file aliases**: `CLAUDE.md` and `GEMINI.md` beside each
`AGENTS.md` in the repository root, `src`, `tests` and `utils`. Each pair exposes
exactly the content of its adjacent `AGENTS.md`. They contain no separate
instructions or `@import` bodies.

On Windows, directory aliases are junctions and file aliases are hard links.
On non-Windows systems, they are relative symbolic links. Setup does not require
Windows Developer Mode or Git symlink configuration: these links are created
locally after checkout rather than checked into Git.

The aliases and Windows alias identity state are ignored local files. Run setup
for every clone and worktree, and rerun it after moving a checkout. Also rerun it
after an editor atomically replaces an `AGENTS.md` file: Windows hard links can
otherwise continue to expose the old file. The script uses its local state to
recognize and repair managed aliases and refuses to replace independent files.
`-Check` is read-only and reports missing or incorrect aliases without repairing
them.

Direct guidance loading works before setup. The tracked root and scoped
`AGENTS.md` pointers, Copilot entrypoints and master catalog link directly into
`agents/`. Setup adds native skill discovery and the Claude/Gemini file names.

## Client entrypoints

| Client | Instruction entrypoint | Native repository skills after setup |
|---|---|---|
| Claude Code | Local `CLAUDE.md` aliases to root and applicable scoped `AGENTS.md` pointers. | `.claude/skills/<name>/SKILL.md` aliases. |
| GitHub Copilot | Tracked `.github/copilot-instructions.md` and `.github/instructions/*.instructions.md` point directly to the master store. `src/.github/copilot-instructions.md` covers a workspace opened at `src`. | `.github/skills/<name>/SKILL.md` aliases. |
| Codex | Tracked root and applicable scoped `AGENTS.md` pointers. | `.agents/skills/<name>/SKILL.md` aliases. |
| Cursor Agent | Tracked root and applicable scoped `AGENTS.md` pointers, which require reading the master catalog and matching skills. | Follow the catalog; native skill discovery varies by client version. |
| Gemini CLI | Local `GEMINI.md` aliases to root and applicable scoped `AGENTS.md` pointers. | `.agents/skills/<name>/SKILL.md` aliases. |

The tracked entrypoints explicitly require reading their canonical target in
full and following its guidance links. Keep that requirement when changing a
pointer. Claude and Gemini discover their named instruction files; the local
alias mechanism belongs to this repository's setup script. See the official
[Claude instruction discovery](https://code.claude.com/docs/en/memory),
[Claude skill locations](https://code.claude.com/docs/en/skills#choose-where-skills-load),
[Gemini instruction discovery](https://geminicli.com/docs/cli/gemini-md/) and
[Gemini skills](https://geminicli.com/docs/cli/using-agent-skills/).

Copilot support differs between Visual Studio, VS Code, CLI and cloud agents.
Directory `.instructions.md` pointers supplement clients that do not load scoped
`AGENTS.md`. Visual Studio native agent skills require Visual Studio 2026 18.5 or
later; older clients can still follow the master reading instructions. See the
[instruction support matrix](https://docs.github.com/en/copilot/reference/custom-instructions-support)
and [Visual Studio skills](https://learn.microsoft.com/en-us/visualstudio/ide/copilot-agent-skills?view=visualstudio).

In VS Code, opening only `src` excludes parent customizations by default. Open
`Framework`, or enable `chat.useCustomizationsInParentRepositories` for the
trusted repository. Clients that scan multiple skill locations may display the
same skill more than once; all three local directories expose the same real
files. See [workspace discovery](https://code.visualstudio.com/docs/agent-customization/overview)
and [skills](https://code.visualstudio.com/docs/agent-customization/agent-skills).

Codex scans repository `.agents/skills` directories between its working directory
and the repository root. Its default `AGENTS.md` discovery limit is 32 KiB; the
small tracked entrypoints require reading the full master store. See
[Codex instructions](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
and [skills](https://learn.chatgpt.com/docs/build-skills). Cursor supports root and
nested [AGENTS.md](https://cursor.com/docs/rules).

## Maintain the store

Follow the [guidance maintenance workflow](../agents/instructions.md#maintain-skills-and-patterns)
when adding, changing, renaming or retiring skills or pattern topics.

Edit instructions under `agents/` and skills under `agents/skills`, including
their discovery descriptions. Keep supporting resources beside their canonical
skill and update the catalog when adding or removing a skill. The local aliases
expose changes immediately; no metadata synchronizer or skill-copy generation
is needed. Update both solutions' **Agent Guidance** folders when changing the
tracked guidance layout.

Keep root and scoped `AGENTS.md` files and the Copilot entrypoints as small
read-in-full pointers. Edit an `AGENTS.md` pointer rather than its local
`CLAUDE.md` or `GEMINI.md` aliases, then rerun setup if the file was replaced.
Do not maintain separate client instruction bodies.

## Solution view

Both solutions group the master `agents` store, tracked entrypoint pointers and shared guides under **Agent Guidance**. Its **Guides** subtree contains the documentation indices, architecture guides, coding guidelines and focused pattern topics, plus retained archived papers and source notes. Project, test, demo and resource READMEs are not duplicated as solution items; open them from their real projects or repository directories.

The repository-root README appears once under **Misc**. **Misc → Scripts** contains the packaging scripts and package-consumer index; the alias setup script remains under **Agent Guidance → Scripts**. These virtual folders reference the original files and do not change the repository's physical layout.

## Verify the client session

First run the setup script with `-Check`, then verify the actual client session:

- **Claude:** use `/context` or `/memory` to inspect instruction sources, then
  inspect the `/` menu for repository skills. Confirm the agent read the master
  files and the applicable scope after reading a source, test or demo file.
- **Copilot:** enable custom instructions and inspect Chat References or
  instruction diagnostics. In CLI, `/instructions` and `/skills list` show
  discovery; `/skills info code-style` identifies the selected skill.
- **Codex:** inspect the skill selector and ask the agent to identify the
  canonical instructions, scope and skills it read for a concrete task.
- **Cursor:** inspect applied `AGENTS.md` instructions and ask the agent to
  identify the master guidance and matching canonical skills it read.
- **Gemini:** `/memory show` displays context and `/skills list` displays
  discovery. Reload memory and skills after changes, then confirm the required
  canonical files were read.

Instruction files guide an agent; they do not enforce behavior like a compiler.
This setup targets agent sessions with file-reading tools. Autocomplete and
features without instruction or skill support cannot load the entire reference
chain. Client settings, workspace trust, disabled skills and managed policies
can affect loading. Copilot CLI built-in `explore`, `task` and `code-review`
subagents do not receive repository instructions; custom subagents need
`include-custom-instructions: true`. See the
[CLI command reference](https://docs.github.com/en/copilot/reference/copilot-cli-reference/cli-command-reference).
