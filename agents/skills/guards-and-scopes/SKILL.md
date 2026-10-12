---
name: guards-and-scopes
description: Validate with Guard.* (never inline if/throw) and manage resources/state with disposable scope objects. Trigger on any argument validation, invariant check, or resource/state cleanup.
---

# Guards & Scopes Skill

## Guard (never inline throw)
Always use `Guard` from `Sphere10.Framework` (`src/Sphere10.Framework/Exceptions/Guard.cs`):

```csharp
// ✅ Correct
Guard.ArgumentNotNull(buffer, nameof(buffer));
Guard.Argument(buffer.Length >= MinSize, nameof(buffer), "Buffer too small");
Guard.ArgumentLTE(digest.Length, 32, nameof(digest), "Must be 32 bytes");
Guard.Ensure(!IsDisposed, "Object has been disposed");

// ❌ Wrong
if (buffer == null) throw new ArgumentNullException(nameof(buffer));
```

- Arguments: `ArgumentNotNull`, `Argument`, `ArgumentNot`, `ArgumentNotNullOrEmpty`, `ArgumentInRange`, `ArgumentGTE/GT/LTE/LT`, `ArgumentEquals`, `ArgumentCast<T>`.
- Invariants: `Ensure(condition, msg)` / `Against(condition, msg)` → `InvalidOperationException`.
- Collections: `CheckIndex` / `CheckRange`. File system: `FileExists` / `DirectoryExists`.
- Always pass `nameof(param)`, never string literals.

## Scopes (never bare try/finally when a scope exists)
Wrap acquired resources, entered states, and guaranteed cleanup in `using` scopes:

- Synchronization: `SynchronizedObject.EnterReadScope()` / `EnterWriteScope()`:
  ```csharp
  using (collection.EnterWriteScope()) {
	  collection.Add(item);
  }
  ```
- Access: `ICriticalObject.EnterAccessScope()` for `ObjectSpace`, `ClusteredStreams`, stream-mapped collections.
- Ad-hoc cleanup: `Tools.Scope.ExecuteOnDispose(action)`, `Tools.Scope.DeleteFileOnDispose(path)`.
- Transactions: `TransactionalScopeBase` for commit/rollback (database DACs, `ClusteredStreams`).
- Lightweight: `ActionScope` / `TaskScope` execute a delegate on disposal.

Principle: if an operation acquires a resource, enters a state, or needs guaranteed cleanup → scope object.
