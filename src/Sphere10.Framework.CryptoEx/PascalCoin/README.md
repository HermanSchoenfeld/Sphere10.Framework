# PascalCoin compatibility

PascalCoin's ECIES engine and key-derivation function, and its Base58, Pascal64 and PascalAscii string encodings.

`PascalCoinIesEngine` writes PascalCoin's OpenSSL ECIES format and reads historical managed encodings. `ECIES` in [Elliptic-curve signatures](../EC/README.md) configures it with ECDH, `PascalCoinEciesKdfBytesGenerator` over SHA-512, AES-256-CBC with a zero IV and HMAC-MD5. A caller that constructs the engine directly must pass an unpadded cipher, because the engine pads the message itself. [PascalCoin ECIES compatibility](ECIES-compatibility.md) describes the format defects, their fixes and the migration rules.

The encodings expose their character sets and regular-expression patterns as constants. `Pascal64Encoding` and `PascalAsciiEncoding` validate, escape and unescape strings in those formats. `PascalBase58Encoding` adds a marker byte before encoding, so leading zero bytes survive a round trip. Its static `IsValid` uses a regular expression that only the instance constructor creates, so construct an instance once before calling it. Bitcoin's Base58 variant is in [Bitcoin encodings](../Bitcoin/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Pascal64Encoding`](Pascal64Encoding.cs) | static class | Pascal64 character sets and patterns, with `IsValidEscaped`, `IsValidUnescaped`, `Escape` and `Unescape` |
| [`PascalAsciiEncoding`](PascalAsciiEncoding.cs) | static class | PascalAscii character sets and patterns, with `IsValidEscaped`, `IsValidUnescaped`, `Escape` and `Unescape` |
| [`PascalBase58Encoding`](PascalBase58Encoding.cs) | class | PascalCoin Base58 `Encode`, `Decode`, `TryDecode` and `IsValid` over a `BigInteger` with a marker byte |
| [`PascalCoinEciesKdfBytesGenerator`](PascalCoinEciesKdfBytesGenerator.cs) | class | `IDerivationFunction` that hashes the ECDH shared secret once with its digest and truncates the result to the requested length |
| [`PascalCoinIesEngine`](PascalCoinIesEngine.cs) | class | `CustomIesEngine` that writes PascalCoin's OpenSSL ECIES format with a length header and reads historical managed ciphertexts |
