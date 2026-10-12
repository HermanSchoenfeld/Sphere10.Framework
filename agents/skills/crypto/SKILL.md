---
name: crypto
description: Framework crypto only — Hashers/CHF, Tools.Crypto, IDigitalSignatureScheme. Never raw System.Security.Cryptography. Trigger on hashing, randomness, signing, or key derivation.
---

# Crypto Skill

Read [framework utility guidance](../../../docs/guidelines/patterns/framework-utilities.md) when choosing the existing crypto infrastructure.

Prefer framework crypto (`Sphere10.Framework`, `Sphere10.Framework.CryptoEx`) over raw BCL crypto.

## Hashing

```csharp
using Sphere10.Framework;

var hash = Hashers.Hash(CHF.SHA2_256, data);
```

- `Hashers` pools registered `IHashFunction` implementations; `CHF` names the algorithm (`SHA2_256`, `SHA2_512`, `Blake2b_256`, `SHA3_256`, ...). Use the enum's exact spelling. An enum value alone does not register its implementation: inspect the selected platform and loaded crypto modules before assuming every algorithm is available.
- **Never** `SHA256.Create().ComputeHash(...)`.

## Randomness & secrets
- `Tools.Crypto.GenerateCryptographicallyRandomBytes(n)` — never `new Random()` or bare `RandomNumberGenerator`.
- `Tools.Crypto` also covers password hashing, AES, `SecureErase`.

## Signatures & keys
- `IDigitalSignatureScheme` / `StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>`; implementations: `Schnorr`, `ECDSA`, etc.
- The non-generic contract creates seeded keys with `CreatePrivateKey(ReadOnlySpan<byte>)`; the generic contract and scheme bases expose `GeneratePrivateKey(ReadOnlySpan<byte>)` and bridge it inline. Use the API exposed by the variable's actual type.
- Seeded key generation must remain deterministic. The current ECDSA/Schnorr implementations seed a BouncyCastle `DigestRandomGenerator` exclusively with the supplied seed. Preserve the scheme's existing derivation rather than substituting an unrelated derivation or adding ambient entropy. Parameterless `GeneratePrivateKey()` generates 32 cryptographically random seed bytes before calling the scheme.
- Distinguish message APIs from digest APIs: `Sign`/`Verify` calculate the scheme's digest, while `SignDigest`/`VerifyDigest` receive an already calculated digest. Preserve the scheme's digest length and the signer nonce required by its contract; stateless scheme bases adapt the nonce-bearing primitives to their nonce-free implementations.
- IES is exposed through `scheme.IES` (`IIESAlgorithm`); inspect the chosen scheme's implementation before assuming it supports the requested encryption path.

## Verify the chosen scheme

Use existing hash/signature fixtures as the reference for supported algorithms. Cover deterministic seeded keys, signature verification, altered-message/signature rejection, and required interoperability vectors when the cryptographic path changes. Do not equate managed array overwrite with guaranteed erasure of every copy of a secret.

Current source references: `src/Sphere10.Framework/Crypto/IDigitalSignatureScheme.cs`, `IDigitalSignatureSchemeT.cs`, `DigitalSignatureSchemeBase.cs`, `CryptoTool.cs`, and `src/Sphere10.Framework.CryptoEx/EC/ECDSA.cs` / `EC/Schnorr/Schnorr.cs`.

## Comparers
Use `ComparerFactory.Default.GetEqualityComparer<T>()` — not ad-hoc `IEqualityComparer<T>`. `ByteArrayEqualityComparer` for `byte[]` (not `SequenceEqual` in hot paths). Chain: `new ComparerFactory(ComparerFactory.Default)`.
