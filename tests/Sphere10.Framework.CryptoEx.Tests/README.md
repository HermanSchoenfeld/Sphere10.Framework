# Sphere10.Framework.CryptoEx.Tests

NUnit tests for [Sphere10.Framework.CryptoEx](../../src/Sphere10.Framework.CryptoEx/README.md). They cover ECDSA, ECIES, BIP-340 Schnorr and MuSig, PascalCoin-compatible ECIES and text encodings, verifiable random functions and the extra hash functions that CryptoEx registers with `Hashers`. The project references `Sphere10.Framework`, `Sphere10.Framework.NUnit` and the BouncyCastle and Newtonsoft.Json packages.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.CryptoEx.Tests/Sphere10.Framework.CryptoEx.Tests.csproj -c Release -p:BuildRevision=0
```

Run the PascalCoin ECIES fixtures together:

```powershell
dotnet test tests/Sphere10.Framework.CryptoEx.Tests/Sphere10.Framework.CryptoEx.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~PascalCoinEcies|FullyQualifiedName~PascalCoinIesEngineTests"
```

Run the signature fixtures without the OpenSSL comparison:

```powershell
dotnet test tests/Sphere10.Framework.CryptoEx.Tests/Sphere10.Framework.CryptoEx.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~ECDSATests|FullyQualifiedName~Schnorr|FullyQualifiedName~MuSig"
```

`RegisterCryptoExModule` is an NUnit set-up fixture. It initializes the CryptoEx module before the fixtures in its namespace run and finalizes it afterwards, so filtered runs still see the registered `CHF` and `DSS` values.

## Requirements

`OpenSSLConsistencyTests` runs only on Windows and outside GitHub Actions. It extracts `PascalOpenSSL.exe` and the OpenSSL 1.1 `libcrypto` libraries from the project resources into a temporary folder and starts that executable as a process. Elsewhere its cases report as inconclusive. Every other fixture runs in-process without a network or database.

The [Resources](Resources) folder holds the embedded test inputs. These are the BIP-340 vectors, the PascalCoin ECIES compatibility, encryption and oversized-message vectors, and the OpenSSL helper binaries. [Properties/Resource.resx](Properties/Resource.resx) exposes the BIP-340 vectors and the binaries as typed resources.

## Contents

### Project root

All fixtures, their abstract bases and the set-up fixture.

| Fixture | Purpose |
| --- | --- |
| [Bip340SchnorrTest](Bip340SchnorrTest.cs) | BIP-340 Schnorr signing and verification against the embedded vectors, batch verification, random sign and verify, and private and public key parsing on each key type. |
| [Bip340SchnorrTestData](Bip340SchnorrTest.cs) | JSON row of the BIP-340 vector file. It contains no tests. |
| [BitcoinBase58EncodingTests](BitcoinBase58EncodingTests.cs) | `BitcoinBase58Encoding` encoding, decoding and rejection of invalid strings. |
| [Blake2bFastConsistency](Blake2bFastConsistency.cs) | The `_Fast` BLAKE2b and BLAKE2s `CHF` values returning the same digests as the standard values for empty, random and composite inputs. |
| [Blake2bSlowConsistency](Blake2bSlowConsistency.cs) | The core `Sphere10.Framework.Blake2b` hasher returning the same results as `CHF.Blake2b_512_Fast`. |
| [CryptoSpanTests](CryptoSpanTests.cs) | Schnorr unsigned big-endian integer conversion, HashLib hashing of slices without copying, and ECDSA and Schnorr operations on span slices. |
| [DigestTest](DigestTestBase.cs) | Abstract base, adapted from the BouncyCastle `DigestTest`, that checks an `IDigest` against message and digest vectors. |
| [ECDSATests](ECDSATests.cs) | ECDSA sign and verify on each curve, scheme traits, private and public key parsing, low-S and strict-DER malleability rules, and seeded key generation. |
| [ECIESSecurityTests](ECIESSecurityTests.cs) | ECIES rejection of flipped, truncated, extended, zeroed, spliced, cross-curve and MAC-modified ciphertexts and wrong keys, randomized encryption and round trips across message sizes. |
| [ECIESTests](ECIESTests.cs) | Basic and repeated random ECIES encrypt and decrypt round trips on each curve. |
| [Grindahl512DigestTest](Grindahl512DigestTestBase.cs) | Grindahl-512 digests against reference vectors. |
| [HashTestBase](HashTestBase.cs) | Abstract base holding a shared input and expected digest tables for hash fixtures. |
| [Haval5_256DigestTest](HavalDigestTest.cs) | HAVAL 5-pass 256-bit digests against reference vectors. |
| [MuSigBuilderTest](MuSigBuilderTest.cs) | The MuSig builder's public-key ordering, known vectors, random inputs and detection of a dishonest participant. |
| [MuSigTest](MuSigTest.cs) | Complete MuSig signing against known vectors and with random keys. |
| [Murmur3_x86_32DigestTest](Murmur3X8632DigestTestBase.cs) | Murmur3 x86 32-bit digests and their `UInt32` values. |
| [OpenSSLConsistencyTests](OpenSSLConsistencyTests.cs) | ECDSA signatures verified in both directions against OpenSSL, and OpenSSL rejection of bad signatures. |
| [Pascal64EncodingTests](Pascal64EncodingTests.cs) | Pascal64 start and next characters, string patterns, escaping, length limits and escape round trips. |
| [PascalAsciiEncodingTests](PascalAsciiEncodingTests.cs) | PascalCoin ASCII character and string patterns, escaping and escape round trips. |
| [PascalBase58EncodingTests](PascalBase58EncodingTests.cs) | `PascalBase58Encoding` compatibility with a key encoded by the Pascal implementation. |
| [PascalCoinEciesCompatibilityTests](PascalCoinEciesCompatibilityTests.cs) | Decryption of captured native ciphertexts, the native header for binary messages, the native size limit, malformed framing, authenticated bytes and the unauthenticated original-length field. |
| [PascalCoinEciesEncryptionVectorTests](PascalCoinEciesEncryptionVectorTests.cs) | Encryption with a fixed ephemeral key matching OpenSSL-verified ciphertexts. |
| [PascalCoinEciesLegacyLengthTests](PascalCoinEciesLegacyLengthTests.cs) | Decryption of oversized ciphertexts from the earlier managed implementation without losing bytes. |
| [PascalCoinEciesTest](PascalCoinEciesTest.cs) | Decryption of PascalCoin testnet wallet vectors and encrypt and decrypt round trips. |
| [PascalCoinIesEngineTests](PascalCoinIesEngineTests.cs) | The PascalCoin IES engine's unpadded-cipher rule, input offsets, MAC verification before decryption and range and null-input validation. |
| [RaceConditionTests](RaceConditionTests.cs) | Parallel hashing with every `CHF` value on empty and random inputs, including repeated `GetResult` calls. |
| [RadioGatun32DigestTest](RadioGatun32DigestTestBase.cs) | RadioGatun[32] digests against reference vectors. |
| [RandomHashFastTest](RandomHashTests.cs) | `RandomHashFast.Compute` against the RandomHash vectors. |
| [RandomHashTests](RandomHashTests.cs) | Abstract base holding the RandomHash input lengths and expected digests. |
| [RegisterCryptoExModule](RegisterCryptoExModule.cs) | Set-up fixture that initializes and finalizes the CryptoEx module around the run. |
| [SchnorrSecurityTests](SchnorrSecurityTests.cs) | Schnorr traits, IES round trips, rejection of malformed, forged and transplanted signatures, key parsing, auxiliary randomness and batch verification. |
| [VerifiableRandomTests](VerifiableRandomTests.cs) | `VerfiableRandom` output reproduced from a public key and proof with a uniform byte distribution, and rejection of altered seeds, across signature schemes and hash functions. |
| [VRFTests](VRFTests.cs) | `VRF.CreateCryptographicVRF` proofs and outputs, and rejection of bad, empty, null or altered proofs and outputs, across signature schemes and hash functions. |

`CryptoTest.cs` is fully commented out and contains no compiled fixtures.
