# Non-cryptographic hash functions

MurmurHash3 in its x86 32-bit form, implemented as a BouncyCastle digest.

`Murmur3_x86_32Digest` implements BouncyCastle's `IDigest` and `IMemoable`, so it can be copied and reset like the other BouncyCastle digests. Its constructor takes an optional 32-bit seed, and its parameterless `DoFinal()` returns the hash as a `uint`. MurmurHash3 is not a cryptographic hash, so use it only for checksums and hash tables. [RandomHash](../CHF/README.md) uses it for its checksums.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Murmur3_x86_32Digest`](Murmur3_x86_32Digest.cs) | class | Seedable MurmurHash3 x86 32-bit `IDigest` with a `uint`-returning `DoFinal()` |
