# Patterns and Practices

Use existing Sphere10 abstractions and utilities before introducing another
implementation. Trace the real consumers and registration path, then extend
the layer that owns the behavior. Apply the [coding guide](code-styling.md)
and the matching [repository skills](../../.github/skills/README.md).

## Inheritance foundations and concrete products

A class whose purpose is to supply reusable behavior for subclasses must be
abstract and named `FooBase`. This applies even when every interface member
has an implementation: abstractness communicates that the class is a
foundation rather than a usable product. Generic and nongeneric variants
follow the same rule.

A concrete `Foo` provides usable behavior and has a genuine direct consumer.
Keep existing concrete types when applications construct or resolve them for
that behavior. When their subclassing role also needs a clear extension
contract, extract shared behavior into `FooBase` and retain `Foo` as the
concrete implementation. Do not make a foundation concrete solely because a
test constructs it; tests can declare a small concrete fixture subclass. Do
not add an unused concrete wrapper simply to complete a type hierarchy.

Interfaces describe contracts; abstract bases share invariants and extension
hooks; concrete classes supply usable implementations; decorators forward
the contract and add behavior. Use those layers when the abstraction needs
them. Immutable values, screen definitions and directly executable actions
remain ordinary concrete classes when inheritance is not their purpose.

```csharp
public abstract class MenuBase<TItem> : IApplicationMenu<TItem>
	where TItem : class, IApplicationMenuItem {
	// Shared item storage and extension hooks.
}

public sealed class NativeMenu : MenuBase<NativeMenuItem> {
	// Native presentation resources and directly usable menu behavior.
}
```

Before renaming a hierarchy, inspect direct construction, dependency-injection
registrations, inheritance, decorators, builders, examples and tests. Update
all affected consumers together. Avoid keeping a superseded implementation
active beside an unused replacement.

## Synchronous and asynchronous implementation hierarchies

When an abstraction supports both synchronous and asynchronous execution, expose
both operations and choose the implementation direction through inheritance.
The chosen operation is the primitive: the concrete implementation supplies its
real work, and a directional base adapts the other API to that primitive.

The established architectural reference is
[`IRepository`](../../src/Sphere10.Framework/Repository/IRepository.cs),
[`RepositoryBase`](../../src/Sphere10.Framework/Repository/RepositoryBase.cs),
[`SyncRepositoryBase`](../../src/Sphere10.Framework/Repository/SyncRepositoryBase.cs)
and [`AsyncRepositoryBase`](../../src/Sphere10.Framework/Repository/AsyncRepositoryBase.cs).
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

### Adapter completion and cancellation

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
[`SyncApplicationInitializerBase`](../../src/Sphere10.Framework.Application/Lifecycle/SyncApplicationInitializerBase.cs)
and [`AsyncApplicationInitializerBase`](../../src/Sphere10.Framework.Application/Lifecycle/AsyncApplicationInitializerBase.cs)
as adapter examples; the finalizer bases follow the same structure. Older
repository adapters use `WaitSafe` and `ResultSafe`. Verify their behavior
before reuse: the current `WaitSafe` in
[`TaskExtensions`](../../src/Sphere10.Framework/Extensions/TaskExtensions.cs)
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

### Disposal and platform integration

Disposal follows the implementation's chosen direction. A sync-first base owns
synchronous cleanup; its asynchronous cleanup adapter calls
`FreeManagedResources()` inline and returns `ValueTask.CompletedTask`, as in
`SyncRepositoryBase`. This disposal adapter does not require worker scheduling.
An async-first base owns awaited cleanup; its synchronous adapter waits for
`FreeManagedResourcesAsync()` to finish. When converting a `ValueTask` to a task
for that bridge, call `AsTask()` once inside the async factory.

[`SyncApplicationBase`](../../src/Sphere10.Framework.Application/UI/SyncApplicationBase.cs)
and [`AsyncApplicationBase`](../../src/Sphere10.Framework.Application/UI/AsyncApplicationBase.cs)
apply the pattern to initialization, finalization, exit and owned cleanup. Their
neutral `ApplicationBase` owns once-only state, notifications, pending-operation
coalescing and resource ownership. Adapters must preserve those semantics and
must not report success before tasks and cleanup finish. Application task
inventories remain protected implementation details, rather than public
interface properties. See the
[application lifecycle documentation](../../src/Sphere10.Framework.Application/README.md#application-initialization-and-shutdown)
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
the token. It still uses the host's serialized guard over every retained screen;
replacing that check with dirty flags or checking only the active screen would
weaken the contract. Override the synchronous permission hook when supplying
genuinely synchronous permission work. A synchronous confirmation or task already
running cannot be interrupted merely by passing a token to its `Task.Run`
adapter; platform cancellation support must be explicit.

UI adapters must respect their dispatcher. A worker bridge does not make UI
operations safe on a worker, and blocking a dispatcher needed by a pending task
can deadlock. Prefer awaited calls on UI execution paths.
[`WinFormsApplication`](../../src/Sphere10.Framework.Windows.Forms/Application/WinFormsApplication.cs)
starts lifecycle work on its owning UI thread, dispatches worker requests there,
and pumps UI messages at supported synchronous boundaries while waiting for the
same asynchronous operation. Tests for those boundaries must use a real
WinForms context and message loop. Blazor circuit lifecycle consumers await the
async-first application operations.

### Default interface projections

Execution-direction adapters are separate from default interface projections.
A specialized interface can expose its typed property through a base contract,
as `IApplicationPlugin<TBlock>` does for `Blocks`. Keep those projections inline
in the interface under `#region Base Interface Bridges`, without helper classes.
They project the same value; they do not select scheduling or lifecycle
execution. Do not put sync/async execution adapters into default interface
members.

When extending a paired API, verify both public forms against a genuinely
pending operation, failure propagation, cancellation behavior and the chosen
disposal direction. Update implementations, decorators, registrations, demos
and documentation together so consumers use the intended primitive.

## Ownership and platform extensions

Application task registration belongs on the plugin builder. Call `WithName`
before `RegisterInitializer` or `RegisterFinalizer`, including factory
overloads; an unnamed registration must throw immediately before changing
builder state. Register task services under the built plugin's unique `Name`,
and reject duplicate names when loading. No additional plugin ID is required.
Resolve tasks through the executing application's provider so scoped dependencies
and task instances belong to that application. Keep the inventory protected;
ordinary `ConfigureServices` callbacks continue to register other dependencies.
Reserve unkeyed tasks for the existing process lifecycle and `typeof(IApplication)`
keys for application-owned host tasks. Demonstrate these helpers in real plugins.

Keep common policies and contracts in the UI-independent application layer.
Platform adapters own presentation and platform-specific runtime details.
Use shared contracts for common behavior; inject a platform subtype only when
its extra members are required. Extend a shared type with an explicit platform
name rather than redeclaring the same name in another namespace.

Choose identity according to the abstraction. A host operating on actual
screen objects uses reference identity. A platform route or renderer may need
its own named identifiers; those identifiers should not become requirements
of an otherwise reference-based shared contract.

The running application exposes blocks, application commands, its screen host
and loaded startup plugins. Its runtime hierarchy is application → blocks →
menus and screen definitions; a plugin can contribute several blocks.
`IApplication.Blocks` projects the host's live registered membership, keeping
one membership owner. `LoadedPlugins` records actual successful startup
registrations rather than creating plugins for standalone blocks. Navigation
selection, screen state and view mode remain on the host. UI services provide
interactions and command execution. A single-screen contract and a
multiple-screen contract should expose the operations their consumers need
without requiring every host to provide tab collections and ordering.

Make runtime ownership explicit: a block's `Parent` is its running application,
a menu's `Parent` is its block, and an item's `Parent` is its menu. Startup
registration assigns the block's `Source` to the original plugin after the
complete capture is validated. Removing live membership clears its parent but
preserves startup provenance. Screen command execution follows its parent
chain instead of searching every block for a matching definition.

Screen definitions have immutable metadata and reverse `Activators` membership.
Owning menu/item mutations update parents and reverse membership together;
transfer an object by removing it from its existing owner first. A copied graph
clones each source definition once and uses that clone for its default, commands
and activators. Independent snapshots must not share mutable ownership links.
Metadata variants have no copied commands still targeting the source definition.
Validate complete definition collections before changing host state. The screen
host captures each retained instance's lifetime and owns its type-to-definition
index directly.

Commands retain their contribution level when composed. Application commands
remain available when a block or screen contributes a command with the same
caption or platform key. Within contextual contributions, screen commands
may replace matching block commands. Preserve the original command objects
and callbacks; composition must not rewrite reusable registrations with
per-render ownership state.

Assign a screen's `Host` and `Block` before lifecycle hooks, keep them while a
single instance is hidden, and clear them after actual removal cleanup or host
disposal. A rejected navigation guard leaves those links intact. Shared hosts
detach ownership; platform adapters own view and screen disposal.

A synchronous configuration property must not start an unobserved transition
or block on asynchronous lifecycle work. Runtime changes that can deactivate
screens must await the host's guarded transition API. Make any restriction on
the synchronous setter explicit and leave the current state intact when it
cannot perform the requested change safely.

Expose array snapshots for collection-valued data properties. A defensive
membership copy retains the element references; it does not imply deep
cloning or ownership of those elements. Make resource ownership explicit,
and use the framework's disposable scopes for cleanup and state transitions.
