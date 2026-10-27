// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamPagedSpanTests {

	[Test]
	public void CallerStorageReadMatchesOwnedReadAndRetainsEvents([Values] StreamPagedListType type, [Values] Endianness endianness, [Values] bool materialize) {
		using var stream = new ShortReadStream();
		var list = new TrackingStreamPagedList(type, stream, endianness);
		list.AddRange(new[] { 0x01020304, 0x10203040, 0x50607080 });
		list.ReadItemBytes(1, 1, 3, out var expected);
		var notifications = new List<string>();
		list.Accessing += _ => notifications.Add("accessing");
		list.PageAccessing += (_, _) => notifications.Add("page accessing");
		list.PageReading += (_, _) => notifications.Add("page reading");
		list.PageRead += (_, _) => notifications.Add("page read");
		list.PageAccessed += (_, _) => notifications.Add("page accessed");
		list.Accessed += _ => notifications.Add("accessed");
		var previousScopes = list.OpenScopes;
		Span<byte> destination = stackalloc byte[5];
		destination.Fill(255);
		if (materialize) {
			Assert.That(list.ReadItemBytes(1, 1, 3, out var owned), Is.EqualTo(3));
			owned.CopyTo(destination.Slice(1, 3));
		} else {
			Assert.That(list.ReadItemBytes(1, 1, destination.Slice(1, 3)), Is.EqualTo(3));
		}
		Assert.That(destination.Slice(1, 3).ToArray(), Is.EqualTo(expected));
		Assert.That(destination[0], Is.EqualTo(255));
		Assert.That(destination[4], Is.EqualTo(255));
		Assert.That(list.OpenScopes, Is.EqualTo(previousScopes + 1));
		Assert.That(notifications, Is.EqualTo(new[] { "accessing", "page accessing", "page reading", "page read", "page accessed", "accessed" }));
	}

	[Test]
	public void CallerStorageReadReturnsShortCountAtUnderlyingEnd([Values] StreamPagedListType type) {
		using var stream = new ShortReadStream();
		var list = new StreamPagedList<int>(type, PrimitiveSerializer<int>.Instance, stream, 12);
		list.AddRange(new[] { 1, 0x01020304 });
		list.ReadItemBytes(1, 2, 5, out var expected);
		Span<byte> destination = stackalloc byte[5];
		destination.Fill(255);
		var count = list.ReadItemBytes(1, 2, destination);
		Assert.That(count, Is.EqualTo(expected.Length));
		Assert.That(destination[..count].ToArray(), Is.EqualTo(expected));
		Assert.That(destination[count..].ToArray(), Is.All.EqualTo(255));
	}

	[Test]
	public void CallerStorageReadChecksItemIndex() {
		using var stream = new MemoryStream();
		var list = new StreamPagedList<int>(PrimitiveSerializer<int>.Instance, stream, includeListHeader: false);
		list.Add(1);
		Assert.That(() => list.ReadItemBytes(1, 0, new byte[1].AsSpan()), Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void DynamicDestinationReadUsesTheSelectedPagesLocalIndex([Values] Endianness endianness) {
		using var stream = new MemoryStream();
		var list = new StreamPagedList<int>(StreamPagedListType.Dynamic, PrimitiveSerializer<int>.Instance, stream, 2, endianness);
		list.AddRange(new[] { 0x11121314, 0x21222324, 0x31323334, 0x41424344 });
		var expected = EndianBitConverter.For(endianness).GetBytes(0x41424344);
		Span<byte> destination = stackalloc byte[2];
		Assert.That(list.ReadItemBytes(3, 1, destination), Is.EqualTo(2));
		Assert.That(destination.ToArray(), Is.EqualTo(expected[1..3]));
		Assert.That(list.Read(2), Is.EqualTo(0x31323334));
	}

	[Test]
	public void DynamicExistingRawItemApisRetainTheirLaterPageAddressingFailure([Values] Endianness endianness) {
		using var stream = new MemoryStream();
		var list = new StreamPagedList<int>(StreamPagedListType.Dynamic, PrimitiveSerializer<int>.Instance, stream, 2, endianness);
		list.AddRange(new[] { 0x11121314, 0x21222324, 0x31323334, 0x41424344 });
		var before = stream.ToArray();
		Assert.That(() => list.ReadItemBytes(3, 0, null, out _), Throws.TypeOf<IndexOutOfRangeException>());
		Assert.That(() => list.ReadItemBytes(3, 0, 4, out _), Throws.TypeOf<IndexOutOfRangeException>());
		Assert.That(() => list.WriteItemBytes(3, 1, new byte[] { 0xAA, 0xBB }.AsSpan()), Throws.TypeOf<IndexOutOfRangeException>());
		Assert.That(stream.ToArray(), Is.EqualTo(before));
		Assert.That(list.Read(2), Is.EqualTo(0x31323334));
		Assert.That(list.Read(3), Is.EqualTo(0x41424344));
	}

	[Test]
	public void StaticItemReadsAvoidPerReadPageAndPayloadAllocations() {
		using var stream = new MemoryStream();
		var list = new StreamPagedList<long>(PrimitiveSerializer<long>.Instance, stream, includeListHeader: false);
		list.AddRange(new[] { 7L, 0x0102030405060708L });
		Span<byte> destination = stackalloc byte[8];
		_ = list.ReadItemBytes(1, 0, destination);
		var before = GC.GetAllocatedBytesForCurrentThread();
		var totalRead = 0;
		for (var iteration = 0; iteration < 1000; iteration++)
			totalRead += list.ReadItemBytes(1, 0, destination);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(totalRead, Is.EqualTo(8000));
		Assert.That(destination.ToArray(), Is.EqualTo(EndianBitConverter.Little.GetBytes(0x0102030405060708L)));
		Assert.That(allocated, Is.LessThan(4096));
		TestContext.WriteLine($"1000 static raw item span reads allocated {allocated} bytes.");
	}

	[Test]
	public void StaticPageScopeCanBeRepeatedlyDisposedWithoutClosingOrChangingStorage() {
		using var stream = new MemoryStream();
		var list = new StreamPagedList<long>(PrimitiveSerializer<long>.Instance, stream, includeListHeader: false);
		list.AddRange(new[] { 7L, 8L });
		var scope = list.EnterOpenPageScope(list.Pages[0]);
		scope.Dispose();
		scope.Dispose();
		using (list.EnterOpenPageScope(list.Pages[0]))
			list.Update(1, 9L);
		Assert.That(stream.CanRead, Is.True);
		Assert.That(list.Read(1), Is.EqualTo(9L));
	}

	private sealed class TrackingStreamPagedList : StreamPagedList<int> {
		public TrackingStreamPagedList(StreamPagedListType type, Stream stream, Endianness endianness)
			: base(type, PrimitiveSerializer<int>.Instance, stream, 12, endianness) {
		}

		public int OpenScopes { get; private set; }

		public override PageScope<int> EnterOpenPageScope(IPage<int> page) {
			OpenScopes++;
			return base.EnterOpenPageScope(page);
		}
	}

	private sealed class ShortReadStream : MemoryStream {
		public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(2, count));
		public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(2, buffer.Length)]);
	}
}
