# Extensions

Extension methods over BCL and Framework types, from strings, dates, sequences and streams to the decorator activators that wrap collections, comparers and serializers.

Each class extends one target type or family, and its name says which. The members become available through the `Sphere10.Framework` namespace, except `ObjectExtensions`, which is in the `ExtensionProperties` namespace. Before writing a new helper, look for the target type's class here and for a matching static family in the `Tools` namespace.

`DecoratorExtensions` is the most connected class. Its `As*` and `With*` methods wrap a list, dictionary, set, comparer, stream or serializer in the matching decorator, for example `AsSynchronized`, `AsObservable`, `AsMerkleized`, `AsProjection`, `AsInverted`, `AsNullableSerializer` and `AsTransactional`. The decorators themselves are in [Collections](../Collections/README.md), [Comparers](../Comparers/README.md), [Streams](../Streams/README.md) and [Serialization](../Serialization/README.md). `ToHexString` on `byte[]` adds a `0x` prefix unless `ommit_0x` is `true`. `MemberInfoExtensions` and `TimeSpanExtensions` are currently empty.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionExtensions`](ActionExtensions.cs) | static class | Wraps `Action` delegates to run asynchronously, ignore or handle exceptions, retry or fail over. |
| [`Array2DExtensions`](Array2DExtensions.cs) | static class | Row access, flattening, flat spans and sequence equality for two-dimensional arrays. |
| [`ArrayExtensions`](ArrayExtensions.cs) | static class | Copies, sub-arrays, left and right slices, reversal and insertion sort for arrays. |
| [`AssemblyExtensions`](AssemblyExtensions.cs) | static class | Extracts embedded resources, finds derived types and lists referenced or non-framework assemblies of an `AppDomain`. |
| [`BigIntegerExtensions`](BigIntegerExtensions.cs) | static class | Bit counts, modular inverse, random bits and probable-prime tests for `BigInteger`. |
| [`BoolExtensions`](BoolExtensions.cs) | static class | Formats a `bool` as yes/no or open/closed text. |
| [`ByteArrayExtensions`](ByteArrayExtensions.cs) | static class | Slicing, bit access, XOR, hashing, text encodings and endian readers over `byte[]`. |
| [`CustomAttributesProviderExtensions`](CustomAttributesProviderExtensions.cs) | static class | Typed attribute lookup and presence checks on `ICustomAttributeProvider`. |
| [`DateTimeExtensions`](DateTimeExtensions.cs) | static class | Calendar arithmetic (business and financial days, weeks, months, quarters), rounding, boundaries and formatting for `DateTime`. |
| [`DebugExtensions`](DebugExtensions.cs) | static class | C# extension members on `Debug` that record the main thread and assert, in debug builds, that code is not running on it. |
| [`DecoratorExtensions`](DecoratorExtensions.cs) | static class | Activates decorators, adapters and projections over collections, dictionaries, sets, comparers, streams and serializers. |
| [`DirectoryInfoExtensions`](DirectoryInfoExtensions.cs) | static class | Lists the subfolder names of a `DirectoryInfo`. |
| [`EnumExtensions`](EnumExtensions.cs) | static class | Enum attributes and descriptions, default values and flag set, clear, test and combine operations. |
| [`ExceptionExtensions`](ExceptionExtensions.cs) | static class | Flattens inner and aggregate exceptions, finds the innermost one and formats display and diagnostic text. |
| [`ExpressionExtensions`](ExpressionExtensions.cs) | static class | Resolves the member that a lambda expression accesses. |
| [`FileInfoExtensions`](FileInfoExtensions.cs) | static class | Determines the MIME type of a `FileInfo`. |
| [`GuidExtensions`](GuidExtensions.cs) | static class | Formats a `Guid` as its hexadecimal digits without dashes or braces. |
| [`HashSetExtensions`](HashSetExtensions.cs) | static class | Adds a range of items to a `HashSet<T>`. |
| [`IAsyncEnumerableExtensions`](IAsyncEnumerableExtensions.cs) | static class | Materializes an `IAsyncEnumerable<T>` into an array or list. |
| [`ICollectionExtensions`](ICollectionExtensions.cs) | static class | Sequential range adds and predicate removal on `ICollection<T>`. |
| [`IComparerExtensions`](IComparerExtensions.cs) | static class | Converts an `IComparer<T>` into an equality comparer. |
| [`IDictionaryExtensions`](DictionaryExtensions.cs) | static class | Merging, inversion, reverse lookup, range adds and read-only or synchronized views of dictionaries. |
| [`IDisposableExtensions`](IDisposableExtensions.cs) | static class | Attaches actions that run before or after an `IDisposable` is disposed. |
| [`IEnumerableExtensions`](IEnumerableExtensions.cs) | static class | LINQ-style sequence operations such as partitioning, pairing, zipping, inclusive take, binary search, graph building, hierarchy and randomization. |
| [`IEnumeratorExtensions`](IEnumeratorExtensions.cs) | static class | Wraps enumerators with move and dispose callbacks, boundaries, memory and enumerable views. |
| [`IListExtensions`](IListExtensions.cs) | static class | Sequential range operations, rotation, swapping, clone removal, ordering checks and binary range search on `IList<T>`. |
| [`ILookupExtensions`](ILookupExtensions.cs) | static class | Aggregates and transforms the values of an `ILookup<TKey, TValue>`. |
| [`KeyValuePairExtensions`](KeyValuePairExtensions.cs) | static class | Inverts or projects a `KeyValuePair<TKey, TValue>`. |
| [`LinkedListExtensions`](LinkedListExtensions.cs) | static class | Finds a node by predicate in a `LinkedList<T>`. |
| [`MemberInfoExtensions`](MemberInfoExtensions.cs) | static class | Empty placeholder for `MemberInfo` extensions. |
| [`NullableExtensions`](NullableExtensions.cs) | static class | Tests whether a nullable `int` is null or zero. |
| [`NumericExtensions`](NumericExtensions.cs) | static class | Clamps numbers, dates and time spans to a range, rounds floating values and sums unsigned sequences. |
| [`ObjectExtensions`](ExtensionProperties.cs) | static class | Attaches dynamic extension properties to any object for the object's lifetime. |
| [`PagedListExtensions`](PagedListExtensions.cs) | static class | Shuffling insert and remove operations across the pages of an `IPagedList<T>`. |
| [`ProcessExtensions`](ProcessExtensions.cs) | static class | Kills a process tree and waits for exit with a `TimeSpan` timeout. |
| [`ProducerConsumerQueueExtensions`](ProducerConsumerQueueExtensions.cs) | static class | Put and take helpers, including asynchronous batch and size-limited takes, for `ProducerConsumerQueue<T>`. |
| [`RandomExtensions`](RandomExtensions.cs) | static class | Random booleans, bytes, integers, ranges, decimals, characters and strings from `Random`. |
| [`SemaphoreSlimExtensions`](SemaphoreSlimExtensions.cs) | static class | Waits on a `SemaphoreSlim` and returns a scope that releases it on disposal. |
| [`SpanExtensions`](SpanExtensions.cs) | static class | Additional `Slice` overloads for `Span<T>` and `ReadOnlySpan<T>`. |
| [`StreamExtensions`](StreamExtensions.cs) | static class | Reading, writing, copying, peeking and position-restoring scopes for `Stream`. |
| [`StringBuilderExtensions`](StringBuilderExtensions.cs) | static class | Formatted `AppendLine` and `Clear` for `StringBuilder`. |
| [`StringCollectionExtensions`](StringCollectionExtensions.cs) | static class | Trims many strings and joins them into paragraph-case text. |
| [`StringExtensions`](StringExtensions.cs) | static class | Trimming, casing, escaping, splitting, parsing, formatting and path helpers for `string`. |
| [`TaskExtensions`](TaskExtensions.cs) | static class | Timeouts, cancellation, retry, fail-over, exception filtering and safe waits for `Task` and `Task<T>`. |
| [`TextReaderExtensions`](TextReaderExtensions.cs) | static class | Character-level peek, read and match on `TextReader`. |
| [`TextWriterExtensions`](TextWriterExtensions.cs) | static class | Prints a hierarchy as an indented text diagram to a `TextWriter`. |
| [`TimeSpanExtensions`](TimespanExtensions.cs) | static class | Empty placeholder for `TimeSpan` extensions. |
| [`TreeExtensions`](TreeExtensions.cs) | static class | Breadth-first and depth-first traversal of a hierarchy, optionally with parents. |
| [`TupleExtensions`](TupleExtensions.cs) | static class | Converts tuples to key/value pairs and unpacks `Tuple` instances. |
| [`TypeExtensions`](TypeExtensions.cs) | static class | Reflection queries on `Type`: ancestry, generic definitions, numeric and nullable checks, activation and C# names. |
| [`UniversalExtensions`](UniversalExtensions.cs) | static class | Extensions for any value, such as `IsIn`, `ConcatWith`, `UnionWith`, `ToStringSafe`, graph visits and `AsSignedItem`. |
| [`ValueTaskExtensions`](ValueTaskExtensions.cs) | static class | `ContinueOnSameThread` for `ValueTask` and `ValueTask<T>`. |
