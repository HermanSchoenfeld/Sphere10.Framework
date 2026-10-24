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

For a current example, [IApplicationMenu](../../../src/Sphere10.Framework.Application/UI/IApplicationMenu.cs)
defines the contract, [ApplicationMenuBase](../../../src/Sphere10.Framework.Application/UI/ApplicationMenuBase.cs)
implements item storage and ownership, and
[WinFormsApplicationMenu](../../../src/Sphere10.Framework.Windows.Forms/Application/WinFormsApplicationMenu.cs)
adds native presentation metadata and a directly constructible product:

```csharp
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

using var menu = new WinFormsApplicationMenu("Operations");
IApplicationMenu contract = menu;
```

The base is abstract even though it implements every required menu member.
The concrete menu is also consumed by
[WinFormsApplicationMenuBuilder](../../../src/Sphere10.Framework.Windows.Forms/Application/Builder/WinFormsApplicationMenuBuilder.cs).
It does not need an additional empty concrete wrapper or a custom hierarchy to
make that contract usable.

Before renaming a hierarchy, inspect direct construction, dependency-injection
registrations, inheritance, decorators, builders, examples and tests. Update
all affected consumers together. Avoid keeping a superseded implementation
active beside an unused replacement.

## Event notifications and template hooks

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

protected virtual void OnItemRemoving(long key) {
}

protected void NotifyItemRemoving(long key) {
	OnItemRemoving(key);
	ItemRemoving?.Invoke(key);
}
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
