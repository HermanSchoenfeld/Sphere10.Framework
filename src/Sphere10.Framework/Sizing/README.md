# Sizing

Item sizers that compute the serialized byte size of values, the sizing half of every item serializer.

`IItemSizer<T>` reports whether a type has a constant serialized size, what that size is, whether `null` is supported, and the size of one item or a sequence of items. Every `IItemSerializer<T>` in [Serialization](../Serialization/README.md) extends it, so collections and stream-mapped structures can reserve space or skip records without serializing them. The non-generic `IItemSizer` exposes the same operations over `object` for code that only knows the item type at run time. Sizing methods take a `SerializationContext`, and the `IItemSizerExtensions` overloads create and dispose one for a single call. `SizeDescriptorSizer` sizes the length prefixes that variable-size serializers write, according to a `SizeDescriptorStrategy`.

Derive from `ItemSizer<TItem>` or `ItemSizerBase<TItem>` and implement `CalculateSize`. The two differ only in `ConstantSize` for a variable-size sizer, which is `-1` in `ItemSizer<TItem>` and throws `InvalidOperationException` in `ItemSizerBase<TItem>`. Use `ConstantLengthItemSizer<TItem>` for fixed-size values and `ActionItemSizer<T>` to wrap a delegate.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionItemSizer<T>`](ActionItemSizer.cs) | class | Variable-size sizer that calls a `Func<T, long>` delegate for each item. |
| [`ConstantLengthItemSizer<TItem>`](ConstantLengthItemSizer.cs) | class | Sizer that reports the same byte size for every item and multiplies it for sequences. |
| [`IItemSizer`](IItemSizer.cs) | interface | Untyped sizer contract with item type, null support, constant size and `object`-based size calculation. |
| [`IItemSizer<T>`](IItemSizer.cs) | interface | Typed sizer contract that calculates the size of one item or the total and individual sizes of a sequence. |
| [`IItemSizerExtensions`](IItemSizer.cs) | static class | `CalculateSize`, `CalculateTotalSize` and `PackedCalculateSize` overloads that create their own `SerializationContext`. |
| [`ItemSizer<TItem>`](ItemSizer.cs) | abstract class | Variable-size sizer base whose `ConstantSize` is `-1`, leaving only `CalculateSize` to implement. |
| [`ItemSizerBase<TItem>`](ItemSizerBase.cs) | abstract class | Variable-size sizer base whose `ConstantSize` throws, leaving only `CalculateSize` to implement. |
| [`ItemSizerDecorator<TItem, TObjectSizer>`](ItemSizerDecorator.cs) | class | Forwards every sizer member to an inner sizer, as the base of the serializer decorators. |
| [`SizeDescriptorSizer`](SizeDescriptorSizer.cs) | class | Byte size of a length prefix written with a given `SizeDescriptorStrategy`, VarInt by default. |
| [`SizeDescriptorStrategy`](SizeDescriptorStrategy.cs) | enum | Length-prefix encodings `UseVarInt`, `UseCVarInt`, `UseULong`, `UseUInt32`, `UseUInt16` and `UseByte`. |
| [`SizeDescriptorStrategyExtensions`](SizeDescriptorStrategy.cs) | static class | `IsConstantSize()` for a `SizeDescriptorStrategy`, true for the fixed-width encodings. |
| [`StringSizer`](StringSizer.cs) | class | Sizes a string as its byte count in a given `Encoding`. |
