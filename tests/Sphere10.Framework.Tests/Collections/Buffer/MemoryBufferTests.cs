// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using Sphere10.Framework.NUnit;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemoryBufferTests {

	[Test]
	public void RangeSearchRetainsCapacityMatchesAndStorageOrder() {
		var buffer = new MemoryBuffer(6, 6);
		buffer.Add(7);
		buffer.Add(7);
		Assert.That(buffer.IndexOfRange(new byte[] { 7, 0, 7 }), Is.EqualTo(new long[] { 0, 1, 2, 3, 4, 5 }));
		Assert.That(buffer.IndexOfRange(new byte[] { 7 }), Is.EqualTo(new long[] { 0, 1 }));
		Assert.That(buffer.IndexOfRange(new byte[] { 99 }), Is.Empty);
		Assert.That(buffer.Count, Is.EqualTo(2));
	}

	[Test]
	public void ScalarSearchRetainsLegacyCardinalityAndCapacityBehavior() {
		var buffer = new MemoryBuffer(6, 6);
		buffer.Add(7);
		buffer.Add(7);
		Assert.That(() => buffer.IndexOfL(7), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => buffer.IndexOfL(99), Throws.TypeOf<InvalidOperationException>());
		Assert.That(buffer.Contains(0), Is.True);
		Assert.That(() => buffer.Contains(99), Throws.TypeOf<InvalidOperationException>());
		Assert.That(buffer.Remove(7), Is.True);
		Assert.That(buffer.Count, Is.EqualTo(1));
		Assert.That(buffer.Read(0), Is.EqualTo(7));
	}

	[TestCase(5)]
	[TestCase(8)]
	public void AliasedSpanInsertionRetainsItsLegacyGrowthDependentResult(int initialCapacity) {
		var buffer = new MemoryBuffer(initialCapacity, 16);
		buffer.AddRange(new byte[] { 1, 2, 3, 4, 5 }.AsSpan());
		buffer.InsertRange(1, buffer.ReadSpan(2, 3));
		// Growth detaches the source; without growth, shifting storage also changes the aliased input.
		var expected = initialCapacity == 5 ? new byte[] { 1, 3, 4, 5, 2, 3, 4, 5 } : new byte[] { 1, 3, 4, 2, 2, 3, 4, 5 };
		Assert.That(buffer.AsSpan().ToArray(), Is.EqualTo(expected));
	}

	[Test]
	public void ScalarMutationInvalidatesActiveEnumerator() {
		var buffer = new MemoryBuffer();
		buffer.AddRange(new byte[] { 1, 2, 3 }.AsSpan());
		using var enumerator = buffer.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		buffer.Update(1, 9);
		Assert.That(() => enumerator.MoveNext(), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void ScalarAccessMutationCopyAndEnumerationAvoidPerByteArrays() {
		var buffer = new MemoryBuffer(8192, 8192);
		var destination = new byte[4096];
		buffer.Add(7);
		buffer.Update(0, 8);
		buffer.Insert(0, 9);
		buffer.RemoveRange(0, 1);
		buffer.CopyTo(destination, 0);
		foreach (var item in buffer)
			_ = item;
		buffer.Clear();
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var index = 0; index < 4096; index++)
			buffer.Add((byte)index);
		var checksum = 0;
		for (var index = 0; index < 4096; index++) {
			checksum += buffer.Read(index);
			buffer.Update(index, (byte)(index + 1));
		}
		buffer.Insert(0, 99);
		buffer.RemoveRange(0, 1);
		buffer.CopyTo(destination, 0);
		var enumeratedChecksum = 0;
		foreach (var item in buffer)
			enumeratedChecksum += item;
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(buffer.Count, Is.EqualTo(4096));
		Assert.That(checksum, Is.EqualTo(522240));
		Assert.That(enumeratedChecksum, Is.EqualTo(checksum));
		Assert.That(destination, Is.EqualTo(buffer.AsSpan().ToArray()));
		Assert.That(allocated, Is.LessThan(1024), "Contiguous byte storage must not allocate arrays for each scalar access, mutation or copy.");
		TestContext.WriteLine($"MemoryBuffer scalar access/mutation/copy/enumeration workload allocated {allocated} bytes; search and remove-by-value are excluded.");
	}

	[Test]
	public void CopyToRetainsLegacyPartialWritesWhenTheDestinationIsTooSmall() {
		var buffer = new MemoryBuffer(new byte[] { 1, 2, 3 });
		var destination = new byte[2];
		Assert.That(() => buffer.CopyTo(destination, 0), Throws.TypeOf<IndexOutOfRangeException>());
		Assert.That(destination, Is.EqualTo(new byte[] { 1, 2 }));
	}

	[Test]
	public void EmptyCopyToRetainsLegacyAcceptanceOfUnusedInvalidArguments() {
		var buffer = new MemoryBuffer();
		Assert.That(() => buffer.CopyTo(null, -1), Throws.Nothing);
		Assert.That(() => buffer.CopyTo(Array.Empty<byte>(), int.MaxValue), Throws.Nothing);
	}

	[Test]
	public void IntegrationTests(
		[Values(0, 3, 111)] int startCapacity,
		[Values(1, 391)] int growCapacity,
		[Values(71, 2177)] int maxCapacity) {
		var list = new MemoryBuffer(startCapacity, growCapacity, maxCapacity);
		AssertEx.ListIntegrationTest<byte>(list, maxCapacity, (rng, i) => rng.NextBytes(i), mutateFromEndOnly: true);
	}
}

