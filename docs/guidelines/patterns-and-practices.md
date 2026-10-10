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

## Ownership and platform extensions

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
