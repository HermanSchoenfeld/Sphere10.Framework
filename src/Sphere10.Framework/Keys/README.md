# Keys

Value-type compound keys of two, three or four ordered components for dictionaries and B-tree indexes.

`CompoundKey<TKey1, TKey2>` and its three- and four-component forms are readonly structs that implement `IEquatable` with component-wise equality and `HashCode.Combine`, so they can key a dictionary or a tree without allocating a tuple class. The static `CompoundKey.From` overloads infer the component types. These keys define equality only and do not implement `IComparable`, so an ordered structure needs an explicit comparer, for example from the [Comparers](../Comparers/README.md) domain.

```csharp
var key = CompoundKey.From("orders", 42);
var same = new CompoundKey<string, int>("orders", 42);
// key == same, and key.ToString() is "(orders, 42)"
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CompoundKey`](CompoundKey.cs) | static class | `From` factory overloads that create two-, three- and four-component compound keys with inferred types. |
| [`CompoundKey<TKey1, TKey2>`](CompoundKey.cs) | struct | Readonly two-component key with component-wise equality, hashing and `==` operators. |
| [`CompoundKey<TKey1, TKey2, TKey3>`](CompoundKey.cs) | struct | Readonly three-component key with component-wise equality, hashing and `==` operators. |
| [`CompoundKey<TKey1, TKey2, TKey3, TKey4>`](CompoundKey.cs) | struct | Readonly four-component key with component-wise equality, hashing and `==` operators. |
