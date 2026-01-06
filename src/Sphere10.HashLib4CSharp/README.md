# HashLib4CSharp

**Comprehensive hashing library** providing an easy-to-use interface for computing hashes and checksums of strings, files, streams, byte arrays, and untyped data.

## 📦 Installation

```bash
dotnet add package Sphere10.HashLib4CSharp
```

## ⚡ 10-Second Example

```csharp
using HashLib4CSharp.Base;

// Hash a string with SHA-256
var hash = HashFactory.Crypto.CreateSHA2_256().ComputeString("hello world");
Console.WriteLine(hash.ToString());  // Hex output

// Hash a file
var fileHash = HashFactory.Crypto.CreateSHA3_256().ComputeFile("data.bin");

// Hash bytes
byte[] data = new byte[] { 1, 2, 3, 4, 5 };
var bytesHash = HashFactory.Crypto.CreateBlake2B_256().ComputeBytes(data);
```

## 🏗️ Core Features

- **Multiple Algorithms**: MD5, SHA-1, SHA-2, SHA-3, BLAKE2, BLAKE3, Keccak, and 100+ more
- **Checksums**: CRC-32, Adler-32, and customizable CRC models
- **Non-Cryptographic**: MurmurHash, XXHash, SipHash, FNV, and more
- **Key Derivation**: PBKDF2, Argon2, Scrypt, BLAKE3-KDF
- **MACs**: HMAC, KMAC, Blake2MAC
- **XOF (Extendable Output)**: SHAKE, CShake, Blake2X, Blake3XOF, KMAC-XOF
- **Unified Interface**: Same API for all algorithms
- **Streaming**: Process large files efficiently
- **Adapters**: Integrate with `System.Security.Cryptography`

## 🔧 Core Examples

### Cryptographic Hashes

```csharp
using HashLib4CSharp.Base;

// SHA family
var sha1 = HashFactory.Crypto.CreateSHA1().ComputeString("data");
var sha256 = HashFactory.Crypto.CreateSHA2_256().ComputeString("data");
var sha384 = HashFactory.Crypto.CreateSHA2_384().ComputeString("data");
var sha512 = HashFactory.Crypto.CreateSHA2_512().ComputeString("data");

// SHA-3 family
var sha3_256 = HashFactory.Crypto.CreateSHA3_256().ComputeString("data");
var sha3_512 = HashFactory.Crypto.CreateSHA3_512().ComputeString("data");

// BLAKE family
var blake2b = HashFactory.Crypto.CreateBlake2B_256().ComputeString("data");
var blake2s = HashFactory.Crypto.CreateBlake2S_256().ComputeString("data");
var blake3 = HashFactory.Crypto.CreateBlake3_256().ComputeString("data");

// Keccak
var keccak = HashFactory.Crypto.CreateKeccak_256().ComputeString("data");

// Legacy (not for security)
var md5 = HashFactory.Crypto.CreateMD5().ComputeString("data");
```

### Checksums

```csharp
using HashLib4CSharp.Base;

// CRC-32
var crc32 = HashFactory.Checksum.CRC.CreateCrc32PKZip().ComputeString("data");

// Adler-32
var adler = HashFactory.Checksum.CreateAdler32().ComputeString("data");
```

### Non-Cryptographic Hashes (Fast)

```csharp
using HashLib4CSharp.Base;

// XXHash - extremely fast
var xxh32 = HashFactory.Hash32.CreateXXHash32().ComputeString("data");
var xxh64 = HashFactory.Hash64.CreateXXHash64().ComputeString("data");

// MurmurHash
var murmur2 = HashFactory.Hash32.CreateMurmur2().ComputeString("data");
var murmur3_32 = HashFactory.Hash32.CreateMurmurHash3_x86_32().ComputeString("data");
var murmur3_128 = HashFactory.Hash128.CreateMurmurHash3_x64_128().ComputeString("data");

// SipHash
var sip64 = HashFactory.Hash64.CreateSipHash64_2_4().ComputeString("data");
var sip128 = HashFactory.Hash128.CreateSipHash128_2_4().ComputeString("data");

// FNV
var fnv = HashFactory.Hash32.CreateFNV().ComputeString("data");
var fnv64 = HashFactory.Hash64.CreateFNV64().ComputeString("data");
```

### HMAC (Hash-Based Message Authentication)

```csharp
using HashLib4CSharp.Base;

byte[] key = Encoding.UTF8.GetBytes("secret-key");

// Create HMAC from any hash algorithm
var hmac = HashFactory.HMAC.CreateHMAC(
    HashFactory.Crypto.CreateSHA2_256(), 
    key);

var result = hmac.ComputeString("message");
```

### Key Derivation Functions (KDF)

```csharp
using HashLib4CSharp.Base;

byte[] password = Encoding.UTF8.GetBytes("password");
byte[] salt = Encoding.UTF8.GetBytes("salt");

// PBKDF2-HMAC
var pbkdf2 = HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC(
    HashFactory.Crypto.CreateSHA2_256(),
    password,
    salt,
    iterations: 10000);
byte[] derivedKey = pbkdf2.GetBytes(32);  // 32 bytes

// Scrypt
var scrypt = HashFactory.KDF.PBKDFScrypt.CreatePBKDFScrypt(
    password, salt,
    cost: 16384,
    blockSize: 8,
    parallelism: 1);
byte[] scryptKey = scrypt.GetBytes(32);
```

### Streaming Large Files

```csharp
using HashLib4CSharp.Base;

var hasher = HashFactory.Crypto.CreateSHA2_256();

// Method 1: ComputeFile (convenience)
var hash1 = hasher.ComputeFile("large_file.bin");

// Method 2: ComputeStream
using var stream = File.OpenRead("large_file.bin");
var hash2 = hasher.ComputeStream(stream);

// Method 3: Incremental (for progress tracking)
hasher.Initialize();
var buffer = new byte[8192];
int bytesRead;
while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
    hasher.TransformBytes(buffer, 0, bytesRead);
}
var hash3 = hasher.TransformFinal();
```

### System.Security.Cryptography Adapters

```csharp
using HashLib4CSharp.Base;
using System.Security.Cryptography;

// Wrap as HashAlgorithm
HashAlgorithm adapter = HashFactory.Adapter.CreateHashAlgorithmFromHash(
    HashFactory.Crypto.CreateSHA2_256());

// Use with standard .NET APIs
byte[] hash = adapter.ComputeHash(Encoding.UTF8.GetBytes("data"));
```

## 📋 Algorithm Reference

### Cryptographic Hashes

| Family | Algorithms |
|--------|------------|
| SHA-1 | SHA-1 |
| SHA-2 | SHA-224, SHA-256, SHA-384, SHA-512, SHA-512/224, SHA-512/256 |
| SHA-3 | SHA3-224, SHA3-256, SHA3-384, SHA3-512 |
| BLAKE2 | Blake2B (128-512), Blake2S (128-256), Blake2BP, Blake2SP |
| BLAKE3 | Blake3-256 |
| Keccak | Keccak-224, 256, 288, 384, 512 |
| MD | MD2, MD4, MD5 |
| RIPEMD | RIPEMD, RIPEMD-128, 160, 256, 320 |
| Whirlpool | Whirlpool |
| Tiger | Tiger/Tiger2 (3/4/5 rounds) |
| GOST | GOST, GOST 2012 |

### Non-Cryptographic Hashes

| Size | Algorithms |
|------|------------|
| 32-bit | FNV, FNV1a, Jenkins3, Murmur2, MurmurHash3, XXHash32, and more |
| 64-bit | FNV64, FNV1a64, Murmur2_64, SipHash64, XXHash64 |
| 128-bit | SipHash128, MurmurHash3_x86_128, MurmurHash3_x64_128 |

### Key Derivation

| KDF | Description |
|-----|-------------|
| PBKDF2-HMAC | Password-Based KDF with any HMAC |
| Argon2 | Memory-hard password hashing |
| Scrypt | Memory-hard KDF |
| BLAKE3-KDF | Fast BLAKE3-based key derivation |

## 📦 Dependencies

- Core .NET Framework only (no external dependencies)

## 📜 Credits

This library was developed by **Ugochukwu Mmaduekwe** ([@Xor-el](https://github.com/Xor-el)) and sponsored by **Sphere 10 Software**.

## 📖 Related Projects

- [Sphere10.Framework](../Sphere10.Framework) - Uses HashLib for cryptographic operations
- [Sphere10.Framework.CryptoEx](../Sphere10.Framework.CryptoEx) - Extended cryptography

## ⚖️ License

Distributed under the **MIT License**.

## 👤 Author

**Ugochukwu Mmaduekwe** - [@Xor-el](https://github.com/Xor-el)



