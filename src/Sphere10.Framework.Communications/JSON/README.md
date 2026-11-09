# JSON Converters

Provides Newtonsoft.Json converters for hexadecimal byte arrays and for integers written in decimal or `0x` hexadecimal.

Apply `[JsonConverter(typeof(ByteArrayHexConverter))]` to `byte[]` members of JSON-RPC argument and result types so both sides exchange them as hex strings. `HexadecimalValueConverterReader` converts `int`, `uint`, `long` and `ulong` values. It reads a string that starts with `0x` as base 16 and any other value as base 10, and it writes the value's decimal text unchanged. Both converters are in the `Sphere10.Framework.Communications` namespace and serve the [RPC](../RPC/README.md) layer.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ByteArrayHexConverter`](ByteArrayHexConverter.cs) | class | `JsonConverter<byte[]>` that writes and reads byte arrays as hexadecimal strings |
| [`HexadecimalValueConverterReader`](HexadecimalValueConverterReader.cs) | class | Sealed `JsonConverter` that reads 32-bit and 64-bit integers from decimal or `0x`-prefixed hexadecimal text |
