# Cryptography

Hash functions, digital signature schemes, post-quantum one-time signatures, random number generators, key derivation, encryption and verifiable random functions.

The registries are the entry points. `Hashers` maps each `CHF` value to a pooled `IHashFunction`, and `Signers` maps each `DSS` value to an `IDigitalSignatureScheme`. This core package registers SHA-2, SHA-1 and Blake2b hashers and the post-quantum `PQC_WAMS` and `PQC_WAMSSharp` signers. The `Sphere10.Framework.CryptoEx` package registers the ECDSA and Schnorr signers and further hash functions through its module configuration, so a `CHF` or `DSS` value without a registration fails at first use. `Hashers` reuses hasher instances through `EnterBorrowHasherScope`, which returns the hasher to its pool on disposal, so the static methods are safe to call from several threads.

A signature scheme signs a message digest with a signer nonce. Stateless schemes (`StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>`) ignore the nonce. The AMS family (`AMS`, `WAMS`, `WAMSSharp`) is stateful, because the nonce selects the one-time key batch and index used for the signature. `ItemSigner<TItem>` and `SignedItem<T>` sign serialized objects through [Serialization](../Serialization/README.md). `Tools.Crypto` collects the common one-call helpers for random bytes, fixed-time comparison, password hashing and the string and stream ciphers of `Encryptor`. `Encrypted<TObject>` stores a serialized object under a password-derived key with an authentication tag.

```csharp
byte[] digest = Hashers.Hash(CHF.SHA2_256, data);

var privateKey = Signers.GeneratePrivateKey(DSS.PQC_WAMS);
var publicKey = Signers.DerivePublicKey(DSS.PQC_WAMS, privateKey, signerNonce: 0);
var signature = Signers.Sign(DSS.PQC_WAMS, privateKey, message, signerNonce: 0);
var valid = Signers.Verify(DSS.PQC_WAMS, signature, message, publicKey);
```

Most types are in the `Sphere10.Framework` namespace. `HashRandom`, `Mersenne32`, `IRandomNumberGenerator` and `RandomNumberGeneratorDecorator` are in `Sphere10.Framework.Maths`, and `Tools.Crypto` is in the `Tools` namespace. In `SecureBytes`, `Protected` currently reports `true` while an unprotected scope is open, so reading `Item` inside `EnterUnprotectedScope()` throws.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DigitalSignatureSchemeBase<TPrivateKey, TPublicKey>`](DigitalSignatureSchemeBase.cs) | abstract class | Base for typed signature schemes, with a message digest hash function, traits and the typed parse, derive, sign and verify hooks. |
| [`DigitalSignatureSchemeDecorator<TPrivateKey, TPublicKey, TScheme>`](DigitalSignatureSchemeDecorator.cs) | abstract class | Forwards every `IDigitalSignatureScheme` member to an inner scheme so a subclass can override selected steps. |
| [`DigitalSignatureSchemeTraits`](DigitalSignatureSchemeTraits.cs) | enum | Flags that describe a scheme: one-time, salted, stateless, stateful, IES support, ECDSA, Schnorr or post-quantum. |
| [`IDigitalSignatureScheme`](IDigitalSignatureScheme.cs) | interface | Untyped signature scheme contract for key parsing, creation and derivation, digest calculation, digest signing and verification. |
| [`IDigitalSignatureScheme<TPrivateKey, TPublicKey>`](IDigitalSignatureSchemeT.cs) | interface | Defines typed key operations and their non-generic signature-scheme adapters. |
| [`IDigitalSignatureSchemeExtensions`](IDigitalSignatureScheme.cs) | static class | Adds `GeneratePrivateKey`, `ParsePublicKey`, `ParsePrivateKey`, `Sign` and `Verify` over message bytes to `IDigitalSignatureScheme`. |
| [`IIESAlgorithm`](IIESAlgorithm.cs) | interface | Integrated encryption scheme that encrypts to a public key and decrypts with the private key. |
| [`IIESAlgorithmExtensions`](IIESAlgorithm.cs) | static class | Adds a throwing `Decrypt` to `IIESAlgorithm`. |
| [`IKey`](IKey.cs) | interface | Key material exposed as `RawBytes`. |
| [`IPrivateKey`](IPrivateKey.cs) | interface | Marker for private keys. |
| [`IPublicKey`](IPublicKey.cs) | interface | Marker for public keys. |
| [`SaltedSignatureScheme<TPrivateKey, TPublicKey, TScheme>`](SaltedSignatureScheme.cs) | class | Decorator that signs a salted message authentication code and appends the salt to the signature. |
| [`StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>`](StatelessDigitalSignatureScheme.cs) | abstract class | Base for schemes such as ECDSA, Schnorr and SPHINCS+ that never need the nonces of earlier signatures. |
| [`Tools.Crypto`](CryptoTool.cs) | tools | Secure erase, cryptographic GUIDs and random bytes, fixed-time comparison, password hashing and DES and AES string and stream encryption. |

### Checksums

| Type | Kind | Purpose |
|---|---|---|
| [`MURMUR3_32`](Checksums/MURMUR3_32.cs) | static class | 32-bit MurmurHash3 of a byte span with an optional seed, for non-cryptographic checksums. |

### DataProtection

| Type | Kind | Purpose |
|---|---|---|
| [`ISecureItem`](DataProtection/ISecureItem.cs) | interface | Protected value with a `Protected` flag and `EnterUnprotectedScope()`. |
| [`ISecureItem<T>`](DataProtection/ISecureItemT.cs) | interface | Typed protected value exposed through `Item`. |
| [`SecureBytes`](DataProtection/SecureBytes.cs) | class | Keeps sensitive bytes in a `SecureString` and erases the plain copy when no unprotected scope is open. |
| [`SecureItem<T>`](DataProtection/SecureItemT.cs) | class | Protects a serialized object through `SecureBytes` and an `IItemSerializer<T>`. |

### Encryption

| Type | Kind | Purpose |
|---|---|---|
| [`Encrypted`](Encryption/Encrypted.cs) | class | Factory `For<T>` and the default key-derivation iteration count for `Encrypted<TObject>`. |
| [`Encrypted<TObject>`](Encryption/Encrypted.cs) | class | Stores serialized ciphertext with separate salt, counter and authentication metadata. |
| [`EncryptionSaltPolicy`](Encryption/EncryptionSaltPolicy.cs) | enum | Machine and process properties that `EncryptedStringAttribute` mixes into its salt. |
| [`Encryptor`](Encryption/Encryptor.cs) | static class | DES and AES encryption of strings and streams, and preparation of a password-keyed `SymmetricAlgorithm`. |
| [`IEncrypted<TObject>`](Encryption/Encrypted.cs) | interface | Encrypted object contract with `TryDecrypt` by secret. |

### Hashing

| Type | Kind | Purpose |
|---|---|---|
| [`Blake2b`](Hashing/Blake2b.cs) | class | Managed Blake2b hash function with configurable output size, key, salt and personalization. |
| [`CHF`](Hashing/CHF.cs) | enum | Identifiers of the cryptographic hash functions known to `Hashers`. |
| [`HashAlgorithmAdapter`](Hashing/HashAlgorithmAdapter.cs) | class | Adapts a BCL `HashAlgorithm` to `IHashFunction`. |
| [`HashFunctionBase`](Hashing/HashFunctionBase.cs) | abstract class | Base `IHashFunction` with transform state and a `Finalize` hook. |
| [`Hashers`](Hashing/Hashers.cs) | static class | Registry and pooled static API for hashing, join-hashing, iterated hashing and digest aggregation by `CHF`. |
| [`IHashFunction`](Hashing/IHashFunction.cs) | interface | Incremental hash function with `Transform`, `GetResult`, `Compute` and `Reset`. |
| [`IHashFunctionExtensions`](Hashing/IHashFunction.cs) | static class | Adds array-returning `GetResult` and `Compute` to `IHashFunction`. |

### KDF

| Type | Kind | Purpose |
|---|---|---|
| [`PasswordHasher`](KDF/PasswordHasher.cs) | class | Creates and validates salted PBKDF2-SHA1 password hash strings. |
| [`PBKDF2`](KDF/PBKDF2.cs) | static class | PBKDF2 key derivation from a string secret with SHA-1, or from bytes with a chosen hash algorithm. |

### PQC

| Type | Kind | Purpose |
|---|---|---|
| [`AMS`](PQC/AMS.cs) | class | Stateful post-quantum signature scheme that commits batches of one-time keys in a Merkle tree. |
| [`AMSOTS`](PQC/AMSOTS.cs) | enum | One-time signature algorithm used by an AMS key: W-OTS, W-OTS+ or W-OTS#. |
| [`IOTSAlgorithm`](PQC/IOTSAlgorithm.cs) | interface | One-time signature algorithm contract for key generation, key hashing, digest signing and verification. |
| [`IOTSAlgorithm<TConfig>`](PQC/IOTSAlgorithmT.cs) | interface | Defines a one-time signature algorithm with a specific configuration type. |
| [`IOTSAlgorithmExtensions`](PQC/IOTSAlgorithm.cs) | static class | Adds an array-returning `ComputeKeyHash` to `IOTSAlgorithm`. |
| [`OTSConfig`](PQC/OTSConfig.cs) | abstract class | Parameters of a one-time signature algorithm, including its hash function and key, public key and signature sizes. |
| [`OTSKeyPair`](PQC/OTSKeyPair.cs) | class | One-time private key, public key and a future that yields the public key hash. |
| [`OTSKeySize`](PQC/OTSKeySize.cs) | struct | Width and length of a two-dimensional one-time key or signature. |
| [`WAMS`](PQC/WAMS.cs) | class | AMS over Winternitz one-time signatures (`DSS.PQC_WAMS`). |
| [`WAMSSharp`](PQC/WAMSSharp.cs) | class | AMS over W-OTS# one-time signatures (`DSS.PQC_WAMSSharp`). |
| [`WOTS`](PQC/WOTS.cs) | class | Winternitz One-Time Signature scheme (W-OTS). |
| [`WOTSSharp`](PQC/WOTSSharp.cs) | class | W-OTS# scheme, a salt-hardened W-OTS variant that permits shorter and faster hash functions. |

### RNG

| Type | Kind | Purpose |
|---|---|---|
| [`EntropyPolicy`](RNG/EntropyPolicy.cs) | enum | Entropy sources that `SeedGenerator` hashes: GUID, time, environment and byte order. |
| [`HashRandom`](RNG/HashRandom.cs) | class | Deterministic cryptographically secure generator that hashes a seed and counter. |
| [`IRandomNumberGenerator`](RNG/IRandomNumberGenerator.cs) | interface | Random byte source with `NextBytes(Span<byte>)`. |
| [`IRandomNumberGeneratorExtensions`](RNG/IRandomNumberGenerator.cs) | static class | Adds byte arrays, integers, floats and ASCII characters to `IRandomNumberGenerator`. |
| [`Mersenne32`](RNG/Mersenne32.cs) | class | Seeded Mersenne Twister generator for repeatable non-cryptographic sequences. |
| [`RandomNumberGeneratorDecorator`](RNG/RandomNumberGeneratorDecorator.cs) | class | Forwards `NextBytes` to an inner generator so a subclass can alter the output. |
| [`SeedGenerator`](RNG/SeedGenerator.cs) | static class | Produces a seed by hashing the entropy sources selected by an `EntropyPolicy`. |
| [`SystemCRNG`](RNG/SystemCRNG.cs) | class | Non-deterministic cryptographically secure generator backed by the system random number generator. |

### Signing

| Type | Kind | Purpose |
|---|---|---|
| [`DSS`](Signing/DSS.cs) | enum | Identifiers of the digital signature schemes known to `Signers`, covering ECDSA, Schnorr and the post-quantum AMS variants. |
| [`IItemSigner<TItem>`](Signing/IItemSigner.cs) | interface | Signs and verifies typed items through a digital signature scheme. |
| [`ItemSigner<TItem>`](Signing/ItemSigner.cs) | class | Signs the hash of a serialized item with a chosen scheme. |
| [`ItemSignerExtensions`](Signing/IItemSigner.cs) | static class | Adds throwing `Sign` and `Verify` to `IItemSigner<T>`. |
| [`SignedItem`](Signing/SignedItem.cs) | abstract record | Untyped item and signature pair. |
| [`SignedItem<T>`](Signing/SignedItem.cs) | record | Typed item and signature pair with `Verify` overloads by serializer, hasher or signer. |
| [`Signers`](Signing/Signers.cs) | class | Registry and static API for key parsing, generation, derivation, signing and verification by `DSS`. |

### VRF

| Type | Kind | Purpose |
|---|---|---|
| [`CryptographicVRF`](VRF/CryptographicVRF.cs) | class | Verifiable random function that signs the seed digest as the proof and hashes the proof as the output. |
| [`IVRFAlgorithm`](VRF/IVRFAlgorithm.cs) | interface | Verifiable random function contract for running with a private key and verifying with a public key. |
| [`IVRFAlgorithmExtensions`](VRF/IVRFAlgorithm.cs) | static class | Adds array-returning `Run` and a throwing `VerifyProofOrThrow` to `IVRFAlgorithm`. |
| [`VerfiableRandom`](VRF/VerfiableRandom.cs) | class | `HashRandom` sequence seeded by a VRF output, built from a private key or by verifying a supplied proof against a public key. |
| [`VRF`](VRF/VRF.cs) | class | Static helpers that create a `CryptographicVRF` and generate or verify output by `CHF` and `DSS`. |
| [`VRFAlgorithmBase`](VRF/VRFAlgorithmBase.cs) | abstract class | Base VRF that checks output length and delegates running and verification to subclass hooks. |
