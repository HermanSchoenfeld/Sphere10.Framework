// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class RestorePositionScopePerformanceTests {

	[Test]
	[Category("Performance")]
	public void StaticPageSpanWritesDoNotAllocatePositionScopes([Values] Endianness endianness) {
		const int iterations = 1000;
		const long adjacentItem = 0x1122334455667788L;
		const long replacementItem = 0x0102030405060708L;
		using var stream = new MemoryStream(64);
		var list = new StreamPagedList<long>(PrimitiveSerializer<long>.Instance, stream, endianness, includeListHeader: false);
		list.AddRange(new[] { adjacentItem, 0L });
		var converter = EndianBitConverter.For(endianness);
		ReadOnlySpan<byte> replacement = converter.GetBytes(replacementItem);
		var expected = new byte[16];
		converter.GetBytes(adjacentItem).AsSpan().CopyTo(expected);
		replacement.CopyTo(expected.AsSpan(8));
		stream.Position = 3;
		for (var iteration = 0; iteration < 32; iteration++)
			list.WriteItemBytes(1, 0, replacement);

		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var iteration = 0; iteration < iterations; iteration++)
			list.WriteItemBytes(1, 0, replacement);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.That(stream.Position, Is.EqualTo(3), "Every write must restore the caller's stream position.");
		Assert.That(stream.CanRead, Is.True);
		Assert.That(stream.CanWrite, Is.True);
		Assert.That(stream.ToArray(), Is.EqualTo(expected));
		Assert.That(list.Read(0), Is.EqualTo(adjacentItem));
		Assert.That(list.Read(1), Is.EqualTo(replacementItem));
		TestContext.WriteLine($"{iterations} static-page span writes ({endianness}): {allocated:N0} allocated bytes.");
		Assert.That(allocated, Is.Zero, "Static-page span writes must not allocate a position-restoration scope per operation.");
	}
}
