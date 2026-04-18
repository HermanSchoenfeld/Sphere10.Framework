// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.CryptoEx.EC;
using Sphere10.Framework.CryptoEx.EC.Schnorr;

namespace Sphere10.Framework.CryptoEx.Tests;

/// <summary>
/// Security-focused tests for the Schnorr signature implementation.
/// Covers trait flags, signature malleability, key validation edge cases,
/// malformed input handling, and known attack scenarios.
/// </summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SchnorrSecurityTests {

    private static Schnorr CreateSchnorr() => new(ECDSAKeyType.SECP256K1);

    private static byte[] RandomBytes(int length) =>
        Tools.Crypto.GenerateCryptographicallyRandomBytes(length);

    private static byte[] RandomMessageDigest() =>
        Hashers.Hash(CHF.SHA2_256, RandomBytes(64));

    #region Trait Flags

    [Test]
    public void Traits_HasSchnorrFlag() {
        var schnorr = CreateSchnorr();
        Assert.That(schnorr.Traits.HasFlag(DigitalSignatureSchemeTraits.Schnorr), Is.True, "Schnorr trait flag must be set");
    }

    [Test]
    public void Traits_HasSupportsIESFlag() {
        var schnorr = CreateSchnorr();
        Assert.That(schnorr.Traits.HasFlag(DigitalSignatureSchemeTraits.SupportsIES), Is.True, "SupportsIES trait flag must be set");
    }

    #endregion

    #region Signature Length Validation

    [Test]
    public void VerifyDigest_EmptySignature_ReturnsFalse() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var result = schnorr.VerifyDigest(Array.Empty<byte>(), RandomMessageDigest(), pk.RawBytes);
        Assert.That(result, Is.False, "Empty signature must return false, not throw");
    }

    [Test]
    public void VerifyDigest_TruncatedSignature_ReturnsFalse() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);
        // truncate to half
        var truncated = sig.AsSpan().Slice(0, sig.Length / 2).ToArray();
        var result = schnorr.VerifyDigest(truncated, messageDigest, pk.RawBytes);
        Assert.That(result, Is.False, "Truncated signature must return false, not throw");
    }

    [Test]
    public void VerifyDigest_OversizedSignature_ReturnsFalse() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);
        var oversized = new byte[sig.Length + 1];
        Array.Copy(sig, oversized, sig.Length);
        var result = schnorr.VerifyDigest(oversized, messageDigest, pk.RawBytes);
        Assert.That(result, Is.False, "Oversized signature must return false, not throw");
    }

    [Test]
    public void VerifyDigest_SingleByteSignature_ReturnsFalse() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var result = schnorr.VerifyDigest(new byte[] { 0x42 }, RandomMessageDigest(), pk.RawBytes);
        Assert.That(result, Is.False, "Single-byte signature must return false");
    }

    #endregion

    #region Signature Malleability / Forgery

    [Test]
    public void VerifyDigest_FlipEveryBit_AllFail() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);

        for (var byteIdx = 0; byteIdx < sig.Length; byteIdx++) {
            for (var bitIdx = 0; bitIdx < 8; bitIdx++) {
                var tampered = (byte[])sig.Clone();
                tampered[byteIdx] ^= (byte)(1 << bitIdx);
                bool result;
                try {
                    result = schnorr.VerifyDigest(tampered, messageDigest, pk.RawBytes);
                } catch (Exception) {
                    result = false;
                }
                Assert.That(result, Is.False,
                    $"Flipping bit {bitIdx} of byte {byteIdx} must invalidate the signature");
            }
        }
    }

    [Test]
    public void VerifyDigest_SwappedRAndS_Fails() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);
        // Swap R and S components
        var swapped = new byte[sig.Length];
        Array.Copy(sig, schnorr.KeySize, swapped, 0, schnorr.KeySize);
        Array.Copy(sig, 0, swapped, schnorr.KeySize, schnorr.KeySize);
        Assert.That(schnorr.VerifyDigest(swapped, messageDigest, pk.RawBytes), Is.False, "Swapping R and S must invalidate the signature");
    }

    [Test]
    public void VerifyDigest_AllZeroSignature_Fails() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var zeroSig = new byte[2 * schnorr.KeySize];
        bool result;
        try {
            result = schnorr.VerifyDigest(zeroSig, RandomMessageDigest(), pk.RawBytes);
        } catch (Exception) {
            result = false;
        }
        Assert.That(result, Is.False, "All-zero signature must not verify");
    }

    [Test]
    public void VerifyDigest_AllOneSignature_Fails() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var onesSig = new byte[2 * schnorr.KeySize];
        Array.Fill(onesSig, (byte)0xFF);
        bool result;
        try {
            result = schnorr.VerifyDigest(onesSig, RandomMessageDigest(), pk.RawBytes);
        } catch (Exception) {
            result = false;
        }
        Assert.That(result, Is.False, "All-0xFF signature must not verify");
    }

    [Test]
    public void VerifyDigest_WrongMessage_Fails() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);
        var wrongMessage = RandomMessageDigest();
        Assert.That(schnorr.VerifyDigest(sig, wrongMessage, pk.RawBytes), Is.False, "Signature must not verify against a different message");
    }

    [Test]
    public void VerifyDigest_WrongPublicKey_Fails() {
        var schnorr = CreateSchnorr();
        var sk1 = schnorr.GeneratePrivateKey();
        var sk2 = schnorr.GeneratePrivateKey();
        var pk2 = schnorr.DerivePublicKey(sk2);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk1, messageDigest);
        Assert.That(schnorr.VerifyDigest(sig, messageDigest, pk2.RawBytes), Is.False, "Signature must not verify against a different public key");
    }

    [Test]
    public void VerifyDigest_SignatureFromDifferentKey_NotTransferable() {
        var schnorr = CreateSchnorr();
        var messageDigest = RandomMessageDigest();
        // Sign with key A, verify with key B — must fail
        var skA = schnorr.GeneratePrivateKey();
        var skB = schnorr.GeneratePrivateKey();
        var pkB = schnorr.DerivePublicKey(skB);
        var sigA = schnorr.SignDigest(skA, messageDigest);
        Assert.That(schnorr.VerifyDigest(sigA, messageDigest, pkB.RawBytes), Is.False, "Signature from key A must not verify under key B");
    }

    #endregion

    #region Key Derivation Determinism / Isolation

    [Test]
    public void GeneratePrivateKey_SameSeed_ProducesSameKey() {
        var schnorr = CreateSchnorr();
        var seed = RandomBytes(32);
        var sk1 = schnorr.GeneratePrivateKey(seed);
        // Create a fresh Schnorr instance to ensure no shared RNG state
        var schnorr2 = CreateSchnorr();
        var sk2 = schnorr2.GeneratePrivateKey(seed);
        Assert.That(sk2.RawBytes, Is.EqualTo(sk1.RawBytes), "Same seed must produce identical private keys across instances");
    }

    [Test]
    public void GeneratePrivateKey_DifferentSeeds_ProduceDifferentKeys() {
        var schnorr = CreateSchnorr();
        var sk1 = schnorr.GeneratePrivateKey(RandomBytes(32));
        var sk2 = schnorr.GeneratePrivateKey(RandomBytes(32));
        Assert.That(sk1.RawBytes.SequenceEqual(sk2.RawBytes), Is.False, "Different seeds must produce different private keys");
    }

    [Test]
    public void GeneratePrivateKey_NoSeed_ProducesDifferentKeys() {
        var schnorr = CreateSchnorr();
        var sk1 = schnorr.GeneratePrivateKey();
        var sk2 = schnorr.GeneratePrivateKey();
        Assert.That(sk1.RawBytes.SequenceEqual(sk2.RawBytes), Is.False, "Successive seedless generation must produce unique keys");
    }

    #endregion

    #region Nonce Reuse Detection (Signing Consistency)

    [Test]
    public void SignDigest_SameMessageDifferentAux_ProducesDifferentSignatures() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var messageDigest = RandomMessageDigest();
        var sig1 = schnorr.SignDigestWithAuxRandomData(sk, messageDigest, RandomBytes(32));
        var sig2 = schnorr.SignDigestWithAuxRandomData(sk, messageDigest, RandomBytes(32));
        // Overwhelmingly likely to differ (different aux randomness → different nonce)
        Assert.That(sig1.SequenceEqual(sig2), Is.False, "Different auxiliary randomness must produce different signatures");
    }

    [Test]
    public void SignDigest_SameMessageSameAux_ProducesSameSignature() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var messageDigest = RandomMessageDigest();
        var aux = RandomBytes(32);
        var sig1 = schnorr.SignDigestWithAuxRandomData(sk, messageDigest, aux);
        var sig2 = schnorr.SignDigestWithAuxRandomData(sk, messageDigest, aux);
        Assert.That(sig1.SequenceEqual(sig2), Is.True, "Identical inputs must produce identical, deterministic signatures");
    }

    [Test]
    public void SignDigest_DefaultAux_ProducesValidSignature() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);
        Assert.That(schnorr.VerifyDigest(sig, messageDigest, pk.RawBytes), Is.True, "SignDigest with default (random) aux must produce a valid signature");
    }

    #endregion

    #region Edge-Case Key Values

    [Test]
    public void TryParsePrivateKey_AllZeros_Fails() {
        var schnorr = CreateSchnorr();
        var zeroKey = new byte[schnorr.KeySize];
        Assert.That(schnorr.TryParsePrivateKey(zeroKey, out _), Is.False, "Private key of all zeros (outside valid range) must be rejected");
    }

    [Test]
    public void TryParsePublicKey_AllZeros_Fails() {
        var schnorr = CreateSchnorr();
        var zeroKey = new byte[schnorr.KeySize];
        // x=0 may or may not be on the curve, but should be handled gracefully
        bool result;
        try {
            result = schnorr.TryParsePublicKey(zeroKey, out _);
        } catch (Exception) {
            result = false;
        }
        // Either returns false or throws — both are acceptable, but must not return true
        // with an invalid key. If it did return true, it would be a security issue.
        Assert.That(result, Is.False, "Public key of all zeros must be rejected");
    }

    [Test]
    public void TryParsePublicKey_AllOnes_Fails() {
        var schnorr = CreateSchnorr();
        var onesKey = new byte[schnorr.KeySize];
        Array.Fill(onesKey, (byte)0xFF);
        bool result;
        try {
            result = schnorr.TryParsePublicKey(onesKey, out _);
        } catch (Exception) {
            result = false;
        }
        Assert.That(result, Is.False, "Public key of all 0xFF must be rejected (exceeds field)");
    }

    #endregion

    #region Cross-Key Verification (Confused Deputy)

    [Test, Repeat(8)]
    public void VerifyDigest_CrossKeyAttack_Fails() {
        // Attacker has valid (sig, msg, pk_A) and tries to claim it verifies under pk_B
        var schnorr = CreateSchnorr();
        var skA = schnorr.GeneratePrivateKey();
        var skB = schnorr.GeneratePrivateKey();
        var pkB = schnorr.DerivePublicKey(skB);
        var messageDigest = RandomMessageDigest();
        var sigA = schnorr.SignDigest(skA, messageDigest);
        Assert.That(schnorr.VerifyDigest(sigA, messageDigest, pkB.RawBytes), Is.False);
    }

    #endregion

    #region Batch Verification Security

    [Test]
    public void BatchVerifyDigest_SingleForgedSignature_FailsEntireBatch() {
        var schnorr = CreateSchnorr();
        var count = 5;
        var sks = Enumerable.Range(0, count).Select(_ => schnorr.GeneratePrivateKey()).ToArray();
        var pks = sks.Select(sk => schnorr.DerivePublicKey(sk)).ToArray();
        var messages = Enumerable.Range(0, count).Select(_ => RandomMessageDigest()).ToArray();
        var sigs = Enumerable.Range(0, count).Select(i => schnorr.SignDigest(sks[i], messages[i])).ToArray();

        // Verify the honest batch passes
        Assert.That(schnorr.BatchVerifyDigest(sigs, messages, pks.Select(p => p.RawBytes).ToArray()), Is.True);

        // Forge one signature (flip a bit)
        var forgedSigs = (byte[][])sigs.Clone();
        forgedSigs[2] = (byte[])sigs[2].Clone();
        forgedSigs[2][0] ^= 0x01;

        bool result;
        try {
            result = schnorr.BatchVerifyDigest(forgedSigs, messages, pks.Select(p => p.RawBytes).ToArray());
        } catch (Exception) {
            result = false;
        }
        Assert.That(result, Is.False, "A single forged signature must cause the entire batch to fail");
    }

    [Test]
    public void BatchVerifyDigest_SwappedMessageOrder_Fails() {
        var schnorr = CreateSchnorr();
        var count = 3;
        var sks = Enumerable.Range(0, count).Select(_ => schnorr.GeneratePrivateKey()).ToArray();
        var pks = sks.Select(sk => schnorr.DerivePublicKey(sk).RawBytes).ToArray();
        var messages = Enumerable.Range(0, count).Select(_ => RandomMessageDigest()).ToArray();
        var sigs = Enumerable.Range(0, count).Select(i => schnorr.SignDigest(sks[i], messages[i])).ToArray();

        // Swap messages[0] and messages[1]
        var swappedMessages = (byte[][])messages.Clone();
        (swappedMessages[0], swappedMessages[1]) = (swappedMessages[1], swappedMessages[0]);

        bool result;
        try {
            result = schnorr.BatchVerifyDigest(sigs, swappedMessages, pks);
        } catch (Exception) {
            result = false;
        }
        Assert.That(result, Is.False, "Swapping message order in batch verification must fail");
    }

    #endregion

    #region Signature Uniqueness (No Duplicate Nonces)

    [Test]
    public void SignDigest_DifferentMessages_ProduceDifferentRValues() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var sig1 = schnorr.SignDigest(sk, RandomMessageDigest());
        var sig2 = schnorr.SignDigest(sk, RandomMessageDigest());
        // Extract R components (first KeySize bytes)
        var r1 = sig1.AsSpan().Slice(0, schnorr.KeySize).ToArray();
        var r2 = sig2.AsSpan().Slice(0, schnorr.KeySize).ToArray();
        Assert.That(r1.SequenceEqual(r2), Is.False, "Different messages must produce different R nonce points (nonce reuse = catastrophic key leak)");
    }

    #endregion

    #region Self-Verification Round-Trip

    [Test, Repeat(16)]
    public void SignAndVerify_RoundTrip_Succeeds() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var sig = schnorr.SignDigest(sk, messageDigest);
        Assert.That(schnorr.VerifyDigest(sig, messageDigest, pk.RawBytes), Is.True);
    }

    [Test]
    public void SignAndVerify_WithExplicitAuxRandom_Succeeds() {
        var schnorr = CreateSchnorr();
        var sk = schnorr.GeneratePrivateKey();
        var pk = schnorr.DerivePublicKey(sk);
        var messageDigest = RandomMessageDigest();
        var aux = RandomBytes(32);
        var sig = schnorr.SignDigestWithAuxRandomData(sk, messageDigest, aux);
        Assert.That(schnorr.VerifyDigest(sig, messageDigest, pk.RawBytes), Is.True);
    }

    #endregion
}
