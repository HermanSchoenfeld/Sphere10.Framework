# Keys, graphs and mapped properties

Compound keys keep the `Sphere10.Framework` namespace and live in the source `Keys` folder. `CompoundKey.From(a, b)`, `From(a, b, c)` and `From(a, b, c, d)` infer their component types. Each readonly key uses component-wise default equality and hashing. Matching `CompoundKeyComparer` types compare lexicographically using the supplied component comparers; fixed-size serializers concatenate component bytes in order and retain the existing two-component wire layout.

`IBTree<TKey,TValue>` exposes the sorted dictionary, order, comparer, traversal, insertion/overwrite, inclusive range and validation contract shared by both algorithms and memory/stream storage. `BTreeBase` continues to own shared dictionary adapters. `BTree` and `BTreePlus` implement their distinct node algorithms; lookup composition consumes `IBTree`.

```csharp
var graph = root.BuildGraph(item => item.Children);
foreach (var node in graph.Visit())
	Console.WriteLine(node.Entity);
```

`BuildGraph` materializes ordered connections and preserves repeated edges, cycles and shared nodes. Source equality, or an explicit comparer, determines node identity; the first representative supplies its outgoing connections. A discriminator prunes excluded nodes. Multiple-root overloads return distinct roots in input order, including roots reachable from other roots. Null items and null connection sequences are skipped. Construction and `GraphNode<T>.Visit` use iterative depth-first traversal. Each enumeration of a node's `Visit()` result has independent state and uses node reference identity by default. `GraphNode<T>.Connections` remains enumerable and may be supplied by the caller.

The existing general-purpose `Visit` extension overloads retain their recursive traversal contract. A null item ends its current sibling sequence, and enumerations of the same returned sequence share visited state. Edge and ancestor selectors are invoked lazily after yielding their node. Construct a new extension traversal to start with fresh state; use `GraphNode<T>.Visit` for the new node traversal contract above.

```csharp
var property = StreamMappedProperty.At<long>(stream, offset);
var cached = StreamMappedProperty.At<long>(stream, offset, useCache: true);
```

`At<T>` infers the size from a constant-size serializer. `From` supports explicit size and serializer and can reuse an existing endian reader/writer. Streams and supplied readers/writers remain caller-owned. Reads observe independent stream changes by default; cached properties require `FlushCache()` after external writes. Cached values use the existing equality-based write suppression. Uncached setters always write. `ValueChanged` reports the last observed/assigned value and the new value; setting does not implicitly read first. Framework-owned clustered headers and B-tree header scalars enable caching explicitly and retain their existing invalidation paths.
