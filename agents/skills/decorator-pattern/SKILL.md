---
name: decorator-pattern
description: Layer behavior over an existing abstraction with its decorator family, forwarding members and events while preserving async execution and ownership. Trigger for collection, data-source, serializer, blockchain or logger wrappers.
---

# Decorator Pattern Skill

Read [type hierarchy guidance](../../../docs/guidelines/patterns/type-hierarchies.md) when selecting the contract and inheritance layers.

The codebase layers behavior over abstractions with **decorator classes**. Start with the existing family for the contract being decorated. References include [ExtendedListDecorator](../../../src/Sphere10.Framework/Collections/Lists/ExtendedListDecorator.cs), [DataSourceDecorator](../../../src/Sphere10.Framework/DataSource/DataSourceDecorator.cs) and [BlockchainDecorator](../../../src/Sphere10.Framework.Consensus/Blockchain/BlockchainDecorator.cs). Preserve their contract and dispatch structure rather than copying unrelated storage or old formatting.

## The recipe

When subclasses benefit from a typed inner object, provide a `TConcrete` version and an interface-typed convenience version in the same file. Existing families use this pattern; simpler wrappers need only the form their consumers use. Inheritance-only decorator foundations are abstract, with protected constructors; retain the established `FooDecorator` family name.

```csharp
/// <summary>
/// Decorator pattern for an IFoo. The <typeparamref name="TConcrete"/> generic argument
/// ensures sub-classes can retrieve the decorated object in its type, without an
/// expensive chain of casts/retrieves.
/// </summary>
public abstract class FooDecorator<TItem, TConcrete> : IFoo<TItem>
	where TConcrete : IFoo<TItem> {
	protected readonly TConcrete InternalFoo;

	protected FooDecorator(TConcrete internalFoo) {
		Guard.ArgumentNotNull(internalFoo, nameof(internalFoo));
		InternalFoo = internalFoo;
	}

	// Every interface member is a virtual pass-through to the inner object.
	public virtual long Count => InternalFoo.Count;

	public virtual void Add(TItem item) => InternalFoo.Add(item);
}

/// <summary>Interface-typed convenience decorator.</summary>
public abstract class FooDecorator<TItem> : FooDecorator<TItem, IFoo<TItem>> {
	protected FooDecorator(IFoo<TItem> internalFoo)
		: base(internalFoo) {
	}
}
```

## Rules

1. **Cover the whole contract**, normally with virtual pass-throughs. Reuse a suitable common base when it already supplies the contract. Keep shared forwarding neutral; behavior subclasses override only the operations they decorate. Derived members such as indexers may route through virtual methods to centralize decoration.
2. **Constrain `TConcrete` to the interface** when that generic form is needed. Supply the interface-typed convenience subclass for the corresponding decorator family; do not add unused generic layers mechanically.
3. **Retain the typed inner object** with `Guard.ArgumentNotNull` in the constructor. Follow the family's `Internal<Thing>` name and use a protected readonly field or protected getter: `DictionaryDecorator` has `InternalDictionary`, while `DataSourceDecorator` exposes `InternalDataSource` as a property. Do not copy old mutable storage solely because another family uses it.
4. **Events**: forward subscription to the inner object with `add`/`remove` accessors rather than re-declaring a backing event (see `BlockchainDecorator`).
5. **Preserve the family's dispatch**: `ExtendedListDecorator.this[long]` calls virtual `Read`/`Update`, and its `int` compatibility members call the `long` forms. An ordinary dictionary indexer may forward directly. Avoid extra cast/retrieval chains or duplicated compatibility logic when the interface already supplies an inline bridge.
6. **Forward sync and async operations in their own direction**. An async decorator calls the inner async operation and returns or awaits its actual task; do not rebuild it through the sync operation, block without need, or discard the task. See [the directional implementation guide](../../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies).
7. Keep one decorator family per file (both generic-arity classes together), with the standard license header.
8. **Preserve ownership**. Forward disposal when it is part of the decorated contract, and respect whether the wrapper owns or borrows the inner object. Array membership copies retain element references; decoration must not silently clone a runtime object or alter its parent links. Subclasses may keep state needed by their behavior without duplicating the inner reference.

Event forwarding uses:

```csharp
public event EventHandlerEx<X> Changed {
	add => InternalFoo.Changed += value;
	remove => InternalFoo.Changed -= value;
}
```

## When a decorator already exists
Don't create a parallel decorator for an abstraction that has one. Extend the existing base (e.g. a new list behavior extends `ExtendedListDecorator`, a new merkle-tree behavior extends `MerkleTreeDecorator`).
