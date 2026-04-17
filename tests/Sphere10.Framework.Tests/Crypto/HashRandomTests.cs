// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Maths;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Sphere10.Framework.Tests;

[TestFixture]
public class HashRandomTests {


	[Test]
	public void BadCHF() {
		Assert.That(() => new HashRandom(CHF.ConcatBytes, Guid.Empty.ToByteArray()), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void AcceptsMinSeed() {
		var seed = Tools.Array.Gen<byte>(HashRandom.MinimumSeedLength, 0);
		Assert.That(() => new HashRandom(seed), Throws.Nothing);
	}


	[Test]
	public void BadSeed_1() {
		var seed = Tools.Array.Gen<byte>(HashRandom.MinimumSeedLength - 1, 0);
		Assert.That(() => new HashRandom(seed), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}


	[Test]
	public void BadSeed_2() {
		Assert.That(() => new HashRandom(null), Throws.ArgumentNullException);
	}


	[Test]
	public void GeneratesCorrectly([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf) {
		const int TotalHashIterations = 100;
		var digestSize = Hashers.GetDigestSizeBytes(chf);
		Debug.Assert(digestSize > 0);
		var seed = new Random(31337).NextBytes(digestSize);

		// Calculate all the bytes of iterating the seed TotalHashIterations times
		var expected = new ByteArrayBuilder();
		var lastValue = seed;
		for (var i = 0; i < TotalHashIterations; i++) {
			var nextHash = Hashers.Hash(chf, Tools.Array.Concat<byte>(lastValue, EndianBitConverter.Little.GetBytes((long)i)));
			expected.Append(nextHash);
			lastValue = nextHash;
		}

		var hashRandom = new HashRandom(chf, seed);
		var result = new ByteArrayBuilder();

		// Gen nothing
		result.Append(hashRandom.NextBytes(0));

		// Take first byte
		result.Append(hashRandom.NextBytes(1));

		// Gen nothing
		result.Append(hashRandom.NextBytes(0));

		// Take remaining bytes of first iteration
		result.Append(hashRandom.NextBytes(digestSize - 1));

		// Gen nothing
		result.Append(hashRandom.NextBytes(0));

		// Take full second iteration
		result.Append(hashRandom.NextBytes(digestSize));

		// Take 3rd iteration except last byte
		result.Append(hashRandom.NextBytes(digestSize - 1));

		// Gen nothing
		result.Append(hashRandom.NextBytes(0));

		// Take 2 bytes (last byte of 3rd iteration and first bytes of 4th iteration)
		result.Append(hashRandom.NextBytes(2));

		// Gen nothing
		result.Append(hashRandom.NextBytes(0));

		// Take remaining bytes of 4th iteration and all bytes from remaining iterations
		result.Append(hashRandom.NextBytes((digestSize - 1) + digestSize * (TotalHashIterations - 4)));

		// Gen nothing
		result.Append(hashRandom.NextBytes(0));


		Assert.That(result.ToArray(), Is.EqualTo(expected.ToArray()));

	}


	#region Determinism

	[Test]
	public void SameSeed_ProducesSameSequence() {
		var seed = new Random(12345).NextBytes(32);
		var rng1 = new HashRandom(seed);
		var rng2 = new HashRandom(seed);
		var bytes1 = rng1.NextBytes(256);
		var bytes2 = rng2.NextBytes(256);
		ClassicAssert.AreEqual(bytes1, bytes2,
			"Same seed must produce identical byte sequences");
	}

	[Test]
	public void DifferentSeeds_ProduceDifferentSequences() {
		var rng1 = new HashRandom(new Random(1).NextBytes(32));
		var rng2 = new HashRandom(new Random(2).NextBytes(32));
		var bytes1 = rng1.NextBytes(64);
		var bytes2 = rng2.NextBytes(64);
		ClassicAssert.IsFalse(bytes1.SequenceEqual(bytes2),
			"Different seeds must produce different sequences");
	}

	[Test]
	public void SameSeed_DifferentCHF_ProduceDifferentSequences() {
		var seed = new Random(99).NextBytes(32);
		var rng256 = new HashRandom(CHF.SHA2_256, seed);
		var rng128 = new HashRandom(CHF.Blake2b_128, seed);
		var bytes256 = rng256.NextBytes(64);
		var bytes128 = rng128.NextBytes(64);
		ClassicAssert.IsFalse(bytes256.SequenceEqual(bytes128),
			"Same seed with different CHF must produce different sequences");
	}

	[Test]
	public void Determinism_AcrossMultipleCalls() {
		var seed = new Random(42).NextBytes(32);
		var rng1 = new HashRandom(seed);
		var rng2 = new HashRandom(seed);
		// rng1: read in small chunks
		var result1 = new ByteArrayBuilder();
		result1.Append(rng1.NextBytes(7));
		result1.Append(rng1.NextBytes(13));
		result1.Append(rng1.NextBytes(44));
		// rng2: read in one call
		var result2 = rng2.NextBytes(64);
		ClassicAssert.AreEqual(result1.ToArray(), result2,
			"Splitting reads across calls must produce the same concatenated result");
	}

	#endregion

	#region Output Quality

	[Test]
	public void Output_IsNotAllZeros() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var bytes = rng.NextBytes(1024);
		ClassicAssert.IsFalse(bytes.All(b => b == 0), "Output must not be all zeros");
	}

	[Test]
	public void Output_IsNotAllSameByte() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var bytes = rng.NextBytes(1024);
		ClassicAssert.IsTrue(bytes.Distinct().Count() > 1,
			"Output must contain more than one distinct byte value");
	}

	[Test]
	public void Output_ByteDistribution_IsReasonablyUniform() {
		var rng = new HashRandom(new Random(7).NextBytes(32));
		var bytes = rng.NextBytes(256 * 200); // 51200 bytes — expect ~200 per bucket
		var counts = new int[256];
		foreach (var b in bytes)
			counts[b]++;
		var min = counts.Min();
		var max = counts.Max();
		// With 200 expected per bucket, a 4x range is extremely generous for a CSPRNG
		ClassicAssert.IsTrue(min > 50, $"Minimum bucket count {min} is suspiciously low");
		ClassicAssert.IsTrue(max < 400, $"Maximum bucket count {max} is suspiciously high");
	}

	[Test]
	public void SuccessiveBlocks_AreNotIdentical() {
		var rng = new HashRandom(new Random(3).NextBytes(32));
		var block1 = rng.NextBytes(32);
		var block2 = rng.NextBytes(32);
		ClassicAssert.IsFalse(block1.SequenceEqual(block2),
			"Successive output blocks must differ");
	}

	#endregion

	#region Edge Cases

	[Test]
	public void NextBytes_ZeroLength_ReturnsEmpty() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var bytes = rng.NextBytes(0);
		ClassicAssert.AreEqual(0, bytes.Length);
	}

	[Test]
	public void NextBytes_SingleByte_Succeeds() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var bytes = rng.NextBytes(1);
		ClassicAssert.AreEqual(1, bytes.Length);
	}

	[Test]
	public void NextBytes_ExactDigestSize([Values(CHF.SHA2_256, CHF.Blake2b_128)] CHF chf) {
		var digestSize = Hashers.GetDigestSizeBytes(chf);
		var rng = new HashRandom(chf, new Random(1).NextBytes(32));
		var bytes = rng.NextBytes(digestSize);
		ClassicAssert.AreEqual(digestSize, bytes.Length);
	}

	[Test]
	public void NextBytes_LargerThanDigest_Succeeds() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var digestSize = Hashers.GetDigestSizeBytes(CHF.SHA2_256);
		var bytes = rng.NextBytes(digestSize * 10 + 7);
		ClassicAssert.AreEqual(digestSize * 10 + 7, bytes.Length);
	}

	[Test]
	public void NextBytes_VeryLargeRequest_Succeeds() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var bytes = rng.NextBytes(100_000);
		ClassicAssert.AreEqual(100_000, bytes.Length);
		ClassicAssert.IsFalse(bytes.All(b => b == 0));
	}

	[Test]
	public void Constructor_MinimumSeedLength_Works() {
		var seed = new byte[HashRandom.MinimumSeedLength];
		seed[0] = 1;
		var rng = new HashRandom(seed);
		var bytes = rng.NextBytes(32);
		ClassicAssert.IsFalse(bytes.All(b => b == 0));
	}

	[Test]
	public void Constructor_DefaultParameterless_Succeeds() {
		var rng = new HashRandom();
		var bytes = rng.NextBytes(32);
		ClassicAssert.AreEqual(32, bytes.Length);
		ClassicAssert.IsFalse(bytes.All(b => b == 0));
	}

	#endregion

	#region Thread Safety

	[Test]
	public void ThreadSafety_ConcurrentReads_NoCrash() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var tasks = Enumerable.Range(0, 8).Select(_ =>
			Task.Run(() => rng.NextBytes(1024))
		).ToArray();
		Assert.DoesNotThrow(() => Task.WaitAll(tasks));
		foreach (var task in tasks)
			ClassicAssert.AreEqual(1024, task.Result.Length);
	}

	[Test]
	public void ThreadSafety_TotalBytesProduced_IsCorrect() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var allBytes = new byte[8][];
		var tasks = Enumerable.Range(0, 8).Select(i =>
			Task.Run(() => { allBytes[i] = rng.NextBytes(256); })
		).ToArray();
		Task.WaitAll(tasks);
		// All tasks got their bytes
		foreach (var bytes in allBytes) {
			ClassicAssert.IsNotNull(bytes);
			ClassicAssert.AreEqual(256, bytes.Length);
		}
	}

	#endregion

	#region Span Overload

	[Test]
	public void SpanOverload_FillsExistingBuffer() {
		var rng = new HashRandom(new Random(1).NextBytes(32));
		var buffer = new byte[64];
		rng.NextBytes(buffer.AsSpan());
		ClassicAssert.IsFalse(buffer.All(b => b == 0),
			"Span overload must fill the provided buffer");
	}

	[Test]
	public void SpanOverload_MatchesArrayOverload() {
		var seed = new Random(55).NextBytes(32);
		var rng1 = new HashRandom(seed);
		var rng2 = new HashRandom(seed);
		var arrayResult = rng1.NextBytes(128);
		var spanBuffer = new byte[128];
		rng2.NextBytes(spanBuffer.AsSpan());
		ClassicAssert.AreEqual(arrayResult, spanBuffer,
			"Span and array overloads with same seed must produce identical output");
	}

	#endregion

}

