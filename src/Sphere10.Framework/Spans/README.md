# Spans

Endian-aware sequential reading of primitive values from byte spans, and span helpers in `Tools.Spans`.

`ByteSpanReader` keeps a read offset and an `EndianBitConverter`. Each call takes the source `ReadOnlySpan<byte>`, reads a byte, character, byte block, two-dimensional byte array or 16-, 32- or 64-bit integer at the current offset, and advances the offset. The reader does not hold the span, so pass the same span to each call. For stream-based reading and writing with a chosen endianness, use `EndianBinaryReader` and `EndianBinaryWriter`, which the [Serialization](../Serialization/README.md) serializers read and write through. `Tools.Spans` currently provides only `Empty<T>()`.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ByteSpanReader`](ByteSpanReader.cs) | class | Reads primitive values in sequence from a byte span with a chosen `EndianBitConverter`, advancing an internal offset. |
| [`Tools.Spans`](SpanTool.cs) | tools | Span helpers, currently `Empty<T>()` for an empty `Span<T>`. |
