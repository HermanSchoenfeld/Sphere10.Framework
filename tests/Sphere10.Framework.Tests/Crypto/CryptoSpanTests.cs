// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CryptoSpanTests {
	[Test]
	public void IterateRetainsLegacyOversizedDestinationBehavior([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF algorithm, [Values(0, 1, 7)] int iterations) {
		var size = Hashers.GetDigestSizeBytes(algorithm);
		var input = new Random(71).NextBytes(size);
		var destination = Enumerable.Repeat((byte)0xA5, size + 17).ToArray();
		if (iterations == 0) {
			Hashers.Iterate(algorithm, input, iterations, destination);
			Assert.That(destination[..size], Is.EqualTo(input));
		} else {
			Assert.That(() => Hashers.Iterate(algorithm, input, iterations, destination), Throws.TypeOf<ArgumentException>());
			Assert.That(destination[..size], Is.EqualTo(Hashers.Hash(algorithm, input)), "The legacy copy fails after computing the first digest.");
		}
		Assert.That(destination[size..], Is.All.EqualTo(0xA5));
	}

	[Test]
	public void IterateAcceptsOverlappingSpans([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF algorithm, [Values(0, 1, 7)] int iterations) {
		var size = Hashers.GetDigestSizeBytes(algorithm);
		var storage = new Random(72).NextBytes(size + 8);
		var expected = storage.AsSpan(0, size).ToArray();
		for (var i = 0; i < iterations; i++)
			expected = Hashers.Hash(algorithm, expected);
		Hashers.Iterate(algorithm, storage.AsSpan(0, size), iterations, storage.AsSpan(4, size));
		Assert.That(storage.AsSpan(4, size).ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void AggregateRetainsTheWholeDestinationAsItsNextHashInput([Values] bool flip) {
		var digests = new[] { new byte[32], Enumerable.Repeat((byte)1, 32).ToArray(), Enumerable.Repeat((byte)2, 32).ToArray() };
		var destination = Enumerable.Repeat((byte)0xA5, 48).ToArray();
		var expected = (byte[])destination.Clone();
		digests[0].CopyTo(expected, 0);
		foreach (var digest in digests.Skip(1)) {
			var joined = flip ? Hashers.JoinHash(CHF.SHA2_256, digest, expected) : Hashers.JoinHash(CHF.SHA2_256, expected, digest);
			joined.CopyTo(expected, 0);
		}
		Hashers.Aggregate(CHF.SHA2_256, digests, destination, flip);
		Assert.That(destination, Is.EqualTo(expected));
	}

	[Test]
	public void Blake2bReusesDigestStorageWithoutReturningStaleResults() {
		using var hasher = new Blake2b(Blake2b._256Config);
		var input = new byte[] { 1, 2, 3, 4 };
		var expected = Hashers.Hash(CHF.Blake2b_256, input);
		var expectedEmpty = Hashers.Hash(CHF.Blake2b_256, ReadOnlySpan<byte>.Empty);
		Span<byte> output = stackalloc byte[32];
		hasher.Compute(input, output);
		Assert.That(output.ToArray(), Is.EqualTo(expected));
		hasher.GetResult(output);
		Assert.That(output.ToArray(), Is.EqualTo(expected));
		hasher.Reset();
		hasher.GetResult(output);
		Assert.That(output.ToArray(), Is.EqualTo(expectedEmpty));
		hasher.GetResult(output);
		Assert.That(output.ToArray(), Is.EqualTo(expectedEmpty));
		hasher.Transform(input.AsSpan(0, 1));
		hasher.Transform(input.AsSpan(1));
		hasher.GetResult(output);
		Assert.That(output.ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void FailedFinalizationRetainsThePartialDigestWithoutRetry([Values] bool reuseStorage) {
		using var hasher = new PartialDigestHasher { FillValue = 0xA5 };
		var output = new byte[hasher.DigestSize];
		if (reuseStorage) {
			hasher.Compute(Array.Empty<byte>(), output);
			hasher.Reset();
		}
		output.AsSpan().Fill(0xCC);
		var previousCalls = hasher.FinalizeCalls;
		hasher.FillValue = 7;
		hasher.BytesToWrite = 1;
		hasher.FailFinalize = true;
		Assert.That(() => hasher.Compute(Array.Empty<byte>(), output), Throws.TypeOf<InvalidOperationException>());
		Assert.That(output, Is.All.EqualTo(0xCC), "A failed finalization has not yet copied its result to the caller.");
		hasher.FailFinalize = false;
		hasher.FillValue = 9;
		hasher.GetResult(output);
		Assert.That(output, Is.EqualTo(new byte[] { 7, 0, 0, 0 }));
		Assert.That(hasher.FinalizeCalls, Is.EqualTo(previousCalls + 1), "The original buffer remained authoritative after finalization failed.");
		hasher.GetResult(output);
		Assert.That(output, Is.EqualTo(new byte[] { 7, 0, 0, 0 }));
		Assert.That(hasher.FinalizeCalls, Is.EqualTo(previousCalls + 1));
	}

	[Test]
	public void ResetClearsPriorDigestBytesBeforePartialFinalization() {
		using var hasher = new PartialDigestHasher { FillValue = 0xA5 };
		var output = new byte[hasher.DigestSize];
		hasher.Compute(Array.Empty<byte>(), output);
		Assert.That(output, Is.All.EqualTo(0xA5));
		hasher.Reset();
		hasher.FillValue = 7;
		hasher.BytesToWrite = 1;
		hasher.GetResult(output);
		Assert.That(output, Is.EqualTo(new byte[] { 7, 0, 0, 0 }));
		Assert.That(hasher.FinalizeCalls, Is.EqualTo(2));
	}

	[Test]
	public void DerivedInitializationWithoutBaseRetainsPriorDigestBytesUntilReset() {
		using var hasher = new PartialDigestHasher { FillValue = 0xA5 };
		var output = new byte[hasher.DigestSize];
		hasher.Compute(Array.Empty<byte>(), output);
		hasher.FillValue = 7;
		hasher.BytesToWrite = 1;
		hasher.Compute(Array.Empty<byte>(), output);
		Assert.That(output, Is.EqualTo(new byte[] { 7, 0xA5, 0xA5, 0xA5 }));
		Assert.That(hasher.FinalizeCalls, Is.EqualTo(2));
	}

	[Test]
	public void RepeatedBlake2bDigestsHaveBoundedAllocations() {
		using var hasher = new Blake2b(Blake2b._256Config);
		var input = new byte[32];
		var output = new byte[32];
		for (var i = 0; i < 32; i++)
			hasher.Compute(input, output);
		var start = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 1000; i++)
			hasher.Compute(input, output);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
		TestContext.Out.WriteLine($"1000 repeated Blake2b-256 digests: {allocated:N0} managed bytes");
		Assert.That(output, Is.EqualTo(Hashers.Hash(CHF.Blake2b_256, input)));
		Assert.That(allocated, Is.LessThan(8192), "Digest scratch storage must be reused across computations");
	}

	[Test]
	public void IteratedBlake2bHasBoundedAllocations() {
		var input = new byte[32];
		var output = new byte[32];
		for (var i = 0; i < 32; i++)
			Hashers.Iterate(CHF.Blake2b_256, input, 32, output);
		var start = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 1000; i++)
			Hashers.Iterate(CHF.Blake2b_256, input, 32, output);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
		TestContext.Out.WriteLine($"1000 Blake2b-256 chains of 32: {allocated:N0} managed bytes");
		Assert.That(output, Is.EqualTo(Hashers.Iterate(CHF.Blake2b_256, input, 32)));
		Assert.That(allocated, Is.LessThan(512_000), "Hash-chain allocation must not scale with its iteration count");
	}

	[Test]
	public void WotsRowsMatchIndependentHashChains([Values] bool optimized) {
		var wots = new WOTS(4, CHF.SHA2_256, optimized);
		var keys = wots.GenerateKeys(new byte[] { 3, 1, 4, 1, 5 });
		var digest = Hashers.Hash(CHF.SHA2_256, new byte[] { 2, 7, 1, 8 });
		var signature = wots.SignDigest(keys.PrivateKey, digest);
		var originalPrivateKey = keys.PrivateKey.ToFlatArray();
		for (var row = 0; row < wots.Config.SignatureDigits; row++) {
			// WOTS numbers each digit from its first bit as the least-significant bit.
			var digit = 0;
			for (var bit = 0; bit < 4; bit++)
				digit |= ((digest[(4 * row + bit) / 8] >> (7 - (4 * row + bit) % 8)) & 1) << bit;
			var expected = keys.PrivateKey.GetRow(row).ToArray();
			for (var step = digit; step < wots.Config.ChainLength; step++)
				expected = Hashers.Hash(CHF.SHA2_256, expected);
			Assert.That(signature.GetRow(row).ToArray(), Is.EqualTo(expected));
		}
		Assert.That(wots.VerifyDigest(signature, keys.PublicKey, digest), Is.True);
		Assert.That(keys.PrivateKey.ToFlatArray(), Is.EqualTo(originalPrivateKey));
		signature[0, 0] ^= 1;
		Assert.That(wots.VerifyDigest(signature, keys.PublicKey, digest), Is.False);
	}

	[TestCase(false, "1c428782b53bcd8cc4e305937f7e57521291de9a5d276e41d706ea04dc428069")]
	[TestCase(true, "cc60c14c8d163a690bcceec5b3f930a36e7faa89f3f1d074eb04cd9f86081857")]
	public void DeterministicWotsWireVector(bool optimized, string expectedPublicHash) {
		var wots = new WOTS(4, CHF.SHA2_256, optimized);
		var keys = wots.GenerateKeys(new byte[] { 3, 1, 4, 1, 5 });
		var digest = Hashers.Hash(CHF.SHA2_256, new byte[] { 2, 7, 1, 8 });
		var signature = wots.SignDigest(keys.PrivateKey, digest);
		var publicHash = Hashers.Hash(CHF.SHA2_256, keys.PublicKey.AsFlatSpan());
		var signatureHash = Hashers.Hash(CHF.SHA2_256, signature.AsFlatSpan());
		TestContext.Out.WriteLine($"WOTS optimized={optimized}: public={publicHash.ToHexString()}, signature={signatureHash.ToHexString()}");
		Assert.That(publicHash, Is.EqualTo(expectedPublicHash.ToHexByteArray()));
		Assert.That(signatureHash, Is.EqualTo("9a5a4c11c361e3342c4374dcf1d9da78015a92a593fbead84271aa03ed6b689e".ToHexByteArray()));
		Assert.That(wots.VerifyDigest(signature, keys.PublicKey, digest), Is.True);
	}

	[TestCase(false, "d0e2cd64c0114eb2db3c23b1d905f8023072caacea428539e0b9069ecf89e496")]
	[TestCase(true, "13f7000295a42650f215f06d596a727d5f3b1fbdea1663b61dc880f8bd51a519")]
	public void DeterministicAmsWireVector(bool optimized, string expectedSignatureHash) {
		var scheme = new AMS(new WOTS(2, CHF.SHA2_256, optimized), 3);
		var rawKey = (byte[])scheme.GeneratePrivateKey(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray()).RawBytes.Clone();
		rawKey[1] = 3;
		Assert.That(scheme.TryParsePrivateKey(rawKey, out var key), Is.True);
		Assert.That(key.Height, Is.EqualTo(3));
		var publicKey = scheme.DerivePublicKeyForBatch(key, 123, true);
		var digest = scheme.CalculateMessageDigest(new byte[] { 1, 2, 3 });
		var signature = scheme.SignDigest(key, digest, 123, 7);
		var publicHash = Hashers.Hash(CHF.SHA2_256, publicKey.RawBytes);
		var signatureHash = Hashers.Hash(CHF.SHA2_256, signature);
		TestContext.Out.WriteLine($"AMS optimized={optimized}: public={publicHash.ToHexString()}, signature={signatureHash.ToHexString()}");
		Assert.That(publicHash, Is.EqualTo("001e2bb82ac96b88428ef1d1e1c5954b49a1d644497e4273364dd21d5bc48c71".ToHexByteArray()));
		Assert.That(signatureHash, Is.EqualTo(expectedSignatureHash.ToHexByteArray()));
		Assert.That(scheme.VerifyDigest(signature, digest, publicKey.RawBytes), Is.True);
	}

	[Test]
	public void SeededWotsMatchesGeneratorContract([Values(0, 32, 1024)] int seedLength, [Values] bool optimized) {
		var wots = new WOTS(2, CHF.Blake2b_256, optimized);
		var seed = new Random(76).NextBytes(seedLength);
		var enumeratedSeed = new byte[seedLength + 1];
		seed.CopyTo(enumeratedSeed, 1);
		var expected = wots.GenerateKeys(index => {
			enumeratedSeed[0] = (byte)index;
			return Hashers.Iterate(wots.Config.HashFunction, enumeratedSeed, 2);
		});
		var actual = wots.GenerateKeys(seed);
		Assert.That(actual.PrivateKey, Is.EqualTo(expected.PrivateKey));
		Assert.That(actual.PublicKey, Is.EqualTo(expected.PublicKey));
		Assert.That(actual.PublicKeyHash.Value, Is.EqualTo(expected.PublicKeyHash.Value));
	}

	[Test]
	public void MerkleProofSupportsIrregularInitialPreimages([Values(CHF.ConcatBytes, CHF.SHA2_256, CHF.Blake2b_128)] CHF algorithm, [Values(0, 1, 7)] int depth) {
		var start = new byte[] { 1, 2, 3 };
		var proof = Enumerable.Range(0, depth).Select(i => Enumerable.Repeat((byte)(i + 1), i + 5).ToArray()).ToArray();
		var flags = Enumerable.Range(0, depth).Select(i => i % 2 == 0).ToArray();
		var expected = start;
		for (var i = 0; i < depth; i++)
			expected = flags[i] ? Hashers.JoinHash(algorithm, expected, proof[i]) : Hashers.JoinHash(algorithm, proof[i], expected);
		Assert.That(MerkleMath.VerifyProof(algorithm, start, proof, flags, expected), Is.True);
		expected = (byte[])expected.Clone();
		expected[0] ^= 1;
		Assert.That(MerkleMath.VerifyProof(algorithm, start, proof, flags, expected), Is.False);
	}

	[Test]
	public void AmsSignatureRoundTripsAcrossBatches([Values] bool sharp, [Values(0, 3)] int height) {
		AMS scheme = sharp ? new WAMSSharp(height, 2, CHF.Blake2b_128) : new WAMS(height, 2, CHF.SHA2_256);
		var rawKey = (byte[])scheme.GeneratePrivateKey(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray()).RawBytes.Clone();
		// The existing parser reads height from byte 1; select real proof heights explicitly.
		rawKey[1] = (byte)height;
		Assert.That(scheme.TryParsePrivateKey(rawKey, out var key), Is.True);
		Assert.That(key.Height, Is.EqualTo(height));
		var publicKey = scheme.DerivePublicKeyForBatch(key, 123, true);
		var digest = scheme.CalculateMessageDigest(new byte[] { 1, 2, 3 });
		var index = (1 << key.Height) - 1;
		var signature = scheme.SignDigest(key, digest, 123, index);
		Assert.That(scheme.VerifyDigest(signature, digest, publicKey.RawBytes), Is.True);
		Assert.That(signature.Length, Is.EqualTo(3 + scheme.Config.OTS.PublicKeySize.Length * scheme.Config.OTS.PublicKeySize.Width
			+ scheme.Config.OTS.SignatureSize.Length * scheme.Config.OTS.SignatureSize.Width + key.Height * scheme.Config.OTS.DigestSize));
		signature[3] ^= 1;
		Assert.That(scheme.VerifyDigest(signature, digest, publicKey.RawBytes), Is.False);
	}

	[Test]
	public void SecureEraseDoesNotAllocateAnotherSecretSizedArray() {
		var secret = Enumerable.Repeat((byte)0xA5, 1024 * 1024).ToArray();
		Tools.Crypto.SecureErase(new byte[32]);
		var start = GC.GetAllocatedBytesForCurrentThread();
		Tools.Crypto.SecureErase(secret);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
		TestContext.Out.WriteLine($"Overwrite 1 MiB secret: {allocated:N0} managed bytes");
		Assert.That(secret.Distinct().Take(2).Count(), Is.EqualTo(2));
		Assert.That(allocated, Is.LessThan(8192));
	}

	private sealed class PartialDigestHasher : HashFunctionBase {
		public override int DigestSize => 4;
		public byte FillValue { get; set; }
		public int BytesToWrite { get; set; } = 4;
		public bool FailFinalize { get; set; }
		public int FinalizeCalls { get; private set; }

		public override void Initialize() {
			// Reset invalidated the original cached digest even if a derived initializer did not call base.
		}

		protected override void Finalize(Span<byte> digest) {
			FinalizeCalls++;
			digest[..BytesToWrite].Fill(FillValue);
			if (FailFinalize)
				throw new InvalidOperationException("Injected finalization failure.");
		}

		public override object Clone() => throw new NotSupportedException();
	}
}
