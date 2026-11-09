# Big-integer helpers

Conversions between BouncyCastle `BigInteger` values and fixed-length unsigned big-endian byte arrays.

`BigIntegerUtils.BigIntegerToBytes` writes a value into exactly the requested number of bytes, dropping a leading sign byte and padding on the left with zeros. `BytesToBigIntegerPositive` reads bytes as a positive value. Both return `null` for a `null` argument. `ECDSA` and `Schnorr` in [Elliptic-curve signatures](../EC/README.md) use them for key and signature encoding. The `Pack` helper in this folder is internal.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BigIntegerUtils`](BigIntegerUtils.cs) | static class | Converts a BouncyCastle `BigInteger` to a fixed-length unsigned big-endian array and back |
