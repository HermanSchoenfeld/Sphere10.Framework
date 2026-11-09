# HashLib4CSharp Source

Holds the hashing library's source: the `HashFactory` entry point, the `IHash` family of interfaces, CRC models, Argon2 and BLAKE2 parameters, and the internal algorithm implementations.

Every algorithm is created through a nested factory of the static `HashFactory`, such as `HashFactory.Crypto.CreateSHA2_256()`, `HashFactory.Checksum.CRC.CreateCRC(model)`, `HashFactory.Hash64.CreateXXHash64()`, `HashFactory.HMAC.CreateHMAC(hash, key)`, `HashFactory.KDF.PBKDFArgon2.CreatePBKDFArgon2(password, parameters)`, `HashFactory.XOF.CreateShake_256(...)` and `HashFactory.Adapter.CreateHashAlgorithmFromHash(hash)`. The factories return interfaces, and the implementing classes are internal. `IHashExtensions` adds the `Compute...` and `Transform...` methods for byte arrays, spans, strings, streams, files and unmanaged memory, including asynchronous stream and file forms. An `IHash` keeps state between `Initialize` and `TransformFinal`, so give each concurrent computation its own instance or a `Clone()`.

Namespaces follow the folders under the upstream `HashLib4CSharp` root, for example `HashLib4CSharp.Base` and `HashLib4CSharp.Interfaces`. The [project README](../README.md) lists every algorithm and shows examples for hashes, checksums, MACs, KDFs and XOFs.

## Contents

### Adapter

This folder has no public types. Its internal adapters expose an `IHash` as a `HashAlgorithm`, an HMAC as a `System.Security.Cryptography.HMAC` and a KDF as `DeriveBytes`, and `HashFactory.Adapter` creates them.

### Base

| Type | Kind | Purpose |
|---|---|---|
| [`HashFactory`](Base/HashFactory.cs) | static class | Entry point whose nested static classes (`Checksum`, `Hash32`, `Hash64`, `Hash128`, `Crypto`, `HMAC`, `KMAC`, `Blake2BMAC`, `Blake2SMAC`, `KDF`, `XOF`, `Adapter`, `NullDigestFactory`) create every algorithm |

The internal base classes `Hash`, `BlockHash`, `HashBuffer`, `HashResult` and `MultipleTransformNonBlock` also live here.

### Checksum

| Type | Kind | Purpose |
|---|---|---|
| [`CRCModel`](Checksum/CRCModel.cs) | class | CRC parameter set (width, polynomial, initial value, reflection, final XOR, check value and names) with predefined static models from CRC-3 to CRC-64 |

The Adler-32, fast CRC-32 and generic CRC implementations are internal.

### Crypto

This folder has no public types. It holds the internal cryptographic hash implementations, such as the SHA, BLAKE, RIPEMD, Tiger, HAVAL and GOST families, which `HashFactory.Crypto` creates.

### Enum

| Type | Kind | Purpose |
|---|---|---|
| [`Argon2Type`](Enum/Argon2Type.cs) | enum | Argon2 variant: data-dependent (Argon2d), data-independent (Argon2i) or hybrid (Argon2id) addressing |
| [`Argon2Version`](Enum/Argon2Version.cs) | enum | Argon2 version `Sixteen` (0x10) or `Nineteen` (0x13), where version 19 fixes a memory-reuse weakness |

The `HashRounds` and `HashSize` enums are internal.

### Hash32, Hash64 and Hash128

These folders have no public types. They hold the internal 32-bit, 64-bit and 128-bit non-cryptographic hashes, such as FNV, MurmurHash, xxHash and SipHash, which `HashFactory.Hash32`, `HashFactory.Hash64` and `HashFactory.Hash128` create.

### Interfaces

| Type | Kind | Purpose |
|---|---|---|
| [`IBlake2BMAC`](Interfaces/IMAC.cs) | interface | Keyed BLAKE2b MAC |
| [`IBlake2BMACNotBuiltIn`](Interfaces/IMAC.cs) | interface | Library-implemented keyed BLAKE2b MAC |
| [`IBlake2MAC`](Interfaces/IMAC.cs) | interface | Common base of the keyed BLAKE2 MACs |
| [`IBlake2SMAC`](Interfaces/IMAC.cs) | interface | Keyed BLAKE2s MAC |
| [`IBlake2SMACNotBuiltIn`](Interfaces/IMAC.cs) | interface | Library-implemented keyed BLAKE2s MAC |
| [`IChecksum`](Interfaces/IHashInfo.cs) | interface | Marker for checksum algorithms such as Adler-32 and CRC |
| [`ICRCFactory`](Interfaces/ICRCFactory.cs) | interface | CRC checksum that exposes its `CRCModel`, check value and names |
| [`IHash`](Interfaces/IHash.cs) | interface | Core hash contract: name, block, hash and buffer sizes, `Initialize`, `TransformByteSpan`, `TransformFinal` and `Clone` |
| [`IHashExtensions`](Interfaces/IHash.cs) | static class | Adds `TransformBytes`, `TransformString`, `TransformStream`, `TransformFile`, `TransformUntyped` and matching one-call `Compute...` methods, with async stream and file forms, to `IHash` |
| [`IHashResult`](Interfaces/IHashResult.cs) | interface | Digest value with byte, integer, hexadecimal and Base64 accessors and equality |
| [`IHashWithKey`](Interfaces/IHashInfo.cs) | interface | Keyed non-cryptographic hash such as MurmurHash, xxHash or SipHash |
| [`IHMAC`](Interfaces/IMAC.cs) | interface | HMAC over an underlying hash |
| [`IHMACNotBuiltIn`](Interfaces/IMAC.cs) | interface | Library-implemented HMAC that also exposes its `WorkingKey` |
| [`IKDF`](Interfaces/IKDF.cs) | interface | Key-derivation contract with `GetBytes`, `GetBytesAsync` and `Clear` for sensitive state |
| [`IKDFNotBuiltIn`](Interfaces/IKDF.cs) | interface | Library-implemented key derivation that can `Clone` itself |
| [`IKMAC`](Interfaces/IMAC.cs) | interface | KMAC message authentication code |
| [`IKMACNotBuiltIn`](Interfaces/IMAC.cs) | interface | Library-implemented KMAC |
| [`IMAC`](Interfaces/IMAC.cs) | interface | Message authentication code: an `IHash` with a settable `Key` and `Clear` |
| [`IMACNotBuiltIn`](Interfaces/IMAC.cs) | interface | Base of the library-implemented MACs |
| [`IPBKDF2HMAC`](Interfaces/IKDF.cs) | interface | PBKDF2 key derivation over an HMAC |
| [`IPBKDF2HMACNotBuiltIn`](Interfaces/IKDF.cs) | interface | Library-implemented PBKDF2-HMAC |
| [`IPBKDFArgon2`](Interfaces/IKDF.cs) | interface | Argon2 key derivation |
| [`IPBKDFArgon2NotBuiltIn`](Interfaces/IKDF.cs) | interface | Library-implemented Argon2 |
| [`IPBKDFBlake3`](Interfaces/IKDF.cs) | interface | BLAKE3 key-derivation mode |
| [`IPBKDFBlake3NotBuiltIn`](Interfaces/IKDF.cs) | interface | Library-implemented BLAKE3 key derivation |
| [`IPBKDFScrypt`](Interfaces/IKDF.cs) | interface | scrypt key derivation |
| [`IPBKDFScryptNotBuiltIn`](Interfaces/IKDF.cs) | interface | Library-implemented scrypt |
| [`IWithKey`](Interfaces/IHashInfo.cs) | interface | Hash with a settable `Key` and its `KeyLength` |
| [`IXOF`](Interfaces/IXOF.cs) | interface | Extendable-output function with a settable output size in bits and `DoOutput` into a span |
| [`IXOFExtensions`](Interfaces/IXOF.cs) | static class | Adds a `DoOutput(byte[], offset, length)` overload to `IXOF` |

The remaining interfaces in `IHashInfo.cs`, such as `IBlockHash`, `ICrypto` and `IHash32`, are internal.

### KDF

| Type | Kind | Purpose |
|---|---|---|
| [`Argon2Parameters`](KDF/PBKDFArgon2NotBuiltIn.cs) | class | Immutable Argon2 settings (type, version, salt, secret, additional data, iterations, memory and lanes) whose byte arrays are returned as copies, with `Clear` |
| [`Argon2ParametersBuilder`](KDF/PBKDFArgon2NotBuiltIn.cs) | class | Fluent builder with `WithSalt`, `WithSecret`, `WithIterations`, `WithMemoryAsKiB`, `WithParallelism`, `WithType`, `WithVersion` and `Build` |

The PBKDF2-HMAC, Argon2, scrypt and BLAKE3 KDF implementations are internal.

### MAC and NullDigest

These folders have no public types. They hold the internal HMAC, KMAC and keyed BLAKE2 implementations, and a null digest that `HashFactory.NullDigestFactory.CreateNullDigest()` returns.

### Params

| Type | Kind | Purpose |
|---|---|---|
| [`Blake2BConfig`](Params/Blake2BParams.cs) | class | BLAKE2b digest size, key, salt and personalization, with `Clear` and `Clone` |
| [`Blake2BTreeConfig`](Params/Blake2BParams.cs) | class | BLAKE2b tree-hashing parameters: fan-out, depth, leaf size, node offset, last-node flag and inner hash size |
| [`Blake2SConfig`](Params/Blake2SParams.cs) | class | BLAKE2s digest size, key, salt and personalization, with `Clear` and `Clone` |
| [`Blake2STreeConfig`](Params/Blake2SParams.cs) | class | BLAKE2s tree-hashing parameters: fan-out, depth, leaf size, node offset, last-node flag and inner hash size |
| [`Blake2XBConfig`](Params/Blake2XBParams.cs) | class | Pairs a `Blake2BConfig` and a `Blake2BTreeConfig` to configure the BLAKE2Xb extendable-output function |
| [`Blake2XSConfig`](Params/Blake2XSParams.cs) | class | Pairs a `Blake2SConfig` and a `Blake2STreeConfig` to configure the BLAKE2Xs extendable-output function |

### Utils

| Type | Kind | Purpose |
|---|---|---|
| [`Converters`](Utils/Converters.cs) | static class | Converts between hexadecimal strings and byte arrays and encodes strings to bytes with a given `Encoding` |

`ArrayUtils`, `Bits` and `PointerUtils` are internal.
