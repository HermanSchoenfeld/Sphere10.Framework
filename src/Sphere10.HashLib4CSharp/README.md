# HashLib4CSharp

Sphere10.HashLib4CSharp is Sphere 10 Software's fork of HashLib4CSharp, Ugochukwu Mmaduekwe's hashing library. It computes cryptographic hashes, checksums, non-cryptographic hashes, MACs, key derivations and extendable-output functions over strings, byte arrays, spans, streams, files and unmanaged memory, through one `IHash` interface.

In Sphere10 Framework applications, Sphere10.Framework.CryptoEx registers many of these algorithms with the core `Hashers` registry through its `HashLibAdapter`, so Framework code can select them by `CHF` value.

## 📦 Installation

```bash
dotnet add package Sphere10.HashLib4CSharp
```

The library targets .NET Standard 2.1. Its namespaces keep the upstream `HashLib4CSharp.*` names, and the compute and transform extension methods are in `HashLib4CSharp.Interfaces`.

## ⚡ 10-Second Example

```csharp
using System.Text;
using HashLib4CSharp.Base;
using HashLib4CSharp.Interfaces;

// SHA-256 of a UTF-8 string, as upper-case hexadecimal
string hex = HashFactory.Crypto.CreateSHA2_256().ComputeString("hello world", Encoding.UTF8).ToString();

// SHA3-256 of a file
IHashResult fileHash = HashFactory.Crypto.CreateSHA3_256().ComputeFile("data.bin");

// BLAKE2b-256 of a byte array
byte[] digest = HashFactory.Crypto.CreateBlake2B_256().ComputeBytes(new byte[] { 1, 2, 3, 4, 5 }).GetBytes();
```

An `IHashResult` returns the digest through `GetBytes()`, `ToString()`, `ToBase64String()` and integer accessors such as `GetUInt32()`. `ToString()` gives upper-case hexadecimal, and `ToString(true)` separates the bytes with dashes. `Equals` compares two results in constant time.

## 🏗️ Core Features

- **Cryptographic hashes**: SHA-0, SHA-1, SHA-2, SHA-3, Keccak, BLAKE2, BLAKE3, MD2, MD4, MD5, RIPEMD, Tiger, Tiger2, HAVAL, GOST, Whirlpool and others, listed in the reference below
- **Checksums**: Adler-32, CRC-32 (PKZip and Castagnoli) and any `CRCModel`
- **Non-cryptographic hashes**: 32-, 64- and 128-bit functions, including MurmurHash, xxHash, SipHash and FNV
- **Key derivation**: PBKDF2-HMAC, Argon2, scrypt and the BLAKE3 key-derivation mode
- **MACs**: HMAC over any hash, KMAC, and keyed BLAKE2b and BLAKE2s
- **Extendable-output functions (XOF)**: SHAKE, cSHAKE, BLAKE2X, BLAKE3 XOF and KMAC XOF
- **One interface**: every algorithm is an `IHash` with `Initialize`, `TransformByteSpan`, `TransformFinal` and `Clone`
- **Inputs**: strings with an explicit `Encoding`, byte arrays, spans, streams, files and unmanaged memory, with asynchronous stream and file overloads
- **Adapters**: wrappers that expose a hash as a `HashAlgorithm`, an HMAC as a `System.Security.Cryptography.HMAC` and a KDF as `DeriveBytes`

## 🔧 Core Examples

### Cryptographic Hashes

```csharp
using System.Text;
using HashLib4CSharp.Base;
using HashLib4CSharp.Interfaces;

var sha256 = HashFactory.Crypto.CreateSHA2_256().ComputeString("data", Encoding.UTF8);
var sha3 = HashFactory.Crypto.CreateSHA3_512().ComputeString("data", Encoding.UTF8);
var blake3 = HashFactory.Crypto.CreateBlake3_256().ComputeString("data", Encoding.UTF8);
var keccak = HashFactory.Crypto.CreateKeccak_256().ComputeString("data", Encoding.UTF8);

// Legacy algorithms such as MD5 are not suitable for security purposes
var md5 = HashFactory.Crypto.CreateMD5().ComputeString("data", Encoding.UTF8);
```

### Checksums

```csharp
using HashLib4CSharp.Base;
using HashLib4CSharp.Checksum;
using HashLib4CSharp.Interfaces;

byte[] data = { 1, 2, 3 };
var crc32 = HashFactory.Checksum.CRC.CreateCrc32PKZip().ComputeBytes(data);
var adler32 = HashFactory.Checksum.CreateAdler32().ComputeBytes(data);
var crc64 = HashFactory.Checksum.CRC.CreateCRC(CRCModel.CRC64_XZ).ComputeBytes(data);
```

`CRCModel` provides predefined parameter sets, such as `CRCModel.CRC32_BZIP2` and `CRCModel.CRC64_XZ`. `CreateCRC` also accepts a custom `CRCModel` with its own `Width`, `Polynomial`, `InitialValue`, `ReflectIn`, `ReflectOut` and `XorOut`.

### Non-Cryptographic Hashes

```csharp
using System.Text;
using HashLib4CSharp.Base;
using HashLib4CSharp.Interfaces;

var xxh64 = HashFactory.Hash64.CreateXXHash64().ComputeString("data", Encoding.UTF8);
var murmur128 = HashFactory.Hash128.CreateMurmurHash3_x64_128().ComputeString("data", Encoding.UTF8);
var fnv1a = HashFactory.Hash32.CreateFNV1a().ComputeString("data", Encoding.UTF8);

// Keyed functions return IHashWithKey
IHashWithKey sipHash = HashFactory.Hash64.CreateSipHash64_2_4();
sipHash.Key = new byte[16];
var sip = sipHash.ComputeString("data", Encoding.UTF8);
```

### HMAC (Hash-Based Message Authentication)

```csharp
using System.Text;
using HashLib4CSharp.Base;
using HashLib4CSharp.Interfaces;

byte[] key = Encoding.UTF8.GetBytes("secret-key");
var hmac = HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA2_256(), key);
var mac = hmac.ComputeString("message", Encoding.UTF8);
```

### Key Derivation Functions (KDF)

```csharp
using System.Text;
using HashLib4CSharp.Base;

byte[] password = Encoding.UTF8.GetBytes("password");
byte[] salt = Encoding.UTF8.GetBytes("salt");

var pbkdf2 = HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC(HashFactory.Crypto.CreateSHA2_256(), password, salt, iterations: 10000);
byte[] derivedKey = pbkdf2.GetBytes(32);

var scrypt = HashFactory.KDF.PBKDFScrypt.CreatePBKDFScrypt(password, salt, cost: 16384, blockSize: 8, parallelism: 1);
byte[] scryptKey = scrypt.GetBytes(32);
```

`HashFactory.KDF.PBKDFArgon2.CreatePBKDFArgon2(password, parameters)` takes an `Argon2Parameters` object, which `Argon2ParametersBuilder.Build()` creates. Every KDF also has `GetBytesAsync`, and `Clear()` wipes its sensitive state.

### Streaming Large Files

```csharp
using System.IO;
using HashLib4CSharp.Base;
using HashLib4CSharp.Interfaces;

var hasher = HashFactory.Crypto.CreateSHA2_256();

// One call
IHashResult fromFile = hasher.ComputeFile("large_file.bin");

// Incremental, for example to report progress
hasher.Initialize();
using (var stream = File.OpenRead("large_file.bin")) {
    var buffer = new byte[81920];
    int read;
    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        hasher.TransformBytes(buffer, 0, read);
}
IHashResult incremental = hasher.TransformFinal();
```

`ComputeStream`, `ComputeFileAsync` and `ComputeStreamAsync` cover the other cases, and the file methods accept an offset and a length. An `IHash` keeps its state between `Initialize` and `TransformFinal`, so give each concurrent computation its own instance, for example through `Clone()`.

### System.Security.Cryptography Adapters

```csharp
using System.Security.Cryptography;
using System.Text;
using HashLib4CSharp.Base;

HashAlgorithm adapter = HashFactory.Adapter.CreateHashAlgorithmFromHash(HashFactory.Crypto.CreateSHA2_256());
byte[] hash = adapter.ComputeHash(Encoding.UTF8.GetBytes("data"));
```

`CreateHMACFromHMACNotBuiltIn` and `CreateDeriveBytesFromKDFNotBuiltIn` adapt HMACs and KDFs in the same way.

## 📋 Algorithm Reference

### Cryptographic Hashes

`HashFactory.Crypto` creates these algorithms. `CreateBlake2B` and `CreateBlake2S` also accept a configuration with the digest size, key, salt and personalization.

| Family | Algorithms |
|--------|------------|
| SHA-0, SHA-1 | SHA-0, SHA-1 |
| SHA-2 | SHA-224, SHA-256, SHA-384, SHA-512, SHA-512/224, SHA-512/256 |
| SHA-3 | SHA3-224, SHA3-256, SHA3-384, SHA3-512 |
| Keccak | Keccak-224, 256, 288, 384, 512 |
| BLAKE2 | Blake2B (128 to 512 bits), Blake2S (128 to 256 bits), Blake2BP, Blake2SP |
| BLAKE3 | Blake3-256, unkeyed or keyed |
| MD | MD2, MD4, MD5 |
| RIPEMD | RIPEMD, RIPEMD-128, 160, 256, 320 |
| Tiger | Tiger and Tiger2, with 3, 4 or 5 rounds and 128-, 160- or 192-bit output |
| HAVAL | 3, 4 or 5 passes, with 128-, 160-, 192-, 224- or 256-bit output |
| GOST | GOST, GOST 3411-2012 (256 and 512 bits) |
| Others | Whirlpool, Grindahl-256, Grindahl-512, HAS-160, Panama, RadioGatun-32, RadioGatun-64, Snefru-8 (128 and 256 bits) |

### Non-Cryptographic Hashes

| Size | Factory | Algorithms |
|------|---------|------------|
| 32-bit | `HashFactory.Hash32` | AP, Bernstein, Bernstein1, BKDR, DEK, DJB, ELF, FNV, FNV1a, Jenkins3, JS, Murmur2, MurmurHash3_x86_32, OneAtTime, PJW, Rotating, RS, SDBM, ShiftAndXor, SuperFast, XXHash32 |
| 64-bit | `HashFactory.Hash64` | FNV64, FNV1a64 (`CreateFNV1a64`; the misspelled `CreateFV1a64` remains as an obsolete alias), Murmur2_64, SipHash64_2_4, XXHash64 |
| 128-bit | `HashFactory.Hash128` | SipHash128_2_4, MurmurHash3_x86_128, MurmurHash3_x64_128 |

The Murmur, xxHash and SipHash functions are keyed and return `IHashWithKey`.

### MACs, Key Derivation and XOFs

| Kind | Factory methods |
|------|-----------------|
| HMAC | `HashFactory.HMAC.CreateHMAC(hash, key)` |
| KMAC | `HashFactory.KMAC.CreateKMAC128`, `CreateKMAC256` |
| Keyed BLAKE2 | `HashFactory.Blake2BMAC.CreateBlake2BMAC`, `HashFactory.Blake2SMAC.CreateBlake2SMAC` |
| PBKDF2-HMAC | `HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC` |
| Argon2 | `HashFactory.KDF.PBKDFArgon2.CreatePBKDFArgon2` |
| scrypt | `HashFactory.KDF.PBKDFScrypt.CreatePBKDFScrypt` |
| BLAKE3 key derivation | `HashFactory.KDF.PBKDFBlake3.CreatePBKDFBlake3` |
| XOF | `HashFactory.XOF.CreateShake_128`, `CreateShake_256`, `CreateCShake_128`, `CreateCShake_256`, `CreateBlake2XB`, `CreateBlake2XS`, `CreateBlake3XOF`, `CreateKMAC128XOF`, `CreateKMAC256XOF` |
| Adapters | `HashFactory.Adapter.CreateHashAlgorithmFromHash`, `CreateHMACFromHMACNotBuiltIn`, `CreateDeriveBytesFromKDFNotBuiltIn` |
| Null digest | `HashFactory.NullDigestFactory.CreateNullDigest` |

## 📦 Dependencies

- **System.Runtime.CompilerServices.Unsafe**: the only package dependency

## 📜 Credits

This library was developed by **Ugochukwu Mmaduekwe** ([@Xor-el](https://github.com/Xor-el)) and sponsored by **Sphere 10 Software**. The upstream project is [Xor-el/HashLib4CSharp](https://github.com/Xor-el/HashLib4CSharp).

## 📖 Related Projects

- [Sphere10.Framework.CryptoEx](../Sphere10.Framework.CryptoEx/README.md) - Registers HashLib algorithms with the Framework's `Hashers` registry through `HashLibAdapter`
- [Sphere10.Framework](../Sphere10.Framework/README.md) - The core `Hashers` registry and the `CHF` algorithm identifiers
- [Sphere10.HashLib4CSharp.Tests](../../tests/Sphere10.HashLib4CSharp.Tests) - NUnit tests for the algorithms, which run with `dotnet test tests/Sphere10.HashLib4CSharp.Tests/Sphere10.HashLib4CSharp.Tests.csproj`

## ⚖️ License

Distributed under the **MIT License**, as each source file header states (copyright 2020 Ugochukwu Mmaduekwe). The package metadata keeps the upstream license expression `Apache-2.0 OR MIT`.

## 👤 Author

**Ugochukwu Mmaduekwe** - [@Xor-el](https://github.com/Xor-el)



