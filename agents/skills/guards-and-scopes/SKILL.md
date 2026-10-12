---
name: guards-and-scopes
description: Validate arguments and invariants with Guard.*, and use existing disposable scopes for owned resource cleanup and state transitions. Trigger on validation, synchronization or resource cleanup work.
---

# Guards & Scopes Skill

Read [framework utility guidance](../../../docs/guidelines/patterns/framework-utilities.md) for argument and invariant validation, and [resource scope guidance](../../../docs/guidelines/patterns/resource-scopes.md) for owned acquire/release work.

## Argument and invariant guards
Use [Guard](../../../src/Sphere10.Framework/Exceptions/Guard.cs) for argument and invariant checks instead of repeating equivalent `if`/`throw` guards. Choose the helper whose predicate and exception contract match the check:

```csharp
// ✅ Correct
Guard.ArgumentNotNull(buffer, nameof(buffer));
Guard.Argument(buffer.Length >= minimumSize, nameof(buffer), "Buffer too small");
Guard.ArgumentNotNull(digest, nameof(digest));
Guard.ArgumentEquals(digest.Length, 32, nameof(digest), "Must be 32 bytes");
Guard.Ensure(!isDisposed, "Object has been disposed");

// ❌ Wrong
if (buffer == null) throw new ArgumentNullException(nameof(buffer));
```

- Arguments: `ArgumentNotNull`, `Argument`, `ArgumentNot`, `ArgumentNotNullOrEmpty`, `ArgumentNotNullOrWhitespace`, `ArgumentInRange`, `ArgumentGTE/GT/LTE/LT`, `ArgumentEquals`, `ArgumentCast<T>`.
- Invariants: `Ensure(condition, msg)` / `Against(condition, msg)` → `InvalidOperationException`.
- Collections: `CheckIndex` / `CheckRange`. File system: `FileExists` / `DirectoryExists`.
- Pass `nameof(parameter)` to helpers accepting a parameter name; path helpers such as `FileExists(path)` have no parameter-name argument.
- Guard nulls before dereferencing, and validate all required arguments before changing owned state. Preserve optional values allowed by the API; do not add artificial required configuration.
- Guard helpers do not replace domain-specific exceptions, cancellation, rethrows or aggregate cleanup failures. Preserve the operation's error semantics. A boolean failure returned by validation is not necessarily an exceptional condition.

## Existing resource and state scopes
Wrap acquired resources, entered states and guaranteed cleanup in existing `using` scopes instead of reproducing their acquire/release logic:

- Synchronization: `SynchronizedObject.EnterReadScope()` / `EnterWriteScope()`.
- Access: `ICriticalObject.EnterAccessScope()` for `ObjectSpace`, `ClusteredStreams`, stream-mapped collections.
- Ad-hoc cleanup: `Tools.Scope.ExecuteOnDispose(action)`, `Tools.Scope.DeleteFileOnDispose(path)`.
- Transactions: reuse the concrete scope for the backend. `TransactionalScopeBase<TTransaction>` is an abstract extension foundation, not directly constructible; its close hook alone does not guarantee automatic rollback.
- Lightweight: `ActionScope` / `TaskScope` execute a delegate on disposal.

```csharp
using (collection.EnterWriteScope()) {
	collection.Add(item);
}
```

Read [disposable-scopes](../disposable-scopes/SKILL.md) for actual factories, transaction ownership and async completion. Reader/writer and monitor scopes are thread-affine: dispose them on the acquiring thread and do not hold them across asynchronous suspension. Use `await using` for asynchronous cleanup. A scope owns its exit action; it does not imply ownership of every object used inside it.
