---
name: decorator-pattern
description: Build a FooDecorator wrapper over an interface following the ExtendedListDecorator<TItem, TConcrete> reference — pass-through virtual members, TConcrete generic arg, convenience subclass. Trigger when layering behavior over an existing abstraction (collections, data sources, serializers, blockchains, loggers).
---

# Decorator Pattern Skill

The codebase layers behavior over abstractions with **decorator classes**. The canonical reference is `ExtendedListDecorator<TItem, TConcrete>` (`src/Sphere10.Framework/Collections/Lists/ExtendedListDecorator.cs`). Follow it exactly.

## The recipe

A decorator family has **two classes** in one file: a full two-generic-arg version and a convenience interface-typed version.

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
	public virtual int Count => InternalFoo.Count;

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
1. **Mirror the whole interface** with `virtual` pass-throughs to the inner object. Never implement logic in the decorator base — subclasses override only what they add.
2. **`TConcrete` generic arg** constrained to the interface (`where TConcrete : IFoo<TItem>`). This lets subclasses reach the decorated object in its concrete type without re-casting. Always also provide the interface-typed convenience subclass (`FooDecorator<TItem> : FooDecorator<TItem, IFoo<TItem>>`).
3. **Store the inner object** in a field named `Internal<Thing>` (`InternalList`, `InternalDictionary`, `InternalBlockchain`, `InternalDataSource`). Prefer `protected readonly` + `Guard.ArgumentNotNull` in the constructor (the newer convention — see `DictionaryDecorator`, `DataSourceDecorator`, `BlockchainDecorator`). Some older files use an `internal` mutable field and no null-check (e.g. `CollectionDecorator`); **do not copy that** for new code.
4. **Events**: forward subscription to the inner object with `add`/`remove` accessors rather than re-declaring a backing event (see `BlockchainDecorator`).
   ```csharp
   public event EventHandlerEx<X> Changed {
	   add => InternalFoo.Changed += value;
	   remove => InternalFoo.Changed -= value;
   }
   ```
5. **Indexer / operator members**: route them through the overridable virtual methods so decoration is centralized (e.g. `ExtendedListDecorator.this[long]` calls `Read`/`Update`, not the field directly).
6. **Subclasses** (e.g. `BufferDecorator`, `RecyclableListDecorator`, `DynamicMerkleTreeDecorator`) generally declare **no new field** — they extend the base decorator and override specific members. They may add their own convenience non-`TConcrete` subclass too.
7. Keep one decorator family per file (both generic-arity classes together), with the standard license header.

## When a decorator already exists
Don't create a parallel decorator for an abstraction that has one. Extend the existing base (e.g. a new list behavior extends `ExtendedListDecorator`, a new merkle-tree behavior extends `MerkleTreeDecorator`).
