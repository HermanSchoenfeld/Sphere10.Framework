# Conversion

Endian-aware conversion between primitive values and bytes, and exact decimal text for `double` values.

`EndianBitConverter` is the `System.BitConverter` equivalent with a chosen byte order. Use `EndianBitConverter.Little`, `EndianBitConverter.Big` or `EndianBitConverter.For(endianness)` rather than constructing the sealed subclasses. Each converter reads from `ReadOnlySpan<byte>` with `ToInt32`, `ToUInt64`, `ToDouble`, `ToDecimal` and similar members, and writes with `GetBytes` or the allocation-free `WriteTo(value, Span<byte>, index)`. `EndianBinaryReader` and `EndianBinaryWriter` in [IO](../IO/README.md) wrap a converter around a stream, and the serializers in [Serialization](../Serialization/README.md) take an `Endianness` to select one.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BigEndianBitConverter`](BigEndianBitConverter.cs) | class | `EndianBitConverter` that reads and writes big-endian bytes, exposed as `EndianBitConverter.Big`. |
| [`DoubleConverter`](DoubleConverter.cs) | class | `ToExactString` writes the exact decimal value of a `double`. |
| [`EndianBitConverter`](EndianBitConverter.cs) | abstract class | `BitConverter` equivalent for either byte order, with span-based reads and `WriteTo` writes. |
| [`Endianness`](Endianness.cs) | enum | Byte order of a converter: `LittleEndian` or `BigEndian`. |
| [`LittleEndianBitConverter`](LittleEndianBitConverter.cs) | class | `EndianBitConverter` that reads and writes little-endian bytes, exposed as `EndianBitConverter.Little`. |
