# Framework Utilities

Use the existing framework utility that covers the required semantics. Read the [Tools reference](../../tools-reference.md) and [resource-scope practice](resource-scopes.md) for the selected domain. The [framework reuse guide](framework-reuse.md) explains adoption of existing framework code.

## Tools namespace for client utilities

Public utility facades intended for Framework clients live in the **`Tools`** global namespace so consumers can discover them via `Tools.` intellisense. Prefer an existing `Tools.*` API over raw BCL calls when its contract fits. Internal validation and implementation helpers belong to their owning domain, not this public discovery surface. Key tools include:

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
- **`Tools.Scope`** — typed scope activation for synchronization, access, stream position, paging, hashers, callbacks and file/directory lifetimes; use the concrete transaction scopes for backend transactions. Platform assemblies add C# 14 static extension factories; see [scope factories](resource-scopes.md#toolsscope-factories).
- **`Tools.Exceptions`** — execution with captured failures, selected exception handling and intentional best-effort operations.
- **`Tools.Sqlite`** / **`Tools.MSSQL`** — provider-specific helpers; the Firebird project exposes `FirebirdDAC`, not a `Tools.Firebird` facade.
- **`Tools.NUnit`** — test utilities (e.g., 2D array formatting).

Utilities are static classes with static methods and no instance state. A type
that needs state, a lifetime, substitution or a hierarchy is a service, a scope or
a data source and is named and designed as one. `Tools.Windows` is a static
partial facade whose domain utilities are nested static classes. Read the
Windows-specific rules below before adding a utility, and read
[process Sentries](process-sentries.md) for executable wrappers.

Before adding a tool, identify its actual client-facing contract and owning project/domain. A public facade must have a coherent domain name; Application/UI utilities would belong under an application/UI facade rather than an unrelated root `Tools.UI`. Do not export internal methods merely because several Framework assemblies use them.

Keep internal implementation work in an internal domain helper, such as `ApplicationHelper` in `Sphere10.Framework.Application.UI`. Clients use the owning builder, host, settings object or editor control. Platform libraries may share internal implementation through the existing Framework assembly boundaries; do not grant product assemblies friend access to bypass that boundary.

Use the exact existing facade names. `Tools.Expressions` is plural and exposes
`CreateExpression`; it has no `BuildPredicate` or `CombinePredicates` methods.
`Tools.Runtime.IsWebApp` currently returns `false`, and its release-build flag
depends on the `RELEASE` compilation symbol. Do not infer ASP.NET hosting or
general OS/version detection from those helpers.

Observe stream ownership flags: `ReadByteArray` defaults to closing its input,
and `WriteStreamToFile` defaults `closeReadStream` to `true`. Supply the relevant
`false` flag when the caller retains ownership of a reusable stream.

## Exception execution and capture

Use `Tools.Exceptions.Execute*` when a failure is deliberately captured as data,
stored on an inventory row, translated to an availability message, reported to a
callback, or ignored by a best-effort operation. Do not repeat an ad-hoc
`try/catch` whose only purpose is that capture or handling. Prefer a nonthrowing
API such as `TryParse` when one already represents the expected result.

Choose the helper for the actual failure contract:

- `ExecuteCapturingException(action, out error)` captures an exception or assigns
  `null` on success. Its collection overload appends failures for later reporting.
- `ExecuteCapturingException<TException>(action, out error, exceptionFilter)` catches
  only that exception type and its subtypes. The optional filter further narrows
  the match; other exceptions propagate with their original identity and stack.
  The broad overload also accepts an explicit filter.
- `await ExecuteCapturingExceptionAsync(action, exceptionFilter)` returns the
  captured exception or `null`. A typed overload narrows the exception type.
  Always await it; never pass an async lambda to a synchronous `Action` helper.
- `Execute(action, handler)` returns success and invokes the handler on failure;
  a handler failure propagates. `ExecuteWithExceptionHandler` deliberately
  suppresses handler failures, so use it only when that best-effort behavior is
  intended.
- `Execute(argument, static value => ..., static (value, error) => ...)` applies the same policy to a prefetched argument. Use it on hot cleanup paths to keep the ordered body and error handler without creating capturing delegates per call.
- `ExecuteIgnoringException(action)` is for an explicitly optional operation
  whose failure must not replace the primary result. `TryOrDefault` is available
  when deliberately discarding the error and returning a fallback value.

Preserve the original exception type/filter, partial assignments, fallback value,
notification behavior and thread ownership. Broad capture helpers include
`OperationCanceledException`; exclude it explicitly when cancellation belongs to
the caller or the owning lifecycle. The async helper awaits the supplied work;
it does not establish a background-worker boundary. Native resource work still
needs its existing worker dispatch.

Complete an operation's externally observed task only after its cleanup scopes
have restored command state and released ownership. Do not signal completion
inside a captured delegate while that cleanup remains outside the async helper.

```csharp
Tools.Exceptions.ExecuteCapturingException<NetworkInformationException>(() => {
	row.PhysicalAddress = adapter.GetPhysicalAddress().GetAddressBytes();
	row.LinkSpeedBitsPerSecond = adapter.Speed;
}, out var hardwareError);
row.HardwareError = hardwareError;

var captureError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
	readings = await collector.CaptureAsync(cancellationToken);
}, error => error is not OperationCanceledException);
if (captureError != null)
	status = captureError.Message;
```

Keep an explicit `try/catch` where it implements a real recovery, retry,
rollback/rethrow, task-completion bridge or cancellation-ownership protocol that
cannot be expressed by the helper without changing control flow. Use the
[resource-scope practice](resource-scopes.md) for cleanup, and keep application
policy at its owning boundary. A ref-like input that cannot be captured by a
delegate can also require a narrow local handler; do not copy it or alter its
lifetime solely to fit a wrapper. Do not broaden a narrow catch or turn cancellation
into a successful empty inventory merely to shorten the code.

For cleanup owners, keep the normal logical sequence visible in one ordered body and use `Tools.Exceptions.Execute*` or `Tools.Lambda.Action*` for the matching error policy. For example, `ClusteredStream.Close` persists the descriptor, releases its owner, runs registered finalizers and closes the underlying stream in that order; its error handler restores retry admission and rethrows the original exception. Do not scatter defensive state transitions around each step or suppress cleanup failures to simplify the code. Apply this review consistently to every cleanup change in the family. Retain narrow acquisition-transfer and native/async `finally` handlers where they carry real ownership or where a delegate wrapper would add allocation to a hot path without clarifying the flow.

## Windows utility facade and source folders

Expose reusable Windows utilities through the central
[WindowsTool.cs](../../../src/Sphere10.Framework.Windows/WindowsTool.cs) facade.
Inspect the existing utility before extending it. Each domain utility is a
static class nested in the partial `Tools.Windows` class and declared in its
domain folder, for example `Tools.Windows.Files` in `Files/FileUtil.cs`:

```csharp
namespace Tools;

public static partial class Windows {

	public static class Files {
		public static FileStream OpenAlternateStream(string filePath, string streamName, FileMode mode, FileAccess access)
			=> WindowsFile.OpenAlternateStream(filePath, streamName, mode, access);
	}
}
```

Every member is static. A utility has no instance fields, no virtual members and
no subclasses, and it is never passed as a parameter or registered as a service.
Consumers call `Tools.Windows.<Domain>.<Method>()`. Code inside these files is in
the `Tools` namespace, where `Array`, `Environment` and `Registry` name Framework
tools or sibling utilities, so write `System.Array`, `System.Environment` and
`Microsoft.Win32.Registry` there. The facade does not change ownership of
resources returned by an operation.

Four utilities own state and remain per-thread instances returned by properties
in `WindowsTool.cs`: `Tools.Windows.Assemblies` (a metadata cache),
`Tools.Windows.Registry` (an access lock), `Tools.Windows.UiAutomation` and
`Tools.Windows.Dumps` (each a worker host). Do not add another.

Current additional domains include `Tools.Windows.Pipes` for passive local pipe
name discovery and `Tools.Windows.TraceSessions` for accessible native ETW session
identity/logfile queries. Their utilities live under Pipes and Tracing.
Client reads, capture attribution/retention, selected log output and recovery/exit
policy stay in the app. `PktMonSentry` wraps an executable rather than becoming
a native utility property; see [its command boundary](process-sentries.md).

Choose an appropriate domain property and extend the existing utility when it
already owns the responsibility. `Tools.Windows.Services` holds the native
service resource operations. ServiceControlManagerSentry is a
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

A static utility is not a lease for returned handles. Acquire and dispose
thread-affine scopes on the actual worker thread, do not carry them across await,
and obey each returned resource's owner. A utility with mutable state must still
use the existing [synchronization contract](synchronization.md) where needed.

### Discovery of resource owners and reusable widgets

When adding a reusable client operation, resource class or widget, include its
coherent `Tools` discovery route. A thin factory can return the existing concrete
class directly. Add it to the existing owning domain rather than reimplementing
the class's behavior. Keep constructor arguments, defaults, validation, cancellation,
thread dispatch and ownership unchanged.

Return a fresh mutable
data source, control, hook, suspension owner, session or disposable resource from
an explicit factory; never store these in a thread-static or global Tools field.
Discovery must not start polling, native acquisition or UI construction. Resource
constructors retain their actual behavior, including any existing monitor/timer
ownership. The caller still registers application/screen finalization at its
existing owner. Do not make a factory into a second service locator.

Native widget factories are discovered through `Tools.WinForms`, a static partial
class in the Windows Forms assembly. Its nested static classes `Windowing`,
`Charts`, `Logging`, `Grids`, `Review` and `Controls` create existing types on the
caller's UI thread. It is a separate class from `Tools.Windows` because a partial
class cannot span two assemblies. Keep these implementations in their
owning Windows Forms domain folders. Do not duplicate the native `Tools.Windows`
type or add a Windows-to-Windows-Forms project dependency to create this branch.
Designer-owned controls retain normal native constructor serialization. Scope
acquisition remains on `Tools.Scope`; builders remain the configuration API.
DTOs, internal validators, private readers and application policy do not need
Tools aliases. Document the route in the Tools reference and use it in an actual
consumer while retaining behavioral and ownership checks.

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

Use `ObjectDisposedException.ThrowIf(isDisposed, this)` for use-after-disposal
checks instead of a manual throw or `Guard.Ensure`. Use the string overload when
a deliberate fixed object name is required. Preserve argument-validation,
cancellation and cleanup contracts; ordinary repeated-disposal returns remain
returns.

```csharp
// ✅ Correct
Guard.ArgumentNotNull(buffer, nameof(buffer));
Guard.Argument(buffer.Length >= MinSize, nameof(buffer), "Buffer too small");
ObjectDisposedException.ThrowIf(IsDisposed, this);

// ❌ Wrong — do not use inline throws
if (buffer == null) throw new ArgumentNullException(nameof(buffer));
if (buffer.Length < MinSize) throw new ArgumentException("Buffer too small");
```

## Cryptography

The framework provides its own crypto infrastructure in `Sphere10.Framework` (core) and `Sphere10.Framework.CryptoEx` (extended). **Prefer framework crypto over raw BCL crypto.**

- **Hashing** — use `Hashers.Hash(CHF.SHA2_256, data)` or other `CHF` enum values. `Hashers` pools registered `IHashFunction` implementations for concurrent hashing. Register algorithms before hashing starts; registry replacement is not coordinated with concurrent hashing. Never use `System.Security.Cryptography.SHA256.Create()` directly in framework consumers.
- **`CHF` enum** — canonical hash function identifiers: `SHA2_256`, `SHA2_512`, `Blake2b_256`, `Blake2b_128`, `SHA3_256`, etc. Enum membership does not guarantee an implementation is registered; extended algorithms require the owning module or an explicit registration. See the [Tools reference](../../tools-reference.md).
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
| ad-hoc capture/ignore/report-only `try/catch` | `Tools.Exceptions.Execute*` |
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
