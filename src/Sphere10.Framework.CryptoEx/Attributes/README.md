# Curve metadata attributes

Attributes that attach a BouncyCastle curve name, an OpenSSL curve identifier, the group order and the prime field to each `ECDSAKeyType` value.

Each member of [`ECDSAKeyType`](../EC/ECDSAKeyType.cs) carries these attributes, so the curve parameters sit beside the enumeration that selects them. `SECT283K1` is a binary-field curve and has no `KeyTypePrimeFieldAttribute`. The order and prime-field attributes parse their hexadecimal argument into a positive BouncyCastle `BigInteger`. No code in this library reads the attributes at run time, so they serve as declared metadata that callers can read through reflection. The signature schemes that use the curves are described in [Elliptic-curve signatures](../EC/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BouncyCastleCurveAttribute`](BouncyCastleCurveAttribute.cs) | class | Records the BouncyCastle named-curve `Name` of a curve enumeration value |
| [`KeyTypeOrderAttribute`](KeyTypeOrderAttribute.cs) | class | Records a curve's group order as a `BigInteger` `Value` parsed from hexadecimal |
| [`KeyTypePrimeFieldAttribute`](KeyTypePrimeFieldAttribute.cs) | class | Records a prime-field curve's field characteristic as a `BigInteger` `Value` parsed from hexadecimal |
| [`OpenSSLIDAttribute`](OpenSSLIDAttribute.cs) | class | Records the OpenSSL numeric curve identifier `ID`, such as 714 for secp256k1 |
