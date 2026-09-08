---
name: tools-namespace
description: Use and extend the global Tools namespace (Tools.Array, Tools.Crypto, Tools.Text, ...). Trigger when adding a static utility or when tempted to call a raw BCL API that a tool already covers.
---

# Tools Namespace Skill

Static utility classes live in the **global `Tools` namespace** so they're discoverable via `Tools.` intellisense everywhere. See `src/Sphere10.Framework/Collections/Arrays/ArrayTool.cs` for the pattern.

## Before calling a BCL API, check for a tool
| Instead of | Use |
|---|---|
| `System.BitConverter` | `EndianBitConverter.Little` / `.Big` |
| `File.ReadAllBytes` / `File.WriteAllBytes` | `Tools.FileSystem.*` |
| `Array.Copy` / `Buffer.BlockCopy` | `Tools.Array.*` |
| complex `string.Join`/`string.Format` | `Tools.Text.*` |
| `Enum.GetValues` / `Enum.Parse` | `Tools.Enum.*` |
| `new Random()` / `RandomNumberGenerator` | `Tools.Crypto.GenerateCryptographicallyRandomBytes(n)` |
| `Activator.CreateInstance` | `Tools.Reflection.ActivateWithCompatibleArgs` |

Key tool classes: `Tools.Array`, `Tools.Collection`, `Tools.Crypto`, `Tools.Text`, `Tools.Enum`, `Tools.Values` (futures: `Tools.Values.Future.Explicit(...)`, `.Reloadable(...)`), `Tools.Lambda`/`Tools.Expression`, `Tools.Reflection`, `Tools.Runtime` (`IsDebugBuild`), `Tools.FileSystem`, `Tools.Streams`, `Tools.Scope`, `Tools.Sqlite`/`Tools.MSSQL`/`Tools.Firebird`, `Tools.NUnit` (test helpers).

`Tools.Windows` is a separate static facade with `[ThreadStatic]` singletons (`Registry`, `Services`, `Security`, `Processes`, `Win32`).

## Adding a new tool
1. Create a `static class` in `namespace Tools;` anywhere in the codebase — discovery is automatic via `Tools.` intellisense.
2. Use the license header and `// ReSharper disable CheckNamespace` if the folder namespace differs.
3. Keep methods pure where possible; validate arguments with `Guard.*` (see [guards-and-scopes](../guards-and-scopes/SKILL.md)).
4. Don't duplicate an existing tool's responsibility — extend it instead.
