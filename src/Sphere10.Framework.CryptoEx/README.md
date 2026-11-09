# 🔐 Sphere10.Framework.CryptoEx

<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

Sphere10.Framework.CryptoEx adds elliptic-curve cryptography, PascalCoin compatibility and further hash functions to Sphere10 Framework. It provides ECDSA and ECIES on four curves and BIP-340 Schnorr and MuSig signatures on secp256k1, and it registers these schemes and its hash functions with the core `Signers` and `Hashers` registries. Use it when an application signs, verifies or encrypts with elliptic-curve keys, or needs a `CHF` hash function that the core library does not register.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.CryptoEx
```

The package depends on Sphere10.Framework, Sphere10.HashLib4CSharp, BouncyCastle.Cryptography and SauceControl.Blake2Fast. It targets `net10.0`, and also `net11.0` when it is built with a .NET 11 SDK. To build against a Framework source checkout instead, reference `src/Sphere10.Framework.CryptoEx/Sphere10.Framework.CryptoEx.csproj` as [Independent Sphere10 applications](../../docs/guidelines/patterns/new-projects.md#interchangeable-source-and-nuget-references) describes.

### Register the module

`ECDSA` and `Schnorr` work as soon as you construct them. The `DSS.ECDSA_*` and `DSS.SCHNORR_*` entries in `Signers`, and the extra `CHF` values in `Hashers`, exist only after `ModuleConfiguration` registers them. An application built on Sphere10.Framework.Application adds the module at startup:

```csharp
using Sphere10.Framework.Application;

Sphere10Framework.Instance.Build()
	.AddModule<Sphere10.Framework.CryptoEx.ModuleConfiguration>()
	.Start();
```

Code without that host, such as a test fixture, calls `Sphere10.Framework.CryptoEx.ModuleConfiguration.InitializeInternal()` directly. An unregistered scheme or hash function fails with a "No implementation" error.

## ⚡ Quick Start

```csharp
using System;
using System.Text;
using Sphere10.Framework;
using Sphere10.Framework.CryptoEx;
using Sphere10.Framework.CryptoEx.EC;

var ecdsa = new ECDSA(ECDSAKeyType.SECP256K1);
var privateKey = ecdsa.GeneratePrivateKey();
var publicKey = ecdsa.DerivePublicKey(privateKey);

var message = Encoding.UTF8.GetBytes("Hello Blockchain");
var signature = ecdsa.Sign(privateKey, message);
Console.WriteLine(ecdsa.Verify(signature, message, publicKey)); // True

var ciphertext = ecdsa.IES.Encrypt(message, publicKey);
if (ecdsa.IES.TryDecrypt(ciphertext, out var plaintext, privateKey))
	Console.WriteLine(Encoding.UTF8.GetString(plaintext)); // Hello Blockchain
```

`Verify` is an extension method from the `Sphere10.Framework` namespace. Encrypting the same message twice gives different ciphertexts, because each encryption uses a new ephemeral key.

## 🏗️ Core Concepts

CryptoEx implements the core cryptography contracts rather than defining its own. `ECDSA` and `Schnorr` derive from `StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>`, so they implement `IDigitalSignatureScheme`, and `ECDSA.IES` and `Schnorr.IES` return an `IIESAlgorithm`. The registries, the `DSS` and `CHF` enumerations, `VRF` and the hash-based `WAMS` and `WAMSSharp` schemes are part of the [core library](../Sphere10.Framework/README.md#-cryptography).

| Area | Types | Notes |
|---|---|---|
| ECDSA | `ECDSA`, `ECDSAKeyType`, and the fixed-curve subclasses `SECP256k1`, `SECP384R1`, `SECP521R1` and `SECT283K1` | RFC 6979 deterministic nonces, low-S DER signatures and compressed public keys |
| ECIES | `ECDSA.IES`, `Schnorr.IES`, `ECIES`, `PascalCoinIesEngine` | PascalCoin-compatible public-key encryption |
| Schnorr | `Schnorr` | BIP-340 signing, verification and batch verification on secp256k1 |
| MuSig | `MuSigBuilder`, `MuSig`, `MuSigData` | Multi-party Schnorr signatures on secp256k1 |
| Hash functions | `HashLibAdapter`, `Blake2bFastAdapter`, `Blake2sFastAdapter`, `RandomHash2`, `RandomHashFast`, `Murmur3_x86_32Digest` | Extra `CHF` implementations, RandomHash and MurmurHash3 |
| Encodings | `BitcoinBase58Encoding`, `PascalBase58Encoding`, `Pascal64Encoding`, `PascalAsciiEncoding` | Bitcoin Base58 and PascalCoin text encodings |
| Registration | `ModuleConfiguration` | Registers the signature schemes and hash functions |

Most types are in `Sphere10.Framework.CryptoEx` and its `EC`, `EC.Schnorr`, `EC.IES`, `Bitcoin`, `PascalCoin` and `HF` namespaces.

### Curves

| `ECDSAKeyType` | Field | Approximate security | OpenSSL ID |
|---|---|---|---|
| `SECP256K1` | 256-bit prime | 128-bit | 714 |
| `SECP384R1` | 384-bit prime | 192-bit | 715 |
| `SECP521R1` | 521-bit prime | 256-bit | 716 |
| `SECT283K1` | 283-bit binary | 128-bit | 729 |

`Schnorr` and MuSig accept only `SECP256K1`.

## 🔧 Examples

### Deterministic keys

```csharp
using System;
using Sphere10.Framework.CryptoEx;
using Sphere10.Framework.CryptoEx.EC;

var ecdsa = new ECDSA(ECDSAKeyType.SECP256K1);
var seed = Tools.Crypto.GenerateCryptographicallyRandomBytes(32);

var key = ecdsa.GeneratePrivateKey(seed);
var sameKey = ecdsa.GeneratePrivateKey(seed);
Console.WriteLine(key.Equals(sameKey)); // True: the seed determines the key
```

`GeneratePrivateKey(seed)` draws the key from a SHA-256 random generator that is seeded only with `seed`, so the seed is as secret as the key. Signing is deterministic too, because RFC 6979 derives each nonce from the key and the message digest.

### Schnorr signatures

```csharp
using System;
using System.Text;
using Sphere10.Framework.CryptoEx;
using Sphere10.Framework.CryptoEx.EC.Schnorr;

var schnorr = new Schnorr(ECDSAKeyType.SECP256K1);
var privateKey = schnorr.GeneratePrivateKey();
var publicKey = schnorr.DerivePublicKey(privateKey); // 32-byte x-only key

var digest = schnorr.CalculateMessageDigest(Encoding.UTF8.GetBytes("Schnorr signature"));
var signature = schnorr.SignDigest(privateKey, digest);
Console.WriteLine(schnorr.VerifyDigest(signature, digest, publicKey.RawBytes)); // True
```

`SignDigest` mixes 32 random bytes into each signature, as BIP-340 recommends, so repeated signatures of the same digest differ. `SignDigestWithAuxRandomData` takes those bytes from the caller, and `BatchVerifyDigest` checks several signatures together.

### MuSig multi-signatures

Each participant runs a `MuSigBuilder` for the same 32-byte digest and exchanges public keys, then public nonces, then partial signatures with the others. Every builder must receive the public keys in the same order, and `MuSigBuilder.SortPublicKeysInLexicographicOrder` gives the participants a canonical order. This example runs both participants in one process:

```csharp
using System;
using System.Text;
using Sphere10.Framework.CryptoEx;
using Sphere10.Framework.CryptoEx.EC.Schnorr;

var schnorr = new Schnorr(ECDSAKeyType.SECP256K1);
var digest = schnorr.CalculateMessageDigest(Encoding.UTF8.GetBytes("Joint statement"));
var signers = new[] {
	new MuSigBuilder(schnorr.GeneratePrivateKey(), digest),
	new MuSigBuilder(schnorr.GeneratePrivateKey(), digest)
};

foreach (var builder in signers)
	foreach (var signer in signers)
		builder.AddPublicKey(signer.PublicKey);
foreach (var builder in signers)
	foreach (var signer in signers)
		builder.AddPublicNonce(signer.PublicKey, signer.PublicNonce);
foreach (var builder in signers)
	foreach (var signer in signers)
		builder.AddPartialSignature(signer.PublicKey, signer.PartialSignature);

signers[0].VerifyPartialSignatures();
var result = signers[0].BuildAggregatedSignature();
Console.WriteLine(schnorr.VerifyDigest(result.AggregatedSignature, digest, result.AggregatedPublicKey)); // True
```

`VerifyPartialSignatures` throws when a participant's partial signature is invalid. A builder signs once, so create a new builder for every signing session. The optional third constructor argument is a 32-byte session ID, and the builder generates a random one when it is omitted.

### Signature schemes through `Signers`

```csharp
using System;
using System.Text;
using Sphere10.Framework;

var message = Encoding.UTF8.GetBytes("Important message");
foreach (var dss in new[] { DSS.ECDSA_SECP256k1, DSS.ECDSA_SECP384R1, DSS.SCHNORR_SECP256k1, DSS.PQC_WAMS }) {
	var privateKey = Signers.GeneratePrivateKey(dss);
	var publicKey = Signers.DerivePublicKey(dss, privateKey, 0UL);
	var signature = Signers.Sign(dss, privateKey, message, 0UL);
	Console.WriteLine($"{dss}: {Signers.Verify(dss, signature, message, publicKey)}"); // True
}
```

The ECDSA and Schnorr entries need the registered module, whereas `DSS.PQC_WAMS` and `DSS.PQC_WAMSSharp` come from the core library. The last argument of `DerivePublicKey` and `Sign` is the signer nonce, which stateless schemes such as ECDSA and Schnorr ignore.

### Verifiable random function

```csharp
using System;
using Sphere10.Framework;

var vrf = VRF.CreateCryptographicVRF(CHF.SHA2_256, DSS.ECDSA_SECP256k1);
var privateKey = Signers.GeneratePrivateKey(DSS.ECDSA_SECP256k1);
var publicKey = Signers.DerivePublicKey(DSS.ECDSA_SECP256k1, privateKey, 0UL);

var seed = new byte[] { 1, 2, 3, 4 };
var output = vrf.Run(seed, privateKey, 0UL, out var proof);
Console.WriteLine(vrf.TryVerify(seed, output, proof, publicKey)); // True
```

`CryptographicVRF` signs the digest of the seed to form the proof, and it hashes the proof to form the output. A verifier accepts any valid signature, so a dishonest ECDSA or Schnorr signer can choose another nonce, produce a different valid proof and so obtain a different output. Do not rely on the output being the only one that a signer can produce.

## 🔒 PascalCoin ECIES Compatibility

`ECDSA.IES` returns an `ECIES` instance that writes PascalCoin's OpenSSL ECIES format. The format uses ECDH, SHA-512 key derivation, AES-256-CBC with a zero IV, HMAC-MD5 over the ciphertext, and a six-byte header that records the original message length.

- Encryption accepts messages of up to 32,000 bytes and zero-fills only a partial final block.
- Decryption returns exactly the recorded length, so trailing zero bytes survive. It also reads historical managed ciphertexts, including those with an extra block after an aligned message and those whose 16-bit lengths wrapped.
- Upgrade every reader before switching writers, because older managed readers can strip genuine trailing zeros from aligned messages in the native format.
- The MAC does not cover the header, so a successful decryption does not prove that the returned length is authentic. Authenticate the complete encrypted envelope before trusting the plaintext, as a verified PascalCoin transaction signature does.
- When you construct `PascalCoinIesEngine` yourself, pass an unpadded cipher such as `new BufferedBlockCipher(new CbcBlockCipher(new AesEngine()))`. The engine pads the message itself and rejects a `PaddedBufferedBlockCipher`.

[PascalCoin ECIES compatibility](PascalCoin/ECIES-compatibility.md) describes the compatibility defects, their fixes and the remaining native issues.

## 🧮 Hash Functions

The core library registers SHA-1, SHA2-256, SHA2-384, SHA2-512 and BLAKE2b with `Hashers`. `ModuleConfiguration` registers the other `CHF` values, mostly through HashLib4CSharp:

- RIPEMD, RIPEMD-128, RIPEMD-160, RIPEMD-256 and RIPEMD-320.
- BLAKE2b, replacing the core registrations, and BLAKE2s, plus the `_Fast` variants that Blake2Fast implements.
- SHA-0, SHA2-224, SHA2-512/224 and SHA2-512/256, SHA3-224 to SHA3-512, and Keccak-224 to Keccak-512.
- GOST, GOST 34.11-2012 (256 and 512 bits), Grindahl-256, Grindahl-512, HAS-160, Haval, MD2, MD4, MD5, Panama, RadioGatun32, Snefru, Tiger, Tiger2 and Whirlpool.

In a WebAssembly executable, the module also registers HashLib4CSharp versions of SHA-1 and SHA-2, because the core library skips them there.

`RandomHash2.Compute` and `RandomHashFast.Compute` hash a block header with the RandomHash algorithms. `RandomHash2` draws on many of the functions above, so it needs the module registration. `Murmur3_x86_32Digest` provides the non-cryptographic MurmurHash3 (x86, 32-bit) as a BouncyCastle `IDigest`.

## 🔤 Encodings

`BitcoinBase58Encoding` follows Bitcoin Core's Base58 implementation. `PascalBase58Encoding` encodes and decodes PascalCoin's Base58 strings. `Pascal64Encoding` and `PascalAsciiEncoding` validate, escape and unescape PascalCoin's Pascal64 and PascalAscii strings.

## ⚠️ Security Considerations

- Keep private keys and key seeds secret, and never log them. Generate seeds with a cryptographically secure source such as `Tools.Crypto.GenerateCryptographicallyRandomBytes`.
- `ECDSA` accepts only compressed public keys, and its verification rejects high-S signatures.
- `ECIES` accepts `ECDSA` and `Schnorr` keys, and its MAC does not authenticate the message header.
- `ModuleConfiguration` registers Schnorr only as `DSS.SCHNORR_SECP256k1`. The `DSS.SCHNORR_SECP384R1`, `DSS.SCHNORR_SECP521R1` and `DSS.SCHNORR_SECT283K1` values have no registered scheme, because `Schnorr` implements BIP-340 on secp256k1 only.
- The elliptic-curve schemes are not post-quantum. The core library's hash-based `DSS.PQC_WAMS` and `DSS.PQC_WAMSSharp` schemes are reference implementations, so review them before production use.
- MD2, MD4, MD5, SHA-0 and SHA-1 are broken for collision resistance, and MurmurHash3 is not cryptographic. Use SHA-2, SHA-3 or BLAKE2 for commitments.

## 🧪 Tests

The [CryptoEx tests](../../tests/Sphere10.Framework.CryptoEx.Tests/) cover ECDSA, ECIES and its PascalCoin compatibility fixtures, BIP-340 Schnorr vectors, MuSig, VRF, RandomHash, the encodings and digest consistency. Their `RegisterCryptoExModule` setup fixture registers the module before the tests run. Run the project from the repository root:

```powershell
dotnet test ./tests/Sphere10.Framework.CryptoEx.Tests/Sphere10.Framework.CryptoEx.Tests.csproj --configuration Release
```

GitHub Actions runs this test assembly in its test job.

## 📁 Project Structure

| Path | Contents |
|---|---|
| `EC/` | `ECDSA`, `ECDSAKeyType`, the fixed-curve subclasses, the low-S signer and the strict DER encoding |
| `EC/IES/` | `ECIES` and its public-key parser |
| `EC/Schnorr/` | `Schnorr`, `MuSig`, `MuSigBuilder` and the MuSig session types |
| `IES/` | The IES cipher, engine and parameter types that `ECIES` builds on |
| `PascalCoin/` | `PascalCoinIesEngine`, its key-derivation function, the PascalCoin encodings and the [ECIES compatibility notes](PascalCoin/ECIES-compatibility.md) |
| `Bitcoin/` | `BitcoinBase58Encoding` |
| `CHF/` | HashLib4CSharp and Blake2Fast adapters, Grindahl, Haval and RadioGatun digests, and RandomHash |
| `HF/` | `Murmur3_x86_32Digest` |
| `Attributes/` | Curve metadata attributes for `ECDSAKeyType` |
| `Misc/` | `BigIntegerUtils` and `Pack` helpers |
| `ModuleConfiguration.cs` | Registration of the signature schemes and hash functions |

## 📖 Related Documentation

- [Core cryptography](../Sphere10.Framework/README.md#-cryptography), which owns `Signers`, `Hashers`, `VRF` and the WAMS schemes.
- [Technical papers](../../README.md#technical-papers) on the AMS, WAMS and Winternitz signature schemes.

## ⚖️ License

Distributed under the **MIT License**.

See [LICENSE](../../LICENSE) for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer
