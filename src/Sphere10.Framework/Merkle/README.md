# Merkle Trees

Dynamic merkle trees that hash leaf digests into a root and generate and verify existence, consistency, contains, update, append and delete proofs, with merkleized lists, sets, dictionaries and paged buffers.

`IMerkleTree` reports its `HashAlgorithm`, `Root`, `Size` and the hash stored at any `MerkleCoordinate`. A coordinate is a level and an index within that level, and leaves sit at level 0. `IDynamicMerkleTree` adds a mutable `Leafs` list of leaf digests, and the tree recomputes the affected nodes when that list changes. A parent node is the `Hashers.JoinHash` of its two children. A node without a right child bubbles its left child's hash up unchanged. `MerkleMath` holds the navigation, flat-index and proof algorithms, and `IMerkleTreeExtensions` exposes the proof generators on any tree. The folder is the reference implementation of the [dynamic merkle-tree paper](https://sphere10.com/tech/dynamic-merkle-trees), and its types belong to the `Sphere10.Framework` namespace in the `Sphere10.Framework` package.

A merkleized collection implements `IMerkleObject`, whose `MerkleTree` property tracks the collection's state. `MerkleListAdapter<TItem>` decorates an extended list, hashes every item through an `IItemHasher<TItem>` and mirrors each mutation into a `FlatMerkleTree` by default. Its default hasher is an `ItemDigestor<TItem>` over the item's serialized bytes with SHA2-256. `MerklePagedBuffer` hashes each page of a memory-paged buffer as one leaf and recomputes dirty page hashes before the root or a node is read. The `StreamMapped*` types wrap the stream-mapped collections from [Collections](../Collections/README.md) and register a `MerkleTreeIndex` attachment from [ObjectStream](../ObjectStream/README.md), which keeps the tree inside the stream. `MerkleTree.ComputeMerkleRoot` computes a root from items or leaf digests in one call.

`PartialMerkleTree.GetValue` currently calls itself instead of returning the tracked hash, so reading a node or the root of a partial tree does not return.

## Choose a tree

| Type | Use when |
|---|---|
| `FlatMerkleTree` | Random reads and proofs must be fast. Every perfect node lives in one contiguous buffer, reads are O(1), and appending or updating a leaf costs O(log N) because dirty ancestors are recomputed lazily. Inserting or removing a leaf moves the later leaves and costs O(N). |
| `SimpleMerkleTree` | The tree is small. It keeps every level in lists and recomputes all of them after any leaf change, and it is not thread-safe. |
| `LongMerkleTree` | The dataset is very large and only grows. It keeps only the sub-root hashes, accepts `Add` and `Clear`, and throws `NotSupportedException` on insert, update and removal. Reading a node inside a sub-root throws `InvalidOperationException`. |
| `PartialMerkleTree` | A proof is built from a known subset of nodes. It is a dictionary of coordinates to hashes with a fixed leaf count. |

## Example

```csharp
var chf = CHF.SHA2_256;
var leafs = new[] { "A", "B", "C" }.Select(x => Hashers.Hash(chf, Encoding.ASCII.GetBytes(x))).ToArray();
var tree = new FlatMerkleTree(chf);
tree.Leafs.AddRange(leafs);

var proof = tree.GenerateExistenceProof(1).ToArray();
var valid = MerkleMath.VerifyExistenceProof(chf, tree.Root, tree.Size, 1, leafs[1], proof);
```

The [merkle fixtures](../../../tests/Sphere10.Framework.Tests/Merkle) cover tree math, proofs and the tree implementations, and the [stream-mapped merkle fixtures](../../../tests/Sphere10.Framework.Tests/Collections/StreamMappedMerkle) cover the stream-mapped collections.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DynamicMerkleTreeDecorator`](DynamicMerkleTreeDecorator.cs) | abstract class | Non-generic decorator base over an `IDynamicMerkleTree`. |
| [`DynamicMerkleTreeDecorator<TMerkleTree>`](DynamicMerkleTreeDecorator.cs) | abstract class | Decorator base that forwards tree members and the `Leafs` list to an inner dynamic tree. |
| [`FlatMerkleTree`](FlatMerkleTree.cs) | class | Dynamic tree that stores every perfect node in one flat buffer, computes imperfect nodes on the fly and recomputes dirty nodes lazily. |
| [`IDynamicMerkleTree`](IDynamicMerkleTree.cs) | interface | Merkle tree with a mutable `Leafs` list of leaf digests. |
| [`IMerkleCollection<TItem>`](IMerkleCollection.cs) | interface | Collection that is also an `IMerkleObject`. |
| [`IMerkleDictionary<TKey, TValue>`](IMerkleDictionary.cs) | interface | Dictionary whose key/value pairs are tracked by a merkle tree. |
| [`IMerkleList<TItem>`](IMerkleList.cs) | interface | Extended list whose ordered items are tracked by a merkle tree. |
| [`IMerkleObject`](IMerkleObject.cs) | interface | Object that exposes the `MerkleTree` of its current state. |
| [`IMerkleSet<TItem>`](IMerkleSet.cs) | interface | Set whose items are tracked by a merkle tree. |
| [`IMerkleTree`](IMerkleTree.cs) | interface | Tree contract with `HashAlgorithm`, `Root`, `Size` and `GetValue` by coordinate. |
| [`IMerkleTreeExtensions`](IMerkleTree.cs) | static class | Node, children, leaf and path-to-root reads, `CalculateOldRoot` and the `Generate*Proof` methods for any `IMerkleTree`. |
| [`LongMerkleTree`](LongMerkleTree.cs) | class | Append-only dynamic tree that keeps only sub-root hashes in memory. |
| [`MerkleCoordinate`](MerkleCoordinate.cs) | record | Level and index of a node, with `LeafAt`, `From`, `Root`, `SubRoots`, `Next`, `Last`, the `Null` sentinel and `ToFlatIndex`. |
| [`MerkleListAdapter<TItem>`](MerkleListAdapter.cs) | class | Merkleized list over an `ExtendedList<TItem>` or a supplied list, usable stand-alone in memory. |
| [`MerkleListAdapter<TItem, TList>`](MerkleListAdapter.cs) | class | List decorator that hashes each item into an inner dynamic tree on every add, update, insert and removal. |
| [`MerkleMath`](MerkleMath.cs) | static class | Height, level-length, parent, child, sibling and sub-root navigation, flat indexing, node hashing and the proof path, generation and verification algorithms. |
| [`MerkleNode`](MerkleNode.cs) | record | A node's coordinate and hash. |
| [`MerkleNodeTraits`](MerkleNodeTraits.cs) | enum | Flags for root, leaf, perfect, left or right child, child presence and bubble-up nodes. |
| [`MerklePagedBuffer`](MerklePagedBuffer.cs) | class | Memory-paged buffer decorator whose tree has one leaf per page hash, for merkleized data files. |
| [`MerkleSize`](MerkleSize.cs) | record | A tree's leaf count and height, built by `FromLeafCount`. |
| [`MerkleSubRoot`](MerkleSubRoot.cs) | record | Height and hash of the root of one perfect subtree. |
| [`MerkleTree`](MerkleTreeTool.cs) | static class | `ComputeMerkleRoot` overloads over items, an item hasher or leaf digests, defaulting to SHA2-256. |
| [`MerkleTreeDecorator`](MerkleTreeDecorator.cs) | abstract class | Non-generic decorator base over an `IMerkleTree`. |
| [`MerkleTreeDecorator<TMerkleTree>`](MerkleTreeDecorator.cs) | abstract class | Decorator base whose virtual members forward to an inner merkle tree. |
| [`MerkleTreeLeafCounter`](Delegates.cs) | delegate | Returns the current number of leaves. |
| [`MerkleTreeLeafGetter`](Delegates.cs) | delegate | Returns the leaf value at an index as a byte span. |
| [`MerkleTreeSubRootsGetter`](Delegates.cs) | delegate | Yields the sub-roots of a tree. |
| [`PartialMerkleTree`](PartialMerkleTree.cs) | sealed class | Dictionary of coordinates to hashes that holds only a subset of a tree's nodes, for multi-item proofs. |
| [`SimpleMerkleTree`](SimpleMerkleTree.cs) | class | Dynamic tree that keeps every level in lists and recomputes them all after a leaf change. |
| [`StreamMappedMerkleDictionary<TKey, TValue>`](StreamMappedMerkleDictionary.cs) | class | Stream-mapped dictionary with a merkle-tree index over its key/value pairs. |
| [`StreamMappedMerkleHashSet<TItem>`](StreamMappedMerkleHashSet.cs) | class | Stream-mapped hash set stored as a merkle dictionary keyed by item digests. |
| [`StreamMappedMerkleHashSet<TItem, TInner>`](StreamMappedMerkleHashSet.cs) | class | Set decorator that pairs a stream-mapped hash set with a supplied merkle tree. |
| [`StreamMappedMerkleList<TItem>`](StreamMappedMerkleList.cs) | class | Stream-mapped list with a merkle-tree index over its item hashes. |
| [`StreamMappedMerkleRecyclableList<TItem>`](StreamMappedMerkleRecyclableList.cs) | class | Stream-mapped recyclable list with a merkle-tree index over its item hashes. |
| [`TreeTraversalType`](TreeTraversalType.cs) | enum | Pre-order, post-order, in-order or level-order traversal. |
