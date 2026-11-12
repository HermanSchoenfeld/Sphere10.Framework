# Sphere10.HashLib4CSharp.Tests

NUnit tests for [Sphere10.HashLib4CSharp](../../src/Sphere10.HashLib4CSharp/README.md), the Framework's fork of the HashLib4CSharp hashing library. The fixtures check every algorithm family against known test vectors and share a common set of behavior checks for cloning, incremental hashing, streams, spans, null input, cancellation and keys.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced project first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.HashLib4CSharp.Tests/Sphere10.HashLib4CSharp.Tests.csproj -c Release -p:BuildRevision=0
```

Run one algorithm family, for example the key derivation functions:

```powershell
dotnet test tests/Sphere10.HashLib4CSharp.Tests/Sphere10.HashLib4CSharp.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~PBKDF"
```

The fixtures use the `HashLib4CSharp.Tests` namespace. They are `internal` classes, which NUnit discovers like public ones. The suite needs no network, database or desktop session.

## Contents

### src

All fixtures, abstract test bases and test-vector tables. Each row lists every class declared in its file.

| Fixture | Purpose |
| --- | --- |
| [HashTestBase, CloneTestBase, CommonTestBase, CRCFactoryTestBase, AlgorithmTestBase, NullDigestAlgorithmTestBase, ByteKeyAlgorithmTestBase, FourByteKeyAlgorithmTestBase, EightByteKeyAlgorithmTestBase, SixteenByteKeyAlgorithmTestBase, CryptoAlgorithmTestBase, Blake2CryptoAlgorithmTestBase](src/HashTestBase.cs) | Abstract bases holding the shared inputs and the inherited checks for cloning, null input, span and array equality, chunked incremental hashing, multithreading, streams, cancellation, known digests, keys and HMAC. |
| [Blake2BTestVectors, Blake2BPTestVectors, Blake2STestVectors, Blake2SPTestVectors, Blake3TestVectors](src/BlakeTestVectors.cs) | Keyed and unkeyed vector tables for the BLAKE2 and BLAKE3 fixtures. They contain no tests. |
| [Blake2BTest, Blake2BPTest, Blake2STest, Blake2SPTest, Blake3Test, GostTest, GOST3411_2012_256Test, GOST3411_2012_512Test, Grindahl256Test, Grindahl512Test, HAS160Test, Haval_3_128Test to Haval_5_256Test (15 fixtures), Keccak_224Test, Keccak_256Test, Keccak_288Test, Keccak_384Test, Keccak_512Test, MD2Test, MD4Test, MD5Test, PanamaTest, RadioGatun32Test, RadioGatun64Test, RIPEMDTest, RIPEMD128Test, RIPEMD160Test, RIPEMD256Test, RIPEMD320Test, SHA0Test, SHA1Test, SHA2_224Test, SHA2_256Test, SHA2_384Test, SHA2_512Test, SHA2_512_224Test, SHA2_512_256Test, SHA3_224Test, SHA3_256Test, SHA3_384Test, SHA3_512Test, Snefru_8_128Test, Snefru_8_256Test, Tiger_3_128Test to Tiger_5_192Test (9 fixtures), Tiger2_3_128Test to Tiger2_5_192Test (9 fixtures), WhirlPoolTest](src/CryptoTest.cs) | Cryptographic hash digests, HMAC results and algorithm-specific cases such as BLAKE2 tree mode and keyed vectors. |
| [APTest, BernsteinTest, Bernstein1Test, BKDRTest, DEKTest, DJBTest, ELFTest, FNVTest, FNV1aTest, Jenkins3Test, JSTest, Murmur2Test, MurmurHash3_x86_32Test, OneAtTimeTest, PJWTest, RotatingTest, RSTest, SDBMTest, ShiftAndXorTest, SuperFastTest, XXHash32Test](src/Hash32Test.cs) | 32-bit non-cryptographic hashes, with key handling for the seeded algorithms. |
| [FNV64Test, FNV1a64Test, Murmur2_64Test, SipHash64_2_4Test, XXHash64Test](src/Hash64Test.cs) | 64-bit non-cryptographic hashes and their keys. |
| [MurmurHash3_x86_128Test, MurmurHash3_x64_128Test, SipHash128_2_4Test](src/Hash128Test.cs) | 128-bit non-cryptographic hashes and their keys. |
| [Adler32Test, Crc32PKZipTest, Crc32CastagnoliTest, CRCFactoryTest](src/ChecksumTest.cs) | Adler-32 and CRC-32 checksums, and the check value of every predefined `CRCModel` through the CRC factory. |
| [NullDigestTest](src/NullDigestTest.cs) | The null digest, which returns its input unchanged and does not implement hash or block sizes. |
| [MACTestBase, HMACTestBase, KMACTestBase, Blake2MACTestBase](src/MACTestBase.cs) | Abstract bases for the MAC fixtures. |
| [MD5HMACTest, KMAC128Test, KMAC256Test, Blake2BMACTest, Blake2SMACTest](src/MACTest.cs) | HMAC, KMAC and keyed BLAKE2 MAC results, and key changes followed by reinitialization. |
| [KDFTestBase, PBKDF2HMACTestBase, PBKDFScryptTestBase, PBKDFBlake3TestBase, PBKDFArgon2TestBase](src/KDFTestBase.cs) | Abstract bases for the key derivation fixtures. |
| [PBKDF2HMACSHA1Test, PBKDF2HMACSHA256Test, PBKDFScryptTest, PBKDFBlake3Test, PBKDFArgon2Test](src/KDFTest.cs) | PBKDF2, scrypt, BLAKE3 and Argon2 key derivation vectors, argument validation, cancellation and cloning. |
| [XOFTestBase, ShakeTestBase, CShakeTestBase, KMACXOFTestBase, Blake2XTestBase](src/XOFTestBase.cs) | Abstract bases for the extendable-output fixtures. |
| [Shake_128Test, Shake_256Test, CShake_128Test, CShake_256Test, KMAC128XOFTest, KMAC256XOFTest, Blake2XBTest, Blake2XSTest, Blake3XOFTest](src/XOFTest.cs) | SHAKE, cSHAKE, KMAC XOF, BLAKE2X and BLAKE3 extendable output, including very long and streamed output, buffer checks and cloning. |
| [AdapterTestBase, HashAdapterTestBase, HMACAdapterTestBase, KDFAdapterTestBase](src/AdapterTestBase.cs) | Abstract bases that compare HashLib adapters with the matching `System.Security.Cryptography` implementations. |
| [MD5AdapterTest, SHA1AdapterTest, SHA256AdapterTest, SHA384AdapterTest, SHA512AdapterTest, MD5HMACAdapterTest, SHA1HMACAdapterTest, SHA256HMACAdapterTest, SHA384HMACAdapterTest, SHA512HMACAdapterTest, SHA1KDFAdapterTest, SHA256KDFAdapterTest, SHA384KDFAdapterTest, SHA512KDFAdapterTest](src/AdapterTest.cs) | `HashAlgorithm`, `HMAC` and `DeriveBytes` adapters producing the same results as the .NET implementations. |
