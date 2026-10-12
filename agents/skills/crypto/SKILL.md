---
name: crypto
description: Framework crypto only — Hashers/CHF, Tools.Crypto, IDigitalSignatureScheme. Never raw System.Security.Cryptography. Trigger on hashing, randomness, signing, or key derivation.
---

# Crypto Skill

Prefer framework crypto (`Sphere10.Framework`, `Sphere10.Framework.CryptoEx`) over raw BCL crypto.

## Hashing
```csharp
var hash = Hashers.Hash(CHF.SHA2_256, data);
```
- `Hashers` is the thread-safe registry of `IHashFunction`; `CHF` names the algorithm (`SHA2_256`, `SHA2_512`, `BLAKE2B_256`, `SHA3_256`, ...).
- **Never** `SHA256.Create().ComputeHash(...)`.

## Randomness & secrets
- `Tools.Crypto.GenerateCryptographicallyRandomBytes(n)` — never `new Random()` or bare `RandomNumberGenerator`.
- `Tools.Crypto` also covers password hashing, AES, `SecureErase`.

## Signatures & keys
- `IDigitalSignatureScheme` / `StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>`; implementations: `Schnorr`, `ECDSA`, etc.
- `GeneratePrivateKey(ReadOnlySpan<byte> seed)` must be deterministic — seed a `DigestRandomGenerator` exclusively with the provided seed. Parameterless overload uses system entropy.
- IES via `scheme.IES` (`IIESAlgorithm`).

## Comparers
Use `ComparerFactory.Default.GetEqualityComparer<T>()` — not ad-hoc `IEqualityComparer<T>`. `ByteArrayEqualityComparer` for `byte[]` (not `SequenceEqual` in hot paths). Chain: `new ComparerFactory(ComparerFactory.Default)`.
