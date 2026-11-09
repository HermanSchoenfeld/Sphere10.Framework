# Values

Value types and helpers for deferred values, operation results, variable-length integers, GUID generation, bit manipulation, value ranges and the `Tools.Time`, `Tools.Enums` and `Tools.Values` utilities.

`IFuture<T>` is a value that becomes available later, read through `Value`. `LazyLoad<T>` fetches once on first read, `Reloadable<T>` fetches lazily under a lock and refetches after `Invalidate()`, `ProxyValue<T>` fetches on every read, `ExplicitFuture<T>` waits for a caller to set it once, and `BackgroundFetchedValue<T>` starts its fetch on the thread pool at construction and blocks readers until it completes. `Tools.Values.Future` creates each kind, and `IFutureExtensions.AsProjection` maps one future onto another. `LazyLoad<T>` is not synchronized, so concurrent first reads can run its loader more than once. `Reloadable<T>.Invalidate()` disposes a loaded value that is an `IDisposable` or an `IDisposable[]`.

`Result` collects result codes with a severity and either a message or an enum value, and `Result<TValue>` adds a value. `IsFailure` is true when any code has `LogLevel.Error`, and `ThrowOnFailure()` throws `InvalidOperationException` with the error messages. The implicit conversion from `Result<TValue>` to `TValue` returns `Value` without checking for failure.

`VarInt` and `CVarInt` encode unsigned integers in as few bytes as possible. `VarInt` stores values below `0xFD` in one byte and larger values after a one-byte size marker, and `CVarInt` uses LEB128-style compact encoding. `ShortGuid` renders a GUID as a base62 string, and the GUID generators produce cryptographically random GUIDs, pre-generated ascending batches or hash-chained deterministic sequences. `Bits` reads, writes and copies bit ranges in byte spans, and `ValueRange<T>` is an immutable range with inclusive or exclusive ends. Serializers for these values live in [Serialization](../Serialization/README.md), and numeric algorithms live in [Maths](../Maths/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BackgroundFetchedValue<T>`](BackgroundFetchedValue.cs) | class | Future whose loader runs on the thread pool at construction, with `Value` blocking until the loader finishes and rethrowing its error in an `AggregateException`. |
| [`Bits`](Bits.cs) | class | Static bit helpers that read, write, copy, set and rotate bits in byte spans in either iteration direction. |
| [`CryptographicallySecureGUIDGenerator`](CryptographicallySecureGUIDGenerator.cs) | class | Creates RFC 4122 version 4 GUIDs from cryptographically random bytes. |
| [`CVarInt`](CVarInt.cs) | struct | Compact LEB128-style variable-length unsigned integer with size calculation, span and stream reading and writing, and arithmetic operators. |
| [`DeterministicSequentialGuidGenerator`](DeterministicSequentialGuidGenerator.cs) | class | Produces a repeatable GUID sequence by repeatedly hashing a seed and taking the first 16 bytes of each digest. |
| [`ExplicitFuture<T>`](ExplicitFuture.cs) | class | Future whose value the caller sets exactly once, and which throws when read before it is set. |
| [`IFuture<T>`](IFuture.cs) | interface | A value that becomes available later, read through `Value`. |
| [`IFutureExtensions`](IFuture.cs) | static class | `AsProjection`, which maps a future's value through a function on every read. |
| [`ISequentialGuidGenerator`](ISequentialGuidGenerator.cs) | interface | Contract for generators that return the next GUID of a sequence. |
| [`IterateDirection`](IterateDirection.cs) | enum | Left-to-right or right-to-left traversal order used by the bit helpers. |
| [`LazyLoad<T>`](LazyLoad.cs) | class | Unsynchronized future that runs its loader on the first read and keeps the result. |
| [`PreGenSequentialGuidGenerator`](PreGenSequentialGuidGenerator.cs) | class | Pre-generates a batch of random GUIDs and returns them in ascending order, regenerating or throwing when the batch is used up. |
| [`ProxyValue<T>`](ProxyValue.cs) | class | Future that runs its loader on every read. |
| [`Reloadable<T>`](Reloadable.cs) | class | Synchronized lazy future that can be invalidated, disposing a disposable value, so the next read fetches again. |
| [`Result`](Result.cs) | class | Serializable operation outcome holding result codes with severities and messages or enum values, with success, failure and merge helpers. |
| [`Result<TValue>`](Result.cs) | class | `Result` that also carries a value, with implicit conversions to and from the value. |
| [`ShortGuid`](ShortGuid.cs) | struct | GUID wrapper whose string form is the base62 encoding of its bytes. |
| [`Tools.Enums`](EnumTool.cs) | tools | Enum helpers for name candidates, `EnumMember`, description and display-name attributes, value lists, range checks and parsing. |
| [`Tools.Time`](DateTimeTool.cs) | tools | Date helpers for year and month differences, next day of week or month, Unix and 1979-based timestamps, ordinal suffixes, month names and minimum or maximum dates. |
| [`Tools.Values`](ValueTool.cs) | tools | Value helpers for clipping, swapping, base-N digits, minimum and maximum, double formatting and parsing, and the `Future` factory for each future type. |
| [`ValueRange<T>`](ValueRange.cs) | class | Immutable range with inclusive or exclusive ends, a comparer, `Contains` and step iterators. |
| [`VarInt`](VarInt.cs) | struct | Variable-length unsigned integer stored in one byte below `0xFD` and after a `0xFD`, `0xFE` or `0xFF` size marker otherwise, with span and stream reading and writing. |
