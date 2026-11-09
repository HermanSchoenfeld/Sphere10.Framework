# Extra hash functions

Hash-function adapters over HashLib4CSharp and Blake2Fast, BouncyCastle digests for Grindahl-512, Haval and RadioGatun32, and the RandomHash proof-of-work algorithms.

The adapters implement the core library's `IHashFunction`, and the [module configuration](../ModuleConfiguration.cs) registers them with `Hashers` for the `CHF` values that the core library does not cover. `HashLibAdapter` wraps a HashLib4CSharp `IHash` and serves most registrations. `Blake2bFastAdapter` and `Blake2sFastAdapter` serve the `CHF.Blake2b_*_Fast` and `CHF.Blake2s_*_Fast` values. `Grindahl512Digest`, `HavalDigest`, `Haval5_256Digest` and `RadioGatun32Digest` are BouncyCastle `IDigest` implementations, and the module does not register them.

`RandomHash2` and `RandomHashFast` hash a block header with the RandomHash algorithms. Both call `Hashers.Hash` for algorithms that only the module registers, so register the module before using them. `RandomHash2Fast` adds an optional cache of computed nonces for mining loops. `RandomHashFast` uses the MurmurHash3 digest from [Non-cryptographic hash functions](../HF/README.md) for its checksums.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Blake2bFastAdapter`](Blake2bFastAdapter.cs) | class | `HashFunctionBase` over Blake2Fast's incremental BLAKE2b for a digest size in bytes, whose `Clone` throws `NotSupportedException` |
| [`Blake2sFastAdapter`](Blake2bFastAdapter.cs) | class | `HashFunctionBase` over Blake2Fast's incremental BLAKE2s for a digest size in bytes, whose `Clone` throws `NotSupportedException` |
| [`Grindahl512Digest`](Grindahl512Digest.cs) | class | BouncyCastle `IDigest` and `IMemoable` implementation of the Grindahl-512 hash |
| [`HashLibAdapter`](HashLibAdapter.cs) | class | `IHashFunction` over a HashLib4CSharp `IHash`, declared in the `Sphere10.Framework` namespace |
| [`Haval5_256Digest`](HavalDigest.cs) | class | Five-pass, 256-bit Haval digest |
| [`HavalDigest`](HavalDigest.cs) | abstract class | BouncyCastle `IDigest` base for Haval with a chosen pass count and digest length, whose subclasses supply the block transform |
| [`RadioGatun32Digest`](RadioGatun32Digest.cs) | class | BouncyCastle `IDigest` and `IMemoable` implementation of RadioGatun32 |
| [`RandomHash2`](RandomHash2.cs) | class | RandomHash2 proof-of-work hash of a block header over 77 `CHF` algorithms, through `Compute`, `Hash` and `TryHash` |
| [`RandomHash2Exception`](RandomHash2Exception.cs) | class | Exception raised when a RandomHash2 computation does not reach its final round |
| [`RandomHash2Fast`](RandomHash2Fast.cs) | class | Sealed `RandomHash2` variant with an optional `Cache` of partially and fully computed nonces and memory statistics |
| [`RandomHashFast`](RandomHashFast.cs) | class | Sealed implementation of the original RandomHash over 18 `CHF` algorithms, through static `Compute` |
| [`RandomHashUtils`](RandomHashUtils.cs) | static class | Little-endian 32-bit word access, 8-bit rotations and array clone, concatenation and comparison used by RandomHash |
