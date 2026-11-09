# Memory

Memory-size units and conversions, readable byte counts, primitive sizes and a union that reinterprets one array reference as another element type.

`Tools.Memory.ConvertMemoryMetric` converts between bit and byte units of `MemoryMetric` with decimal factors, so a kilobyte is 1000 bytes there. `GetBytesReadable` formats a byte count with binary 1024 steps and the unit's description, for example `1.5 mb`. `SizeOfPrimitive` returns the size of a primitive or `decimal` type, and `IsSerializationPrimitive` tests for one, as the binary serializers in [Serialization](../Serialization/README.md) need.

`ReinterpretArray` overlays fields of nine array types at offset zero, so `ReinterpretArray.From(bytes).AsUInt32Array` reads the same memory as 32-bit values without copying. The runtime keeps the original array's element count, so the reinterpreted array reports the wrong `Length`, and indexing past the real data reads outside the buffer. Prefer `MemoryMarshal.Cast` over spans unless a reinterpreted array object is required.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`MemoryMetric`](MemoryMetric.cs) | enum | Bit and byte units from bits and bytes up to peta, with short descriptions such as `kb` and `mbits`. |
| [`ReinterpretArray`](ReinterpretArray.cs) | struct | Explicit-layout union that views one array reference as a byte, 16-, 32- or 64-bit integer, float or double array. |
| [`Tools.Memory`](MemoryTool.cs) | tools | Converts between memory units, formats byte counts for display and reports primitive type sizes. |
