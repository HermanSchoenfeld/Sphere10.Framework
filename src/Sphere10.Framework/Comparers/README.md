# Comparers

Reusable `IComparer<T>` and `IEqualityComparer<T>` implementations for sequences, byte arrays, tuples, projections and composed orderings.

Use these types when a collection, dictionary, sort or lookup needs an ordering or equality rule that the BCL defaults do not provide. `ComparerBuilder.For<T>()` and `EqualityComparerBuilder.For<T>()` start a fluent chain from a neutral comparer, and the comparer extensions in [Extensions](../Extensions/README.md) (`ThenBy`, `ThenByDescending`, `AsInverted`, `AsProjection`, `AsPacked`, `AsNegation`, `AsEqualityComparer`) wrap any existing comparer in the matching decorator from this folder. `ComparerFactory.Default` is a per-type registry of comparers for primitives, their nullable forms and other registered types. Object spaces, object streams and several collection helpers resolve their comparers from it.

Most types live in the `Sphere10.Framework` namespace. `ArrayEqualityComparer<T>` and `EnumerableContainsSameComparer<T>` are in `Sphere10.Framework.Collections`, `EnumerableComparer<T>` is in `Sphere10.Framework.Misc`, and `MemberInfoComparer<T>` is in `Sphere10.Framework.FastReflection`. `ComparerToEqualityComparerAdapter<T>` hashes with `GetHashCode()` of the item, so the hash agrees with the comparer only when the item's own equality does.

```csharp
IComparer<Person> order = ComparerBuilder.For<Person>()
	.StartWith(p => p.LastName)
	.ThenBy(p => p.FirstName);

IEqualityComparer<Person> sameId = EqualityComparerBuilder.For<Person>().By(p => p.Id);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionEqualityComparer<T>`](ActionEqualityComparer.cs) | class | Equality comparer that calls a supplied `Func<T, T, bool>`. |
| [`ArrayEqualityComparer<T>`](ArrayEqualityComparer.cs) | class | Element-wise equality of `T[]` arrays with an optional item comparer. |
| [`ByteArrayComparer`](ByteArrayComparer.cs) | class | Lexicographic ordering of byte arrays and byte spans, with a shared `Instance`. |
| [`ByteArrayEqualityComparer`](ByteArrayEqualityComparer.cs) | class | Content equality and hashing of byte arrays and byte spans, with a shared `Instance`. |
| [`CastedEqualityComparer<TItem, TBase>`](CastedEqualityComparer.cs) | class | Applies an `IEqualityComparer<TItem>` to values typed as a base type `TBase`. |
| [`ComparerBuilder`](ComparerBuilder.cs) | static class | Starts a fluent comparer chain with `For<T>()`, which returns the neutral `IdempotentComparer<T>`. |
| [`ComparerFactory`](ComparerFactory.cs) | class | Registry of comparers and equality comparers by type, with `Default` preloaded for primitives, nullables and common value types. |
| [`ComparerToEqualityComparerAdapter<T>`](ComparerToEqualityComparerAdapter.cs) | class | Treats `Compare(x, y) == 0` as equality and hashes with the item's `GetHashCode()`. |
| [`CompositeComparer<T>`](CompositeComparer.cs) | class | Applies ordered comparers until one distinguishes the values. |
| [`CompositeEqualityComparer<T>`](CompositeEqualityComparer.cs) | class | Requires every composed equality comparer to agree and combines their hash codes. |
| [`DictionaryEqualityComparer<TKey, TValue>`](DictionaryEqualityComparer.cs) | class | Compares two `Dictionary<TKey, TValue>` instances by their key and value sets. |
| [`EnumerableComparer<T>`](EnumerableComparer.cs) | class | Lexicographic ordering of two sequences, where a shorter prefix sorts first. |
| [`EnumerableContainsSameComparer<T>`](EnumerableContainsSameComparer.cs) | class | Treats two sequences as equal when each contains all elements of the other, ignoring order. |
| [`EnumerableEqualityComparer<T>`](EnumerableEqualityComparer.cs) | class | Ordered element-wise equality of two sequences with an optional item comparer. |
| [`EqualityComparerAdapter<T>`](EqualityComparerAdapter.cs) | class | Builds equality from an `IComparer<T>` and hashes through an `IItemChecksummer<T>`. |
| [`EqualityComparerBuilder`](EqualityComparerBuilder.cs) | static class | Starts a fluent equality chain with `For<T>()`, which returns the neutral `IdempotentEqualityComparer<T>`. |
| [`ExtendedListEqualityComparer<T>`](ExtendedListEqualityComparer.cs) | class | Element-wise equality of two `IExtendedList<T>` instances. |
| [`IdempotentComparer<T>`](IdempotentComparer.cs) | class | Comparer that reports every pair as equal, used as the seed of `StartWith` chains. |
| [`IdempotentEqualityComparer<T>`](IdempotentEqualityComparer.cs) | class | Equality comparer that reports every pair as equal, used as the seed of `By` chains. |
| [`InvertedComparer<T>`](InvertedComparer.cs) | class | Reverses the order of an existing comparer. |
| [`KeyValuePairEqualityComparer<TKey, TValue>`](KeyValuePairEqualityComparer.cs) | class | Equality of key/value pairs through separate key and value comparers. |
| [`LegacyComparableAdapter<T>`](LegacyComparableAdapter.cs) | class | Wraps a value as `IComparable<T>`, `IComparable` and `IEquatable<T>` using the default comparer. |
| [`ListEqualityComparer<T>`](ListEqualityComparer.cs) | class | Element-wise equality of two `IList<T>` instances. |
| [`MemberInfoComparer<T>`](MemberInfoComparer.cs) | class | Equality of reflection members that also requires the same `ReflectedType`. |
| [`NegatedEqualityComparer<T>`](NegatedEqualityComparer.cs) | class | Inverts the result of an equality comparer while keeping its hash code. |
| [`PackedComparer`](PackedComparer.cs) | class | Holds a typed comparer behind `IComparer<object>` and returns it again with `Unpack<TItem>()`. |
| [`PackedEqualityComparer`](PackedEqualityComparer.cs) | class | Holds a typed equality comparer behind `IEqualityComparer<object>` and returns it again with `Unpack<TItem>()`. |
| [`ProjectedComparer<TFrom, TTo>`](ProjectedComparer.cs) | class | Orders `TTo` values by mapping them back to `TFrom` and using the source comparer. |
| [`ProjectedEqualityComparer<TFrom, TTo>`](ProjectedEqualityComparer.cs) | class | Compares `TTo` values by mapping them back to `TFrom` and using the source equality comparer. |
| [`ProjectionComparer<TSource, TKey>`](ProjectionComparer.cs) | class | Orders items by a projected key and the key type's comparer. |
| [`ProjectionEqualityComparer<TSource, TKey>`](ProjectionEqualityComparer.cs) | class | Compares and hashes items by a projected key. |
| [`ReferenceEqualityComparer`](ReferenceEqualityComparer.cs) | class | Reference identity equality with runtime identity hash codes, for generic and non-generic use. |
| [`TupleComparer<T1, T2>`](TupleComparer.cs) | class | Orders two-element value tuples item by item with a comparer per element. |
| [`TupleComparer<T1, T2, T3>`](TupleComparer.cs) | class | Orders three-element value tuples item by item with a comparer per element. |
| [`TupleEqualityComparer<T1, T2>`](TupleEqualityComparer.cs) | class | Equality and hashing of two-element value tuples with a comparer per element. |
| [`TupleEqualityComparer<T1, T2, T3>`](TupleEqualityComparer.cs) | class | Equality and hashing of three-element value tuples with a comparer per element. |
