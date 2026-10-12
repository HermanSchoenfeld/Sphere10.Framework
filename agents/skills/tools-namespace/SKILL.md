---
name: tools-namespace
description: Use and extend the global Tools namespace (Tools.Array, Tools.Crypto, Tools.Text, ...). Trigger when adding a static utility or when tempted to call a raw BCL API that a tool already covers.
---

# Tools Namespace Skill

Read [framework utility guidance](../../../docs/guidelines/patterns/framework-utilities.md) before choosing or adding a utility.

Static utility classes live in the **global `Tools` namespace** so they're discoverable via `Tools.` intellisense everywhere. See `src/Sphere10.Framework/Collections/Arrays/ArrayTool.cs` for the pattern.

## Before calling a BCL API, check for a tool

Check the actual method and owning project before selecting an alternative. Reuse the framework implementation when it covers the required semantics; a facade name alone does not establish that a replacement method exists.

| Instead of | Use |
|---|---|
| `System.BitConverter` | `EndianBitConverter.Little` / `.Big` |
| `File.WriteAllBytes` | `Tools.FileSystem.WriteAllBytes` |
| array concatenation, cloning or filling | `Tools.Array.Concat` / `.Clone` / `.Fill` |
| complex `string.Join`/`string.Format` | `Tools.Text.*` |
| `Enum.GetValues` / `Enum.Parse` | `Tools.Enums.GetValues<T>()` / `Tools.Enums.ParseEnum<T>(value, ignoreValueCase)` |
| `new Random()` / `RandomNumberGenerator` | `Tools.Crypto.GenerateCryptographicallyRandomBytes(n)` |
| `Activator.CreateInstance` | `TypeActivator.ActivateWithCompatibleArgs(type, args)` or `type.ActivateWithCompatibleArgs(args)` |

`Tools.FileSystem` currently has no `ReadAllBytes` method. Use existing file/stream helpers only when their contracts fit; do not invent `Tools.FileSystem.ReadAllBytes`. `Tools.Reflection` provides reflection access helpers; activation belongs to `Sphere10.Framework.TypeActivator` and its `Type` extensions.

Key tool classes: `Tools.Array`, `Tools.Collection`, `Tools.Crypto`, `Tools.Text`, `Tools.Enums`, `Tools.Values` (futures: `Tools.Values.Future.Explicit(...)`, `.Reloadable(...)`), `Tools.Lambda`/`Tools.Expressions`, `Tools.Reflection`, `Tools.Runtime` (`IsDebugBuild`), `Tools.FileSystem`, `Tools.Streams`, `Tools.Scope`, `Tools.Sqlite`/`Tools.MSSQL`, `Tools.NUnit` (test helpers). Provider, Windows and NUnit helpers require their respective project/package references; namespace discovery does not add those dependencies. Firebird exposes `Sphere10.Framework.Data.FirebirdDAC`, not a `Tools.Firebird` facade.

Use `Tools.Expressions.CreateExpression` for the existing unary/binary operation factories; `BuildPredicate` and `CombinePredicates` are not provided. `Tools.Runtime` has no general framework-version/OS inspection API, and its current `IsWebApp` implementation returns `false`. Follow the pattern guide's exact API and stream-ownership notes.

`Tools.Windows` is a separate static facade with `[ThreadStatic]` singletons (`Registry`, `Services`, `Security`, `Processes`, `Win32`, `Shell`).

## Adding a new tool
1. Create a `static class` in `namespace Tools;` anywhere in the codebase — discovery is automatic via `Tools.` intellisense.
2. Use the license header and `// ReSharper disable CheckNamespace` if the folder namespace differs.
3. Keep methods pure where possible; validate arguments with `Guard.*` (see [guards-and-scopes](../guards-and-scopes/SKILL.md)).
4. Don't duplicate an existing tool's responsibility — extend it instead.
