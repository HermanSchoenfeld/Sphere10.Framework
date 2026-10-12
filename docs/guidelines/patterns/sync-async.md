# Synchronous and asynchronous implementation hierarchies

When an abstraction supports both synchronous and asynchronous execution, expose
both operations and choose the implementation direction through inheritance.
The chosen operation is the primitive: the concrete implementation supplies its
real work, and a directional base adapts the other API to that primitive.

The established architectural reference is
[`IRepository`](../../../src/Sphere10.Framework/Repository/IRepository.cs),
[`RepositoryBase`](../../../src/Sphere10.Framework/Repository/RepositoryBase.cs),
[`SyncRepositoryBase`](../../../src/Sphere10.Framework/Repository/SyncRepositoryBase.cs)
and [`AsyncRepositoryBase`](../../../src/Sphere10.Framework/Repository/AsyncRepositoryBase.cs).
Apply that structure to other paired APIs, including initializers, finalizers
and application lifecycle operations.

| Layer | Responsibility |
|---|---|
| `IFoo` | Declare both synchronous and asynchronous operations. |
| `FooBase` | Share invariants, state, events and common algorithms; leave both execution primitives abstract. |
| `SyncFooBase` | Leave the synchronous primitive abstract and implement the asynchronous adapter. |
| `AsyncFooBase` | Leave the asynchronous primitive abstract and implement the synchronous adapter. |
| Concrete implementation | Derive from the appropriate directional base and override its chosen primitive. |

A common base may implement derived operations in terms of other operations in
the same direction. For example, `RepositoryBase.Contains` delegates to `TryGet`,
and `ContainsAsync` delegates to `TryGetAsync`. Shared lifecycle orchestration
may also accept execution delegates while retaining a single owner for state,
coalescing and cleanup. The directional base still selects which primitive runs.

```csharp
using System.Threading;
using System.Threading.Tasks;

public interface IWork {
	void Execute();

	Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public abstract class WorkBase : IWork {
	public abstract void Execute();

	public abstract Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public abstract class SyncWorkBase : WorkBase {
	public override Task ExecuteAsync(CancellationToken cancellationToken = default)
		=> Task.Run(Execute, cancellationToken);
}

public abstract class AsyncWorkBase : WorkBase {
	public override void Execute()
		=> Task.Run(() => ExecuteAsync()).GetAwaiter().GetResult();
}
```

The synchronous implementation overrides only `Execute()`. The asynchronous
implementation overrides only `ExecuteAsync(...)` and returns a task representing
all its work. Callers can use either API through `IWork`. Reuse the existing
directional base for the abstraction instead of repeating adapters in concrete
classes or introducing a helper class to choose the execution direction.

## Adapter completion and cancellation

The sync-first asynchronous adapter returns the worker task. It must complete
only after the synchronous primitive finishes and must expose its failure.
Passing a token to `Task.Run` can prevent work that has not started; it does not
interrupt a synchronous primitive already running. Asynchronous primitives pass
cancellation to the operations they await according to the operation's policy;
committed application shutdown continues cleanup even if a caller stops waiting.

The async-first synchronous adapter must wait for the entire asynchronous
primitive and propagate its failure. In the example, the async factory is
evaluated inside `Task.Run`, so it starts without capturing the caller's
synchronization context. The same approach handles `Task<T>` results through
`GetAwaiter().GetResult()`. Running a wait on a task already created on the
caller does not undo that task's captured context.

Use the current
[`SyncApplicationInitializerBase`](../../../src/Sphere10.Framework.Application/Lifecycle/SyncApplicationInitializerBase.cs)
and [`AsyncApplicationInitializerBase`](../../../src/Sphere10.Framework.Application/Lifecycle/AsyncApplicationInitializerBase.cs)
as adapter examples;
[SyncApplicationFinalizerBase](../../../src/Sphere10.Framework.Application/Lifecycle/SyncApplicationFinalizerBase.cs)
and [AsyncApplicationFinalizerBase](../../../src/Sphere10.Framework.Application/Lifecycle/AsyncApplicationFinalizerBase.cs)
follow the same structure. Older
repository adapters use `WaitSafe` and `ResultSafe`. Verify their behavior
before reuse: the current `WaitSafe` in
[`TaskExtensions`](../../../src/Sphere10.Framework/Extensions/TaskExtensions.cs)
queues a wait without joining it, while `ResultSafe` receives an already-started
task. Neither is a replacement for the completion and context guarantees above.

Keep these rules explicit:

- Declare paired execution primitives in the interface and common base; put
  execution adapters in the directional bases.
- Do not select sync versus async execution with a boolean mode on the common
  base or silently make one direction a default there.
- Do not make both directions forward to each other; that creates recursion.
- Do not use `async void`, discard an operation's task, or return
  `Task.CompletedTask` while its asynchronous work remains pending.
- Keep notification hooks and lifecycle state in the shared layer. If a paired
  work hook selects execution direction, adapt it in the corresponding
  directional base too.

## Disposal and platform integration

Disposal follows the implementation's chosen direction. A sync-first base owns
synchronous cleanup; its asynchronous cleanup adapter calls
`FreeManagedResources()` inline and returns a completed `ValueTask`.
`SyncApplicationBase` returns `ValueTask.CompletedTask` explicitly;
`SyncRepositoryBase` currently uses an `async ValueTask` method with no await.
This disposal adapter does not require worker scheduling.
An async-first base owns awaited cleanup; its synchronous adapter waits for
`FreeManagedResourcesAsync()` to finish. When converting a `ValueTask` to a task
for that bridge, call `AsTask()` once inside the async factory.

[`SyncApplicationBase`](../../../src/Sphere10.Framework.Application/UI/SyncApplicationBase.cs)
and [`AsyncApplicationBase`](../../../src/Sphere10.Framework.Application/UI/AsyncApplicationBase.cs)
apply the pattern to initialization, finalization, exit and owned cleanup. Their
neutral `ApplicationBase` owns once-only state, notifications, pending-operation
coalescing and resource ownership. Adapters must preserve those semantics and
must not report success before tasks and cleanup finish. Application task
inventories remain protected implementation details, rather than public
interface properties. See the
[application lifecycle documentation](../../../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown)
for its cancellation and shutdown policies.

The exit pair and its `ConfirmExit`, `CanExit` and `OnExit` work hooks also leave
both directions abstract in the neutral application base. A sync-first exit
calls its boolean hooks and synchronous finalization; an async-first exit
awaits its task-returning hooks and finalization. Adapting the public exit pair
must happen in the directional bases, just as for initialization. Sharing a
pending-operation coordinator does not permit a neutral synchronous method to
forward to the asynchronous public operation.

Distinguish that execution choice from an async-only collaborator. Existing
screen-host and block permission contracts expose only asynchronous guards.
`SyncApplicationBase.CanExit` explicitly waits for those collaborators while
retaining synchronous confirmation, completion and finalization. It starts each
guard factory outside the caller's context and waits with its permission token,
so cancellation or force can release that wait even if the collaborator ignores
the token. It still uses
[ApplicationScreenHost.CanNavigateAsync](../../../src/Sphere10.Framework.Application/UI/ApplicationScreenHost.cs)
and its serialized guard over every retained screen;
replacing that check with dirty flags or checking only the active screen would
weaken the contract. Override the synchronous permission hook when supplying
genuinely synchronous permission work. A synchronous confirmation or task already
running cannot be interrupted merely by passing a token to its `Task.Run`
adapter; platform cancellation support must be explicit.

UI adapters must respect their dispatcher. A worker bridge does not make UI
operations safe on a worker, and blocking a dispatcher needed by a pending task
can deadlock. Prefer awaited calls on UI execution paths.
[`WinFormsApplication`](../../../src/Sphere10.Framework.Windows.Forms/Application/WinFormsApplication.cs)
starts lifecycle work on its owning UI thread, dispatches worker requests there,
and pumps UI messages at supported synchronous boundaries while waiting for the
same asynchronous operation. Tests for those boundaries must use a real
WinForms context and message loop. Blazor circuit lifecycle consumers await the
async-first application operations.

The native class derives from `AsyncApplicationBase`; its synchronous public
overrides wait for the same async operation through its own UI message pump.
This is the platform boundary required by its async implementation direction,
rather than a sync-first primitive. The shared
[direction tests](../../../tests/Sphere10.Framework.Application.Tests/UI/ApplicationLifecycleDirectionTests.cs)
and native [lifecycle tests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/WinFormsApplicationLifecycleTests.cs)
exercise those distinct contracts.

## Default interface projections

Default interface projections follow the separate
[interface bridge pattern](interface-bridges.md#default-interface-projections).

When extending a paired API, verify both public forms against a genuinely
pending operation, failure propagation, cancellation behavior and the chosen
disposal direction. Update implementations, decorators, registrations, demos
and documentation together so consumers use the intended primitive.
