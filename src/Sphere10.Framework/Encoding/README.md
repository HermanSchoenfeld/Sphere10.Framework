# Encoding

Text encodings of binary data in hexadecimal, Base32 and Base62.

`HexEncoding` writes lowercase hexadecimal with an optional `0x` prefix, and it validates and decodes text with or without that prefix. `Base32Encoding` uses its own alphabet (`QAZ2WSX3EDC4RFV5TGB6YHN7UJM8K9LP`) rather than the RFC 4648 alphabet, so its output only round-trips through this type. `Base62Encoding` converts byte arrays and 32-bit integers with the digits, uppercase and lowercase letters. The `ToHexString`, `ToBase32String` and `ToBase62String` extensions on `byte[]` in [Extensions](../Extensions/README.md) call these types.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Base32Encoding`](Base32Encoding.cs) | static class | Encodes bytes or ASCII text to Base32 with a custom alphabet, and decodes and validates such text. |
| [`Base62Encoding`](Base62Encoding.cs) | static class | Converts byte arrays, `int` and `uint` values to Base62 text and decodes it back to bytes. |
| [`HexEncoding`](HexEncoding.cs) | class | Encodes, decodes and validates hexadecimal text, with regular-expression patterns and optional `0x` prefix support. |
