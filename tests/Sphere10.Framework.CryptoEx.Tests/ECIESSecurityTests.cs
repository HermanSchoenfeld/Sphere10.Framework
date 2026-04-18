// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sphere10.Framework.CryptoEx.EC;

namespace Sphere10.Framework.CryptoEx.Tests;

/// <summary>
/// Security-focused tests for the ECIES (Integrated Encryption Scheme) implementation.
/// Covers ciphertext tampering, wrong-key decryption, ciphertext malleability,
/// plaintext round-trip integrity, and edge-case message sizes.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ECIESSecurityTests {

    private static byte[] RandomBytes(int length) =>
        Tools.Crypto.GenerateCryptographicallyRandomBytes(length);

    #region Ciphertext Tampering (MAC Integrity)

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    [TestCase(ECDSAKeyType.SECP384R1)]
    [TestCase(ECDSAKeyType.SECP521R1)]
    [TestCase(ECDSAKeyType.SECT283K1)]
    public void Decrypt_FlippedBitInCiphertext_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);
        var message = Encoding.UTF8.GetBytes("Sensitive data for tampering test");
        var ciphertext = ecdsa.IES.Encrypt(message, pk);

        // Flip a bit in the middle of the ciphertext (avoiding header/ephemeral key area)
        var tampered = (byte[])ciphertext.Clone();
        var flipIndex = ciphertext.Length / 2;
        tampered[flipIndex] ^= 0x01;

        Assert.That(ecdsa.IES.TryDecrypt(tampered, out _, sk), Is.False, "Decryption must fail when a ciphertext bit is flipped (MAC should catch tampering)");
    }

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_TruncatedCiphertext_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);
        var message = Encoding.UTF8.GetBytes("Data for truncation test");
        var ciphertext = ecdsa.IES.Encrypt(message, pk);

        // Truncate by removing last byte
        var truncated = ciphertext.AsSpan().Slice(0, ciphertext.Length - 1).ToArray();
        Assert.That(ecdsa.IES.TryDecrypt(truncated, out _, sk), Is.False, "Decryption must fail on truncated ciphertext");
    }

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_AppendedByteToCiphertext_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);
        var message = Encoding.UTF8.GetBytes("Data for append test");
        var ciphertext = ecdsa.IES.Encrypt(message, pk);

        // Append an extra byte
        var extended = new byte[ciphertext.Length + 1];
        Array.Copy(ciphertext, extended, ciphertext.Length);
        extended[^1] = 0x42;

        Assert.That(ecdsa.IES.TryDecrypt(extended, out _, sk), Is.False, "Decryption must fail when extra data is appended to ciphertext");
    }

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_AllZeroCiphertext_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var zeroCiphertext = new byte[128];
        Assert.That(ecdsa.IES.TryDecrypt(zeroCiphertext, out _, sk), Is.False, "Decryption of all-zero ciphertext must fail");
    }

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_RandomGarbage_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var garbage = RandomBytes(256);
        Assert.That(ecdsa.IES.TryDecrypt(garbage, out _, sk), Is.False, "Decryption of random garbage must fail");
    }

    #endregion

    #region Wrong Key Decryption

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    [TestCase(ECDSAKeyType.SECP384R1)]
    [TestCase(ECDSAKeyType.SECP521R1)]
    [TestCase(ECDSAKeyType.SECT283K1)]
    public void Decrypt_WrongPrivateKey_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var skSender = ecdsa.GeneratePrivateKey();
        var pkSender = ecdsa.DerivePublicKey(skSender);
        var skAttacker = ecdsa.GeneratePrivateKey();
        var message = Encoding.UTF8.GetBytes("Confidential message");
        var ciphertext = ecdsa.IES.Encrypt(message, pkSender);

        Assert.That(ecdsa.IES.TryDecrypt(ciphertext, out _, skAttacker), Is.False, "Decryption with a different private key must fail");
    }

    #endregion

    #region Ciphertext Non-Determinism (Ephemeral Key Freshness)

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Encrypt_SameMessageTwice_ProducesDifferentCiphertext(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);
        var message = Encoding.UTF8.GetBytes("Identical plaintext");

        var ciphertext1 = ecdsa.IES.Encrypt(message, pk);
        var ciphertext2 = ecdsa.IES.Encrypt(message, pk);

        Assert.That(ciphertext1.SequenceEqual(ciphertext2), Is.False, "Encrypting the same message twice must produce different ciphertexts (ephemeral key must be fresh)");

        // Both must still decrypt correctly
        Assert.That(ecdsa.IES.TryDecrypt(ciphertext1, out var dec1, sk), Is.True);
        Assert.That(ecdsa.IES.TryDecrypt(ciphertext2, out var dec2, sk), Is.True);
        Assert.That(message.SequenceEqual(dec1), Is.True);
        Assert.That(message.SequenceEqual(dec2), Is.True);
    }

    #endregion

    #region Round-Trip Integrity (Various Message Sizes)

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1, 1)]
    [TestCase(ECDSAKeyType.SECP256K1, 15)]
    [TestCase(ECDSAKeyType.SECP256K1, 16)]
    [TestCase(ECDSAKeyType.SECP256K1, 17)]
    [TestCase(ECDSAKeyType.SECP256K1, 31)]
    [TestCase(ECDSAKeyType.SECP256K1, 32)]
    [TestCase(ECDSAKeyType.SECP256K1, 33)]
    [TestCase(ECDSAKeyType.SECP256K1, 255)]
    [TestCase(ECDSAKeyType.SECP256K1, 256)]
    [TestCase(ECDSAKeyType.SECP256K1, 1024)]
    [TestCase(ECDSAKeyType.SECP256K1, 4096)]
    public void EncryptDecrypt_VariousMessageSizes(ECDSAKeyType keyType, int messageSize) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);
        var message = RandomBytes(messageSize);

        var ciphertext = ecdsa.IES.Encrypt(message, pk);
        Assert.That(ecdsa.IES.TryDecrypt(ciphertext, out var decrypted, sk), Is.True, $"Decryption failed for message size {messageSize}");
        Assert.That(message.SequenceEqual(decrypted), Is.True, $"Decrypted message differs from original for size {messageSize}");
    }

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void EncryptDecrypt_BlockAlignedMessages(ECDSAKeyType keyType) {
        // AES block size boundaries are critical for padding correctness
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);

        foreach (var size in new[] { 16, 32, 48, 64, 128 }) {
            var message = RandomBytes(size);
            var ciphertext = ecdsa.IES.Encrypt(message, pk);
            Assert.That(ecdsa.IES.TryDecrypt(ciphertext, out var decrypted, sk), Is.True, $"Decryption failed at block-aligned size {size}");
            Assert.That(message.SequenceEqual(decrypted), Is.True, $"Decrypted message mismatch at block-aligned size {size}");
        }
    }

    #endregion

    #region Ciphertext Reordering / Splicing

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_SplicedCiphertexts_Fails(ECDSAKeyType keyType) {
        // Take the first half of one ciphertext and second half of another
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);

        var ct1 = ecdsa.IES.Encrypt(Encoding.UTF8.GetBytes("Message one"), pk);
        var ct2 = ecdsa.IES.Encrypt(Encoding.UTF8.GetBytes("Message two"), pk);

        var minLen = Math.Min(ct1.Length, ct2.Length);
        var spliced = new byte[minLen];
        var half = minLen / 2;
        Array.Copy(ct1, 0, spliced, 0, half);
        Array.Copy(ct2, half, spliced, half, minLen - half);

        Assert.That(ecdsa.IES.TryDecrypt(spliced, out _, sk), Is.False, "Spliced ciphertext from two different encryptions must fail decryption");
    }

    #endregion

    #region Empty / Minimal Input

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_EmptyInput_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        Assert.That(ecdsa.IES.TryDecrypt(Array.Empty<byte>(), out _, sk), Is.False, "Decryption of empty input must fail gracefully");
    }

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_SingleByte_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        Assert.That(ecdsa.IES.TryDecrypt(new byte[] { 0x42 }, out _, sk), Is.False, "Decryption of a single byte must fail gracefully");
    }

    #endregion

    #region Cross-Curve Isolation

    [Test]
    public void Decrypt_CrossCurve_Fails() {
        // Encrypt with SECP256K1, try to decrypt with SECP384R1 key
        var ecdsa256 = new ECDSA(ECDSAKeyType.SECP256K1);
        var ecdsa384 = new ECDSA(ECDSAKeyType.SECP384R1);

        var sk256 = ecdsa256.GeneratePrivateKey();
        var pk256 = ecdsa256.DerivePublicKey(sk256);
        var sk384 = ecdsa384.GeneratePrivateKey();

        var message = Encoding.UTF8.GetBytes("Cross-curve test");
        var ciphertext = ecdsa256.IES.Encrypt(message, pk256);

        Assert.That(ecdsa384.IES.TryDecrypt(ciphertext, out _, sk384), Is.False, "Ciphertext encrypted for one curve must not decrypt with a key from a different curve");
    }

    #endregion

    #region MAC Forgery Resistance

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1)]
    public void Decrypt_ModifiedMACBytes_Fails(ECDSAKeyType keyType) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);
        var message = Encoding.UTF8.GetBytes("MAC forgery resistance test");
        var ciphertext = ecdsa.IES.Encrypt(message, pk);

        // Flip bits in the last 16 bytes (where the MAC likely resides)
        var tampered = (byte[])ciphertext.Clone();
        for (var i = Math.Max(0, tampered.Length - 16); i < tampered.Length; i++)
            tampered[i] ^= 0xFF;

        Assert.That(ecdsa.IES.TryDecrypt(tampered, out _, sk), Is.False, "Decryption must fail when MAC bytes are modified");
    }

    #endregion

    #region Repeated Encrypt/Decrypt Stress

    [Test]
    [TestCase(ECDSAKeyType.SECP256K1, 50)]
    public void EncryptDecrypt_RepeatedOperations_AllSucceed(ECDSAKeyType keyType, int iterations) {
        var ecdsa = new ECDSA(keyType);
        var sk = ecdsa.GeneratePrivateKey();
        var pk = ecdsa.DerivePublicKey(sk);

        for (var i = 0; i < iterations; i++) {
            var message = RandomBytes(new Random(i).Next(1, 512));
            var ciphertext = ecdsa.IES.Encrypt(message, pk);
            Assert.That(ecdsa.IES.TryDecrypt(ciphertext, out var decrypted, sk), Is.True, $"Decryption failed at iteration {i}");
            Assert.That(message.SequenceEqual(decrypted), Is.True, $"Decrypted message mismatch at iteration {i}");
        }
    }

    #endregion
}
