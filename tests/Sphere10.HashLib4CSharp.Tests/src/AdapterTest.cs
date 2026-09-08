using System.Security.Cryptography;
using HashLib4CSharp.Base;
using NUnit.Framework;

namespace HashLib4CSharp.Tests
{
    [TestFixture]
    internal class MD5AdapterTest : HashAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            HashAdapterInstance = HashFactory.Adapter.CreateHashAlgorithmFromHash(HashFactory.Crypto.CreateMD5());
            HashAlgorithmInstance = MD5.Create();
        }
    }

    [TestFixture]
    internal class SHA1AdapterTest : HashAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            HashAdapterInstance = HashFactory.Adapter.CreateHashAlgorithmFromHash(HashFactory.Crypto.CreateSHA1());
            HashAlgorithmInstance = SHA1.Create();
        }
    }

    [TestFixture]
    internal class SHA256AdapterTest : HashAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            HashAdapterInstance = HashFactory.Adapter.CreateHashAlgorithmFromHash(HashFactory.Crypto.CreateSHA2_256());
            HashAlgorithmInstance = SHA256.Create();
        }
    }

    [TestFixture]
    internal class SHA384AdapterTest : HashAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            HashAdapterInstance = HashFactory.Adapter.CreateHashAlgorithmFromHash(HashFactory.Crypto.CreateSHA2_384());
            HashAlgorithmInstance = SHA384.Create();
        }
    }

    [TestFixture]
    internal class SHA512AdapterTest : HashAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            HashAdapterInstance = HashFactory.Adapter.CreateHashAlgorithmFromHash(HashFactory.Crypto.CreateSHA2_512());
            HashAlgorithmInstance = SHA512.Create();
        }
    }

    [TestFixture]
    internal class MD5HMACAdapterTest : HMACAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var key = GenerateRandomByteArray(Random.Next(0, 255));
            HMACAdapterInstance =
                HashFactory.Adapter.CreateHMACFromHMACNotBuiltIn(
                    HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateMD5(), key));
            HMACInstance = new HMACMD5(key);
        }
    }

    [TestFixture]
    internal class SHA1HMACAdapterTest : HMACAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var key = GenerateRandomByteArray(Random.Next(0, 255));
            HMACAdapterInstance =
                HashFactory.Adapter.CreateHMACFromHMACNotBuiltIn(
                    HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA1(), key));
            HMACInstance = new HMACSHA1(key);
        }
    }

    [TestFixture]
    internal class SHA256HMACAdapterTest : HMACAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var key = GenerateRandomByteArray(Random.Next(0, 255));
            HMACAdapterInstance =
                HashFactory.Adapter.CreateHMACFromHMACNotBuiltIn(
                    HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA2_256(), key));
            HMACInstance = new HMACSHA256(key);
        }
    }

    [TestFixture]
    internal class SHA384HMACAdapterTest : HMACAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var key = GenerateRandomByteArray(Random.Next(0, 255));
            HMACAdapterInstance =
                HashFactory.Adapter.CreateHMACFromHMACNotBuiltIn(
                    HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA2_384(), key));
            HMACInstance = new HMACSHA384(key);
        }
    }

    [TestFixture]
    internal class SHA512HMACAdapterTest : HMACAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var key = GenerateRandomByteArray(Random.Next(0, 255));
            HMACAdapterInstance =
                HashFactory.Adapter.CreateHMACFromHMACNotBuiltIn(
                    HashFactory.HMAC.CreateHMAC(HashFactory.Crypto.CreateSHA2_512(), key));
            HMACInstance = new HMACSHA512(key);
        }
    }

    [TestFixture]
    internal class SHA1KDFAdapterTest : KDFAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            ByteCount = Random.Next(1, 255);
            var password = GenerateRandomByteArray(Random.Next(0, 255));
            var salt = GenerateRandomByteArray(Random.Next(8, 255));
            var iterations = Random.Next(1, 255);

            KDFAdapterInstance =
                HashFactory.Adapter.CreateDeriveBytesFromKDFNotBuiltIn(
                    HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC(HashFactory.Crypto.CreateSHA1(), password, salt,
                        (uint) iterations));
            ExpectedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA1, ByteCount);
        }
    }

    [TestFixture]
    internal class SHA256KDFAdapterTest : KDFAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            ByteCount = Random.Next(1, 255);
            var password = GenerateRandomByteArray(Random.Next(0, 255));
            var salt = GenerateRandomByteArray(Random.Next(8, 255));
            var iterations = Random.Next(1, 255);

            KDFAdapterInstance =
                HashFactory.Adapter.CreateDeriveBytesFromKDFNotBuiltIn(
                    HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC(HashFactory.Crypto.CreateSHA2_256(), password, salt,
                        (uint) iterations));
            ExpectedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, ByteCount);
        }
    }

    [TestFixture]
    internal class SHA384KDFAdapterTest : KDFAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            ByteCount = Random.Next(1, 255);
            var password = GenerateRandomByteArray(Random.Next(0, 255));
            var salt = GenerateRandomByteArray(Random.Next(8, 255));
            var iterations = Random.Next(1, 255);

            KDFAdapterInstance =
                HashFactory.Adapter.CreateDeriveBytesFromKDFNotBuiltIn(
                    HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC(HashFactory.Crypto.CreateSHA2_384(), password, salt,
                        (uint) iterations));
            ExpectedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA384, ByteCount);
        }
    }

    [TestFixture]
    internal class SHA512KDFAdapterTest : KDFAdapterTestBase
    {
        [OneTimeSetUp]
        public void Setup()
        {
            ByteCount = Random.Next(1, 255);
            var password = GenerateRandomByteArray(Random.Next(0, 255));
            var salt = GenerateRandomByteArray(Random.Next(8, 255));
            var iterations = Random.Next(1, 255);

            KDFAdapterInstance =
                HashFactory.Adapter.CreateDeriveBytesFromKDFNotBuiltIn(
                    HashFactory.KDF.PBKDF2HMAC.CreatePBKDF2HMAC(HashFactory.Crypto.CreateSHA2_512(), password, salt,
                        (uint) iterations));
            ExpectedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, ByteCount);
        }
    }
}

