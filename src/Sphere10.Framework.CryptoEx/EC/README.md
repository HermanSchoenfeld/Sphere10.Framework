# Elliptic-curve signatures

ECDSA on four named curves, BIP-340 Schnorr and MuSig multi-signatures on secp256k1, and the PascalCoin-compatible ECIES that both signature schemes expose.

`ECDSA` and `Schnorr` derive from the core library's `StatelessDigitalSignatureScheme<TPrivateKey, TPublicKey>`, so they implement `IDigitalSignatureScheme` and can be used directly or through `Signers` once the [module configuration](../ModuleConfiguration.cs) has registered them. Both schemes declare their own nested `Key`, `PrivateKey` and `PublicKey` classes, which wrap BouncyCastle key parameters. `ECDSAKeyType` selects the curve, and its members carry the [curve metadata attributes](../Attributes/README.md). The sealed `SECP256k1`, `SECP384R1`, `SECP521R1` and `SECT283K1` classes fix the curve of an `ECDSA` instance. ECDSA signing uses RFC 6979 deterministic nonces through `CustomEcDsaSigner`, and `CustomDsaEncoding` writes and enforces strict low-S DER signatures.

`Schnorr` accepts only `ECDSAKeyType.SECP256K1`, and its constructor rejects other curves. `MuSigBuilder` runs one participant's side of a MuSig signing session over the lower-level `MuSig` operations, and the session classes carry the intermediate values. The `IES` property of both schemes returns an `ECIES` instance, which uses the PascalCoin engine from [PascalCoin compatibility](../PascalCoin/README.md) over the IES building blocks in [Integrated encryption scheme engine](../IES/README.md). The project [README](../README.md) shows signing, MuSig and ECIES examples.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CustomDsaEncoding`](CustomDsaEncoding.cs) | class | `StandardDsaEncoding` that writes low-S DER signatures and decodes only strict BIP-66 DER, with static `IsValidSignatureEncoding` |
| [`CustomEcDsaSigner`](CustomEcDsaSigner.cs) | class | `ECDsaSigner` that can force a low R value, rejects high-S signatures on verification and supplies an RFC 6979 deterministic signer |
| [`ECDSA`](ECDSA.cs) | class | ECDSA signature scheme on a chosen `ECDSAKeyType`, with SHA2-256 digests by default, compressed public keys and an `ECIES` `IES` property |
| [`ECDSAKeyType`](ECDSAKeyType.cs) | enum | Curve selector: `SECP256K1`, `SECP384R1`, `SECP521R1` or `SECT283K1` |
| [`GeneralDigestAdapter`](GeneralDigestAdapter.cs) | class | `IHashFunction` over a BouncyCastle `GeneralDigest`, whose `Clone` throws `NotSupportedException` |
| [`SECP256k1`](SECP256k1.cs) | class | Sealed `ECDSA` fixed to the secp256k1 curve |
| [`SECP384R1`](SECP384R1.cs) | class | Sealed `ECDSA` fixed to the secp384r1 curve |
| [`SECP521R1`](SECP521R1.cs) | class | Sealed `ECDSA` fixed to the secp521r1 curve |
| [`SECT283K1`](SECT283K1.cs) | class | Sealed `ECDSA` fixed to the binary-field sect283k1 curve |

### IES

| Type | Kind | Purpose |
|---|---|---|
| [`ECIES`](IES/ECIES.cs) | class | Sealed `IIESAlgorithm` that encrypts up to 32,000 bytes for an `ECDSA` or `Schnorr` public key in PascalCoin's OpenSSL ECIES format and decrypts with `TryDecrypt` |

### Schnorr

| Type | Kind | Purpose |
|---|---|---|
| [`AggregatedPublicKeyData`](Schnorr/MuSigData.cs) | class | Combined MuSig public-key point and its parity |
| [`AggregatedPublicNonce`](Schnorr/MusigNonceData.cs) | class | Aggregated MuSig nonce, final nonce, nonce coefficient and final-nonce parity |
| [`CurveData`](Schnorr/CurveParam.cs) | class | Generator point, curve, field prime and order of one curve |
| [`CurveParam`](Schnorr/CurveParam.cs) | class | Static `GetCurveData` lookup of `CurveData` for every `ECDSAKeyType` |
| [`MuSig`](Schnorr/MuSig.cs) | class | MuSig operations over a `Schnorr` instance: key aggregation, nonce aggregation, session setup, partial signing and verification, and signature aggregation |
| [`MuSigBuilder`](Schnorr/MuSigBuilder.cs) | class | One participant's MuSig session for a 32-byte digest, which collects public keys, nonces and partial signatures and builds the aggregated signature |
| [`MuSigData`](Schnorr/MuSigData.cs) | class | Result of a MuSig session: the aggregated public key and the aggregated signature |
| [`MuSigSessionCache`](Schnorr/MuSigSessionCache.cs) | class | Per-session MuSig values: final nonce and its parity, public-key parity, challenge and nonce coefficient |
| [`Schnorr`](Schnorr/Schnorr.cs) | class | BIP-340 Schnorr signature scheme on secp256k1 with 32-byte x-only public keys, auxiliary-random signing and batch verification |
| [`SignerMuSigSession`](Schnorr/SignerMuSigSession.cs) | class | One signer's MuSig secret key, key coefficient, key parity and nonces |

`ECIESPublicKeyParser` in `IES/` is internal.
