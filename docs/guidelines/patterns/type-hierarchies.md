# Type Hierarchies

## Inheritance foundations and concrete products

For WinForms visual types, first read the
[designer practice](winforms-designer.md): their concrete nongeneric designer
surface is the intentional exception to the abstract foundation rule below.
Nonvisual services/controllers retain the ordinary hierarchy rules.

A class whose purpose is to supply reusable behavior for subclasses must be
abstract and named `FooBase`. This applies even when every interface member
has an implementation: abstractness communicates that the class is a
foundation rather than a usable product. Generic and nongeneric variants
follow the same rule.

The existing disposal template lineage retains the names `Disposable`, `SyncDisposable` and `AsyncDisposable`; the directional names identify which managed-cleanup primitive the subclass supplies. See [class owners](resource-scopes.md#class-owners).

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

A capability that extends an existing abstraction (refresh, cadence,
synchronization, observation) is a decorator over the contract, not a new
inheritance line. Concrete storage stays minimal and holds data; a composition
assembles the usable product from storage and decorators. Decorators are
direction-agnostic: they add the members their feature needs and call the matching
direction of their inner object; only the leaf adapts one direction to the other,
through a `Sync*` or `Async*` lineage, and every `Sync*` type has an `Async*` dual.
A base meant only for derivation is an abstract template with protected hooks
(`FooBase`); a client that composes without deriving uses an `Action*` variant.
The data-source family (`ListDataSource`/`AsyncListDataSource` storage,
`RefreshableDataSourceBase` and `PollingDataSource` decorators) is the reference; see
[the three kinds of data source](data-sources.md#three-kinds-of-data-source).

For a current example, [IApplicationMenu](../../../src/Sphere10.Framework.Application/UI/IApplicationMenu.cs)
defines the contract, [ApplicationMenuBase](../../../src/Sphere10.Framework.Application/UI/ApplicationMenuBase.cs)
implements item storage and ownership, and
[ApplicationMenu](../../../src/Sphere10.Framework.Windows.Forms/Application/ApplicationMenu.cs)
adds native presentation metadata and a directly constructible product:

```csharp
using SharedUI = Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

using var menu = new ApplicationMenu("Operations");
SharedUI.IApplicationMenu contract = menu;
```

The base is abstract even though it implements every required menu member.
The concrete menu is also consumed by
[ApplicationMenuBuilder](../../../src/Sphere10.Framework.Windows.Forms/Application/ApplicationMenuBuilder.cs).
It does not need an additional empty concrete wrapper or a custom hierarchy to
make that contract usable.

Before renaming a hierarchy, inspect direct construction, dependency-injection
registrations, inheritance, decorators, builders, examples and tests. Update
all affected consumers together. Avoid keeping a superseded implementation
active beside an unused replacement.

## Event notifications and template hooks

### Event contracts and payloads

Declare owned Framework and application events with `EventHandlerEx` or its
generic forms. Never declare an owned event with `EventHandler` or
`EventHandler<TEventArgs>`. Apply this convention across the relevant interfaces,
implementations, forwarding accessors, subscribers, demos and fixtures.

The existing [delegate family](../../../src/Sphere10.Framework/Events/EventHandlerEx.cs)
returns `void` and accepts zero through five meaningful arguments. It has no
implicit sender or `EventArgs` requirement:

| Contract | Invocation | Handler shape |
|---|---|---|
| `EventHandlerEx` | `Changed?.Invoke()` | `() => Refresh()` |
| `EventHandlerEx<T>` | `Changed?.Invoke(value)` | `value => Apply(value)` |
| `EventHandlerEx<T1, T2>` | `Changed?.Invoke(first, second)` | `(first, second) => Apply(first, second)` |

The three-, four- and five-argument forms follow the same call shape. Use a typed
source argument when it is part of the contract, such as
`EventHandlerEx<VisualGrid>` for a grid notification. Do not mechanically retain
`object sender, EventArgs.Empty` or invent another delegate type. Reuse meaningful
existing payload types, including mutable cancellation payloads whose identity
is part of the operation. Do not introduce an empty `EventArgs` subclass merely
to carry the old signature convention.

External .NET events, interface implementations and overrides retain signatures
required by their contracts. Subscribing to a WinForms event still uses its native
handler shape; overriding a platform `On*` method still uses the platform's
arguments. This boundary does not apply to a separate event owned by the product
or Framework. Platform callback types such as Blazor `EventCallback` also retain
their framework contract.

### Notification ownership

An inheritable event-owning class exposes each notification through three matching
members: a public event, a protected nonvirtual `Notify*` method, and a protected
virtual `On*` template method. Operations call `Notify*`; that method calls the
template hook and then invokes the event exactly once with the same arguments.
The hook defaults to an empty implementation when there is no required behavior.
Subclasses override `On*` instead of raising the event or replacing its dispatch.

**When this pattern is applied to one event, apply it to every event in the
class or event family.** Do not leave another event without its hook, invoke a
hook directly from an operation, or raise an event separately from its notifier.
Declare contract events on the corresponding interface and preserve them through
decorators and projections. Existing generic hooks whose signatures already fit
a specialization can be inherited; avoid redundant forwarding overrides.

For example, these members belong inside an event-owning base class:

```csharp
public event EventHandlerEx<long> ItemRemoving;

protected void NotifyItemRemoving(long key) {
	OnItemRemoving(key);
	ItemRemoving?.Invoke(key);
}

protected virtual void OnItemRemoving(long key) {
}
```

Sealed and static owners keep a single notifier without inventing unusable
virtual hooks or a wrapper class. A forwarding decorator uses matching direct
`add`/`remove` accessors instead of owning a second notification stream:

```csharp
public event EventHandlerEx Changed { add => InternalDataSource.Changed += value; remove => InternalDataSource.Changed -= value; }
```

Preserve the notification's lifecycle boundary, argument identity, exception
propagation and synchronization. A pre-removal notification runs while the entry
is still present and accounted for; commit follows successful completion of both
the hook and event. Reentrancy protection must cover the entire notifier, including
event subscribers. A post-removal notification observes committed removal.
Changing event ordering or moving a precondition after a callback requires its
own behavioral justification and tests.

[CacheBase<TKey, TValue>](../../../src/Sphere10.Framework/Cache/CacheBaseT.cs)
uses the pattern for `ItemFetching`, `ItemFetched`, `ItemRemoving` and `ItemRemoved`.
Verify each family member, hook/event ordering, failure and retry behavior, and
forwarding through its public contract rather than testing only the newly added event.

Ordinary synchronous multicast invocation propagates a subscriber exception and
does not continue through later subscribers. Some owners deliberately differ:
[DataSourceBase<T>](../../../src/Sphere10.Framework/DataSource/DataSourceBase.cs)
isolates and logs hook and subscriber failures after committed source changes.
Preserve the actual owner's policy; a delegate migration does not justify adding
catch-and-continue handling, changing lock boundaries or dispatching callbacks
on a different thread.

### Subscription lifetime and asynchronous consumers

The subscribing component owns registration and removal. Retain the same delegate
instance for both; `+= value => Handle(value)` and a newly constructed lambda in
`-=` do not remove each other. Use existing [disposable owners and scopes](resource-scopes.md)
for temporary subscriptions and detach persistent subscriptions at the actual
owner's replacement or teardown boundary. Direct forwarding accessors do not need
a new adapter or subscription wrapper.

Removing a handler does not cancel a callback already in progress. Preserve
callback admission, UI affinity and any existing drain contract through shutdown.
Read [application and screen lifetimes](application-ownership.md#application-and-screen-lifetimes)
when a subscription presents worker data in a view. Do not hold a resource lock
across borrowed callbacks that can synchronously dispatch back to its owner.

`EventHandlerEx` is synchronous. An `async` lambda assigned to it is `async void`;
the publisher cannot await completion or observe failures after suspension through
the invocation. Use an existing task-returning owner operation when work must be
awaited. A required native `async void` event boundary must observe its asynchronous
work and handle failures through the owner's established policy.

The current [InvokeAsync extensions](../../../src/Sphere10.Framework/Events/EventHandlerExExtensions.cs)
return `void` and delegate to `Task.Factory.StartNew` through
[Tools.Threads](../../../src/Sphere10.Framework/Threading/ThreadTool.cs). They provide
neither a completion task nor a UI-dispatch or shutdown-drain guarantee. Do not
substitute them for synchronous notifications or awaited operations.

The current `WaitNext` extensions modify a local delegate copy rather than
subscribing to the publisher's event. They are not a working event-wait boundary.
When completion really comes from an event, subscribe to the actual publisher
and own both handler removal and cancellation registration, including every
success, cancellation and failure path. Do not invent a parallel event framework
to avoid the existing operation or lifetime owner.
