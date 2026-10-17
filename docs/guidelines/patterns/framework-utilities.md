# Framework Utilities

Use the existing framework utility that covers the required semantics. Read the [tools skill](../../../agents/skills/tools-namespace/SKILL.md), [guard skill](../../../agents/skills/guards-and-scopes/SKILL.md) or [crypto skill](../../../agents/skills/crypto/SKILL.md) for the selected domain. The [framework reuse guide](framework-reuse.md) explains adoption of existing framework code.

## Tools Namespace (Maximal Use)

Static utility/tool classes live in the **`Tools`** global namespace so consumers can discover them via `Tools.` intellisense. Always prefer `Tools.*` over raw BCL calls when a tool exists. Key tools include:

- **`Tools.Array`** — concatenation, cloning, filling and transposition.
- **`Tools.Collection`** — generation, repetition, partitioning, searching, `RangeL` and `ResizeArray`.
- **`Tools.Crypto`** — secure random bytes, password hashing, AES helpers and secure erase.
- **`Tools.Text`** — string manipulation, formatting, generation.
- **`Tools.Enums`** — enum descriptions, values, parsing.
- **`Tools.Values`** — futures (`Tools.Values.Future.Explicit(...)`, `Tools.Values.Future.Reloadable(...)`), clipping, comparison.
- **`Tools.Lambda`** — delegate wrappers and setter expressions; **`Tools.Expressions`** — compiled unary/binary operation factories.
- **`Tools.Reflection`** — type and member inspection; use `TypeActivator` or the type activation extensions for construction.
- **`Tools.Runtime`** — `IsDebugBuild`, executable paths, entry-assembly access and design-mode checks.
- **`Tools.FileSystem`** — temp files, read/write, directory management.
- **`Tools.Streams`** — routing, `ReadByteArray`, writing streams to files, compression and encryption.
- **`Tools.Scope`** — cleanup delegates and file/directory lifetime scopes; use the concrete transaction scopes for backend transactions.
- **`Tools.Sqlite`** / **`Tools.MSSQL`** — provider-specific helpers; the Firebird project exposes `FirebirdDAC`, not a `Tools.Firebird` facade.
- **`Tools.NUnit`** — test utilities (e.g., 2D array formatting).

`Tools.Windows` is the existing **static facade** whose thread-static properties
return instance utilities. Read the Windows-specific rules below before adding
a utility, and read [process Sentries](process-sentries.md) for executable wrappers.

To add a new tool, create a `static class` in the `Tools` namespace anywhere in the codebase — it becomes discoverable via `Tools.` intellisense automatically.

Use the exact existing facade names. `Tools.Expressions` is plural and exposes
`CreateExpression`; it has no `BuildPredicate` or `CombinePredicates` methods.
`Tools.Runtime.IsWebApp` currently returns `false`, and its release-build flag
depends on the `RELEASE` compilation symbol. Do not infer ASP.NET hosting or
general OS/version detection from those helpers.

Observe stream ownership flags: `ReadByteArray` defaults to closing its input,
and `WriteStreamToFile` defaults `closeReadStream` to `true`. Supply the relevant
`false` flag when the caller retains ownership of a reusable stream.

## Windows utility facade and source folders

Expose reusable Windows utilities through the central
[WindowsTool.cs](../../../src/Sphere10.Framework.Windows/WindowsTool.cs) facade.
Inspect the existing property/utility before extending it. Retain its established
pattern: a private ThreadStatic instance field and public property using lazy
initialization. Public utility methods are instance methods on that utility;
private stateless helpers may remain static. Consumers use `Tools.Windows.<Domain>`
rather than instantiate an otherwise parallel public helper or invent another
static discovery class. This facade does not change ownership of resources
returned by an operation.

Current additional domains include `Tools.Windows.Pipes` for passive local pipe
name discovery and `Tools.Windows.TraceSessions` for accessible native ETW session
identity/logfile queries. Their instance utilities live under Pipes and Tracing.
Client reads, capture attribution/retention, selected log output and recovery/exit
policy stay in the app. `PktMonSentry` wraps an executable rather than becoming
a native utility property; see [its command boundary](process-sentries.md).

Choose an appropriate domain property and extend the existing utility when it
already owns the responsibility. `Tools.Windows.Services` returns ServiceUtil
for native service resource operations. ServiceControlManagerSentry is a
ProcessSentry executable wrapper for sc.exe and must not absorb those methods.
New native utility abstractions use a Util name, not an Api name. Reuse the
existing native → resource utility → app layering; see
[Windows resource layers](framework-reuse.md#windows-abstraction-layers).

Put implementations under the owning domain folder: Registry, Services,
Security, Processes, Networking, Storage, Files, EventLogs or another existing
domain. Use Misc only for general helpers without a more appropriate domain.
Do not scatter naked helper files at the Windows project root; WindowsTool.cs
remains the central root facade. Physical folders need not introduce additional
public namespaces. Preserve type filenames and
[one-type source organization](../code-styling.md#type-source-files).

ThreadStatic caching means one utility instance per accessing thread, not a
process-wide singleton or a lease for returned handles. Acquire and dispose
thread-affine scopes on the actual worker thread, do not carry them across await,
and obey each returned resource's owner. A utility with mutable state must still
use the existing [synchronization contract](synchronization.md) where needed.

## Deferred utility inputs

For deferred, refreshable or projected inputs, inspect `Tools.Values.Future`
and read [the future contracts](futures.md). Its factories have distinct loading,
failure, concurrency and disposal semantics; a future is not a Task and does not
make a returned value immutable.

## Guard Pattern (Argument & Invariant Checking)

**Never** write `if (condition) throw new ArgumentException(...)` or similar inline throw patterns. Always use the `Guard` static class from `Sphere10.Framework`:

- **`Guard.ArgumentNotNull(value, nameof(value))`** — throws `ArgumentNullException`.
- **`Guard.Argument(condition, nameof(param), "message")`** — throws `ArgumentException` when `condition` is false.
- **`Guard.ArgumentNot(condition, nameof(param), "message")`** — throws when `condition` is true.
- **`Guard.ArgumentNotNullOrEmpty(str, nameof(str))`** — for strings and enumerables.
- **`Guard.ArgumentInRange(value, min, max, nameof(value))`** — throws `ArgumentOutOfRangeException`.
- **`Guard.ArgumentGTE / ArgumentLTE / ArgumentGT / ArgumentLT`** — relational argument checks.
- **`Guard.ArgumentEquals(value, expected, nameof(value))`** — exact value match.
- **`Guard.ArgumentCast<T>(obj, nameof(obj))`** — safe cast or throw.
- **`Guard.Ensure(condition, "message")`** — throws `InvalidOperationException` for internal invariants (not argument validation).
- **`Guard.Against(condition, "message")`** — inverse of `Ensure`, throws when condition is true.
- **`Guard.CheckIndex / Guard.CheckRange`** — collection bounds validation.
- **`Guard.FileExists / Guard.DirectoryExists`** — file-system pre-conditions.

Use `nameof(parameter)` for helpers that accept a parameter name. Path helpers
take only the path. `ArgumentInRange` includes both endpoints; `ArgumentEquals`
currently compares `long` values, rather than providing generic equality.
Guard checks preserve argument/invariant exception contracts; they do not
replace domain exceptions, cancellation or cleanup error propagation.

```csharp
// ✅ Correct
Guard.ArgumentNotNull(buffer, nameof(buffer));
Guard.Argument(buffer.Length >= MinSize, nameof(buffer), "Buffer too small");
Guard.Ensure(!IsDisposed, "Object has been disposed");

// ❌ Wrong — do not use inline throws
if (buffer == null) throw new ArgumentNullException(nameof(buffer));
if (buffer.Length < MinSize) throw new ArgumentException("Buffer too small");
```

## Cryptography

The framework provides its own crypto infrastructure in `Sphere10.Framework` (core) and `Sphere10.Framework.CryptoEx` (extended). **Prefer framework crypto over raw BCL crypto.**

- **Hashing** — use `Hashers.Hash(CHF.SHA2_256, data)` or other `CHF` enum values. `Hashers` pools registered `IHashFunction` implementations for concurrent hashing. Register algorithms before hashing starts; registry replacement is not coordinated with concurrent hashing. Never use `System.Security.Cryptography.SHA256.Create()` directly in framework consumers.
- **`CHF` enum** — canonical hash function identifiers: `SHA2_256`, `SHA2_512`, `Blake2b_256`, `Blake2b_128`, `SHA3_256`, etc. Enum membership does not guarantee an implementation is registered; extended algorithms require the owning module or an explicit registration. See the [crypto skill](../../../agents/skills/crypto/SKILL.md).
- **`Tools.Crypto`** — static tool for random bytes (`GenerateCryptographicallyRandomBytes`), password hashing, AES encryption, secure erase (`SecureErase`).
- **Digital signatures** — `IDigitalSignatureScheme` / `StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>` are in core `Sphere10.Framework`. Implementations: `Schnorr`, `ECDSA` (various curves), etc.
- **Key derivation** — seeded key generation must remain deterministic (same seed → same key). Preserve the chosen scheme's existing derivation; ECDSA/Schnorr use `DigestRandomGenerator` seeded exclusively with the provided seed. The non-generic contract exposes `CreatePrivateKey`; generic schemes expose `GeneratePrivateKey`. Parameterless generation uses system entropy.
- **IES** — `IIESAlgorithm` for integrated encryption schemes, accessed via `scheme.IES`.

`Tools.Crypto` supplies `PasswordHash`, `ValidatePassword`, `EncryptStringAES`
and `DecryptStringAES`; hashing algorithms are accessed through `Hashers`.
`SecureErase` overwrites the supplied array with random bytes; it does not erase
other copies of a secret. `Sign`/`Verify` work with messages and calculate the
scheme's digest; `SignDigest`/`VerifyDigest` accept an already calculated digest.
Use the chosen scheme's digest length and signer nonce contract.

## Dog-Fooding (Prefer Framework Over BCL)

**Always prefer Sphere10 Framework utilities over raw System/BCL equivalents** when a framework tool exists:

| Instead of (BCL) | Use (Framework) |
|---|---|
| `System.BitConverter` | `EndianBitConverter.Little` / `.Big` |
| `SHA256.Create().ComputeHash(...)` | `Hashers.Hash(CHF.SHA2_256, data)` |
| `new Random()` / `RandomNumberGenerator` | `Tools.Crypto.GenerateCryptographicallyRandomBytes(n)` |
| `File.WriteAllBytes` | `Tools.FileSystem.WriteAllBytes` |
| array concatenation, cloning or filling | `Tools.Array.Concat` / `.Clone` / `.Fill` |
| `string.Join`, `string.Format` (complex) | `Tools.Text.*` |
| `Enum.GetValues`, `Enum.Parse` | `Tools.Enums.GetValues<T>()` / `Tools.Enums.ParseEnum<T>(value, ignoreValueCase)` |
| `Activator.CreateInstance` (complex) | `TypeActivator.ActivateWithCompatibleArgs(type, args)` or `type.ActivateWithCompatibleArgs(args)` |
| `Console.WriteLine` (logging) | `SystemLog.Info(...)` |
| inline `if/throw` for args | `Guard.*` |
| ad-hoc `EqualityComparer<T>` | `ComparerFactory.Default.GetEqualityComparer<T>()` |

Check the actual method and its owning project before using a facade. Provider, Windows and NUnit tools require their corresponding project or package references. `Tools.FileSystem` has no `ReadAllBytes` method; do not invent that member. Activation belongs to `TypeActivator` and its `Type` extensions, rather than `Tools.Reflection`.

See [serialization](serialization.md) for endianness, [collections and storage](collections-and-storage.md#comparers--equality) for comparer registration, [logging](logging.md) for application and process logger selection, and [resource scopes](resource-scopes.md) for cleanup and synchronization.

Implementation references: [array utilities](../../../src/Sphere10.Framework/Collections/Arrays/ArrayTool.cs),
[collection utilities](../../../src/Sphere10.Framework/Collections/CollectionTool.cs),
[Guard](../../../src/Sphere10.Framework/Exceptions/Guard.cs),
[delegate utilities](../../../src/Sphere10.Framework/Functional/LambdaTool.cs),
[expression factories](../../../src/Sphere10.Framework/Functional/ExpressionTool.cs),
[runtime helpers](../../../src/Sphere10.Framework/Environment/RuntimeTool.cs),
[stream helpers](../../../src/Sphere10.Framework/Streams/StreamTool.cs),
[cleanup factories](../../../src/Sphere10.Framework/Scopes/ScopeTool.cs),
[Windows facade](../../../src/Sphere10.Framework.Windows/WindowsTool.cs),
[crypto helpers](../../../src/Sphere10.Framework/Crypto/CryptoTool.cs),
[hash pools](../../../src/Sphere10.Framework/Crypto/Hashing/Hashers.cs),
[signature contracts](../../../src/Sphere10.Framework/Crypto/IDigitalSignatureScheme.cs)
and [Firebird DAC](../../../src/Sphere10.Framework.Data.Firebird/FirebirdDAC.cs).
