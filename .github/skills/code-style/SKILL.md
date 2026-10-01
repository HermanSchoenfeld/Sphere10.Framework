---
name: code-style
description: Baseline C# style for this repo — Egyptian braces, tabs, camelCase locals, member ordering, file-scoped namespaces, license header. Apply to every code change.
---

# Code Style Skill

Apply these rules to every file you create or edit. Full rules live in [../../copilot-instructions.md](../../copilot-instructions.md); this is the working checklist.

## Formatting
- Opening braces at **end of line** (K&R / Egyptian), never on a new line.
- **Tabs** for indentation (width 4); spaces only for aligning comments.
- `var` over explicit types wherever possible.
- Omit braces on single-statement `if`/`foreach`/etc.
- No redundant `else` after `return`/`throw`.
- Don't wrap lines unless > ~170 chars. If wrapping after `(`, put `)` on its own line at the originating indent.
- Base/sibling constructor calls (`: base(...)`, `: this(...)`) on the next line, tab-indented.

## Naming
- PascalCase for types, methods, properties, and non-private fields.
- camelCase for local variables and parameters, including lambda, loop, and catch variables.
- `_camelCase` for private fields.
- Self-describing names; no cryptic abbreviations.

## Platform-specific extensions

- Reserve unprefixed application UI names for the shared `Sphere10.Framework.Application.UI` layer. Platform extensions use `Blazor` or `WinForms`, including interfaces: `IBlazorApplicationBlock : IApplicationBlock` and `IWinFormsApplicationBlock : IApplicationBlock`.
- Apply the prefix consistently to corresponding models, builders, hosts and decorators. Reuse shared types directly when no extension is required; avoid same-name platform redeclarations and the namespace aliases they force.

## Structure
- File-scoped namespaces (`namespace X;`), following `CompanyName.Product.Tier.Domain` — not folder structure.
- `ImplicitUsings` is disabled: add explicit `using` directives.
- `Nullable` is `annotations`; `LangVersion` is `latest`. Existing annotations are supported, but nullable-reference analysis warnings are disabled. Declare reference types without `?` annotations and do not use postfix null-forgiving `!` operators. Configure nullable-reference warning suppression in the project instead. Keep nullable value types when the API requires them.

## Collection-valued data properties

- Use `T[]` for sequence-valued properties on data objects and interfaces, including their base classes and decorators. Do not expose these properties as `IEnumerable<T>`, read-only collection interfaces or list types.
- Materialize sequences; use `Array.Empty<T>()` for empty collections. Copy arrays where needed to preserve ownership of stored collection membership. Mutable lists may remain implementation details; keyed mappings may remain dictionaries.
- Enumerable method inputs and intentionally sequence-producing methods remain appropriate. See [the coding guide](../../../docs/guidelines/code-styling.md#collection-valued-data-properties).

## Member order in a class
1. Events
2. Private fields
3. Protected fields
4. Constructors (simple → complex), finalizers
5. Public properties
6. Internal/protected/private properties
7. Public methods
8. Internal/protected/private methods
9. Inner types

## Comments
- Comment logical segments inside method bodies; one blank line between segments and between members; no blank lines within a segment.

## New files
Every new source file starts with the license header:
```csharp
// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
```
