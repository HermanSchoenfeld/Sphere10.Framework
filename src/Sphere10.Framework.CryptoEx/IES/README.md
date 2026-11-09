# Integrated encryption scheme engine

The integrated encryption scheme cipher, engine, parameter and ephemeral-key types on which the elliptic-curve `ECIES` is built.

These types follow BouncyCastle's IES design and IEEE P1363a. `CustomIesEngine` combines a key agreement, a key-derivation function, a MAC and an optional buffered block cipher, and it generates an ephemeral key pair for each encryption. `IesCipher` buffers the input and runs the engine when `DoFinal` is called, after `Init` has supplied the key and an `IesParameterSpec`. Most callers use `ECIES` from [Elliptic-curve signatures](../EC/README.md) instead, which configures these types for PascalCoin through `PascalCoinIesEngine` in [PascalCoin compatibility](../PascalCoin/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CustomIesEngine`](CustomIesEngine.cs) | class | IES engine over an `IBasicAgreement`, `IDerivationFunction`, `IMac` and `BufferedBlockCipher`, with a virtual `ProcessBlock` for derived formats |
| [`EphemeralKeyPair`](EphemeralKeyPair.cs) | class | Generated asymmetric key pair with its encoded public key |
| [`EphemeralKeyPairGenerator`](EphemeralKeyPairGenerator.cs) | class | Generates `EphemeralKeyPair` instances from a BouncyCastle key-pair generator and a `KeyEncoder` |
| [`IAlgorithmParameterSpec`](IAlgorithmParameterSpec.cs) | interface | Marker for algorithm parameters passed to `IesCipher.Init` |
| [`IKeyParser`](IKeyParser.cs) | interface | Reads an `AsymmetricKeyParameter`, such as an ephemeral public key, from a stream |
| [`IesCipher`](IesCipher.cs) | class | Buffering IES cipher that encrypts or decrypts with a `CustomIesEngine` once `DoFinal` is called |
| [`IesParameterSpec`](IesParameterSpec.cs) | class | IES parameters: derivation and encoding vectors, MAC and cipher key sizes in bits, the cipher nonce and point compression |
| [`KeyEncoder`](KeyEncoder.cs) | class | Encodes an elliptic-curve public key with or without point compression, returning `null` for other key types |
