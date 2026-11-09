# Bitcoin encodings

Base58 encoding and decoding of byte arrays that follows Bitcoin Core's implementation.

`BitcoinBase58Encoding` exposes static `Encode`, `Decode`, `TryDecode` and `IsValid` members. Decoding skips leading and trailing whitespace and maps each leading `1` character to a zero byte, as Bitcoin Core does. `Decode` throws `ArgumentException` for an invalid string, and `TryDecode` returns `false` instead. A `null` string makes both throw `ArgumentNullException`. PascalCoin uses its own Base58 variant, which [PascalCoin compatibility](../PascalCoin/README.md) provides.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BitcoinBase58Encoding`](BitcoinBase58Encoding.cs) | class | Static Bitcoin Base58 `Encode`, `Decode`, `TryDecode` and `IsValid` over the Bitcoin alphabet |
