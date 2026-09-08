# PascalCoin ECIES compatibility and remaining issues

PascalCoin has two ECIES implementations. Its default Windows OpenSSL backend and the older .NET/CryptoLib4Pascal implementations share ECDH, SHA-512 key derivation, AES-256-CBC and HMAC-MD5, but their padding and length handling differ. Fixing byte recovery does not require replacing those primitives.

## Padding and wire compatibility

OpenSSL pads only an incomplete 16-byte block with zeros and returns the recorded original length. Older managed readers automatically remove terminal zeros and ignore that length: plaintext `41 00` becomes `41` even though decryption reports success.

Older managed writers also append a full padding block to aligned input, while recording no additional block. For 16 plaintext bytes, they write **32 ciphertext bytes but record a 16-byte body**. Their MAC covers all 32 bytes; OpenSSL checks the recorded 16 and rejects the message. The extra block happens to preserve aligned trailing zeros during managed self-decryption.

The .NET repair in [PascalCoinIesEngine](PascalCoinIesEngine.cs), used through [ECDSA.IES](../EC/IES/ECIES.cs), writes native-compatible padding and explicit little-endian lengths. Its reader validates framing, authenticates the actual ciphertext, decrypts without automatic zero removal, checks the padding suffix and returns the reconstructed original bytes. It accepts both native records and historical managed records with the extra aligned block.

New encryption uses PascalCoin's **32,000-byte maximum**. Legacy reads can exceed that limit, including consistent historical records whose 16-bit lengths wrapped; they must be checked against the actual ciphertext rather than rejected solely by the new write limit.

Upgrade readers before switching writers. Old managed readers can lose genuine trailing zeros from new native-format aligned messages. Existing ciphertext and transaction signatures are not rewritten. [Captured compatibility fixtures](../../../tests/Sphere10.Framework.CryptoEx.Tests/PascalCoinEciesCompatibilityTests.cs) cover the original backend formats without requiring Pascal or OpenSSL at test time.

## Metadata authentication remains unresolved

The legacy MAC excludes the six-byte header. For plaintext `41`, changing original length from 1 to 2 leaves the MAC valid and can return `41 00`. Both lengths describe plausible zero-padded data. Bounds and padding checks cannot remove this ambiguity; byte recovery is not a complete integrity repair.

A complete authentication repair needs an explicitly versioned format whose MAC covers the version, header, encoded ephemeral public key and ciphertext, or AEAD with metadata bound as authenticated associated data. A verified outer authenticated envelope can also protect existing ciphertext. Failed authentication must not trigger fallback to legacy decoding. Public-key encryption alone does not authenticate the sender.

A verified PascalCoin transaction signature covers the complete encrypted payload, including its header. Standalone ECIES, raw RPC payload decryption and P2P message decryption do not automatically receive that protection.

## Original OpenSSL bounds checks

PascalCoin's native `src/core/UECIES.pas` uses untrusted header lengths in pointer arithmetic and memory reads without first proving all ranges valid. This is a source finding; no malformed-memory exploit was demonstrated.

Repair that backend by validating the complete header, key/MAC/body ranges, total framing and decrypted allocation bounds with checked arithmetic before dependent pointer reads or cryptographic processing. Verify the MAC before releasing plaintext. These checks prevent unsafe parsing; they do not authenticate the original-length field.

## Regression coverage

The regular NUnit suite preserves the audit findings without a Pascal/OpenSSL installation:

- [Compatibility](../../../tests/Sphere10.Framework.CryptoEx.Tests/PascalCoinEciesCompatibilityTests.cs): captured OpenSSL, CryptoLib4Pascal and historical .NET records; binary boundaries; malformed framing; tampering; and the unresolved header-authentication ambiguity.
- [Encryption vectors](../../../tests/Sphere10.Framework.CryptoEx.Tests/PascalCoinEciesEncryptionVectorTests.cs): fixed-key ciphertext independently verified by original OpenSSL.
- [Legacy lengths](../../../tests/Sphere10.Framework.CryptoEx.Tests/PascalCoinEciesLegacyLengthTests.cs): oversized historical records and wrapped 16-bit lengths.
- [Engine checks](../../../tests/Sphere10.Framework.CryptoEx.Tests/PascalCoinIesEngineTests.cs): input offsets/ranges, bounded key parsing and MAC verification before cipher use.
