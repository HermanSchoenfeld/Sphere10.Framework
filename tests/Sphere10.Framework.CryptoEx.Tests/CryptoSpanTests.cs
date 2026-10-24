// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using HashLib4CSharp.Base;
using NUnit.Framework;
using Sphere10.Framework.CryptoEx.EC;
using Sphere10.Framework.CryptoEx.EC.Schnorr;

namespace Sphere10.Framework.CryptoEx.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CryptoSpanTests {
	[TestCase("", "0")]
	[TestCase("00", "0")]
	[TestCase("80", "128")]
	[TestCase("FF", "255")]
	[TestCase("0080", "128")]
	[TestCase("FF0001", "16711681")]
	public void SchnorrArrayIntegerConversionRetainsUnsignedBigEndianValues(string hexadecimal, string expected) {
		Assert.That(Schnorr.BytesToBigIntPositive(hexadecimal.ToHexByteArray()).ToString(), Is.EqualTo(expected));
	}

	[Test]
	public void SchnorrArrayIntegerConversionRetainsNull() {
		Assert.That(Schnorr.BytesToBigIntPositive((byte[])null), Is.Null);
	}

	[Test]
	public void HashLibHashesSlicesWithoutCopyingPreimages() {
		var input = new Random(73).NextBytes(1024 * 1024 + 4);
		var output = new byte[32];
		using var hasher = new HashLibAdapter(HashFactory.Crypto.CreateSHA2_256());
		for (var i = 0; i < 8; i++)
			hasher.Compute(input.AsSpan(2, input.Length - 4), output);
		var start = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 8; i++)
			hasher.Compute(input.AsSpan(2, input.Length - 4), output);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
		TestContext.Out.WriteLine($"8 HashLib SHA-256 hashes of 1 MiB slices: {allocated:N0} managed bytes");
		Assert.That(output, Is.EqualTo(Hashers.Hash(CHF.SHA2_256, input.AsSpan(2, input.Length - 4))));
		Assert.That(allocated, Is.LessThan(1024 * 1024), "The adapter must not allocate a preimage-sized array per hash");
	}

	[Test]
	public void EcdsaConsumesSeedDigestAndPublicKeySlices([Values(ECDSAKeyType.SECP256K1, ECDSAKeyType.SECP384R1)] ECDSAKeyType keyType) {
		var scheme = new ECDSA(keyType);
		var seed = new byte[] { 99, 1, 2, 3, 4, 99 };
		var privateKey = scheme.GeneratePrivateKey(seed.AsSpan(1, 4));
		Assert.That(privateKey.RawBytes, Is.EqualTo(scheme.GeneratePrivateKey(new byte[] { 1, 2, 3, 4 }).RawBytes));
		var publicKey = scheme.DerivePublicKey(privateKey);
		var digest = new Random(74).NextBytes(34);
		var signature = scheme.SignDigest(privateKey, digest.AsSpan(1, 32));
		Assert.That(signature, Is.EqualTo(scheme.SignDigest(privateKey, digest.AsSpan(1, 32).ToArray())));
		var publicKeyStorage = new byte[publicKey.RawBytes.Length + 2];
		publicKey.RawBytes.CopyTo(publicKeyStorage, 1);
		Assert.That(scheme.VerifyDigest(signature, digest.AsSpan(1, 32), publicKeyStorage.AsSpan(1, publicKey.RawBytes.Length)), Is.True);
		digest[1] ^= 1;
		Assert.That(scheme.VerifyDigest(signature, digest.AsSpan(1, 32), publicKeyStorage.AsSpan(1, publicKey.RawBytes.Length)), Is.False);
	}

	[Test]
	public void SchnorrConsumesSlicesAcrossDigestLengths([Values(32, 1024)] int digestLength) {
		var scheme = new Schnorr(ECDSAKeyType.SECP256K1);
		var seed = new byte[] { 99, 1, 2, 3, 4, 99 };
		var privateKey = scheme.GeneratePrivateKey(seed.AsSpan(1, 4));
		Assert.That(privateKey.RawBytes, Is.EqualTo(scheme.GeneratePrivateKey(new byte[] { 1, 2, 3, 4 }).RawBytes));
		var publicKey = scheme.DerivePublicKey(privateKey);
		var digest = new Random(75).NextBytes(digestLength + 2);
		var auxiliary = new byte[34];
		var signature = scheme.SignDigestWithAuxRandomData(privateKey, digest.AsSpan(1, digestLength), auxiliary.AsSpan(1, 32));
		var publicKeyStorage = new byte[publicKey.RawBytes.Length + 2];
		publicKey.RawBytes.CopyTo(publicKeyStorage, 1);
		var signatureStorage = Enumerable.Repeat((byte)0xA5, signature.Length + 2).ToArray();
		signature.CopyTo(signatureStorage, 1);
		Assert.That(scheme.VerifyDigest(signatureStorage.AsSpan(1, signature.Length), digest.AsSpan(1, digestLength), publicKeyStorage.AsSpan(1, 32)), Is.True);
		digest[1] ^= 1;
		Assert.That(scheme.VerifyDigest(signatureStorage.AsSpan(1, signature.Length), digest.AsSpan(1, digestLength), publicKeyStorage.AsSpan(1, 32)), Is.False);
	}
}
