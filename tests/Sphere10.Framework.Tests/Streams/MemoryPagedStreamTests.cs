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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.NUnit;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemoryPagedStreamTests {

	[Test]
	public void RandomAccessOperationsMatchFileStreamAcrossPageEvictions() {
		var filename = Tools.FileSystem.GenerateTempFilename();
		using var delete = Tools.Scope.ExecuteOnDispose(() => File.Delete(filename));
		using var expected = new FileStream(filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 32, maxMemory: 64);
		var pages = (MemoryPagedBuffer)stream.Buffer;
		AssertEx.StreamIntegrationTests(1024, stream, expected, iterations: 100,
			extraTest: () => Assert.That(pages.CurrentOpenPages, Is.LessThanOrEqualTo(2)));
	}

	[Test]
	public void SmallRandomWritesSaveOncePerDirtyPageAndReadsSeePendingChanges() {
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 64, maxMemory: 4 * 64);
		var pages = (MemoryPagedBuffer)stream.Buffer;
		var expected = new byte[256];
		stream.SetLength(expected.Length);
		var random = new Random(31337);
		for (var index = 0; index < 1024; index++) {
			var position = random.Next(expected.Length);
			stream.Position = position;
			stream.WriteByte(expected[position] = (byte)(index + 1));
		}
		stream.Position = 0;
		var actual = new byte[expected.Length];
		stream.ReadExactly(actual);
		Assert.That(actual, Is.EqualTo(expected));
		Assert.That(source.WriteCalls, Is.Zero, "Reads must see dirty pages without forcing writes.");
		Assert.That(source.LengthCalls, Is.Zero, "Growth must stay in the page cache until a save.");
		Assert.That(pages.CurrentOpenPages, Is.EqualTo(4));
		stream.Flush();
		Assert.That(source.WriteCalls, Is.EqualTo(4), "1024 random updates require only one write per dirty page.");
		Assert.That(source.Bytes, Is.EqualTo(expected));
		Assert.That(pages.CurrentOpenPages, Is.EqualTo(4), "Flushing the stream must keep its pages cached.");
		stream.Flush();
		Assert.That(source.WriteCalls, Is.EqualTo(4), "Clean pages must not be written again.");
	}

	[Test]
	public void EvictionSavesDirtyPagesWithoutExceedingTheCacheLimit() {
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 8, maxMemory: 8);
		var pages = (MemoryPagedBuffer)stream.Buffer;
		var expected = Enumerable.Range(1, 25).Select(value => (byte)value).ToArray();
		foreach (var value in expected) {
			stream.WriteByte(value);
			Assert.That(pages.CurrentOpenPages, Is.LessThanOrEqualTo(1));
		}
		Assert.That(source.WriteCalls, Is.EqualTo(3), "Only evicted pages need to reach the source before Flush.");
		stream.Position = 0;
		var actual = new byte[expected.Length];
		stream.ReadExactly(actual);
		Assert.That(actual, Is.EqualTo(expected));
		stream.Flush();
		Assert.That(source.Bytes, Is.EqualTo(expected));
	}

	[Test]
	public void ExistingContentOpensInPlaceAndOnlyChangedPagesAreWritten() {
		var original = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray();
		using var source = new ObservedStream(original);
		using var stream = new MemoryPagedStream(source, pageSize: 16, maxMemory: 32);
		Assert.That(stream.Length, Is.EqualTo(original.Length));
		var actual = new byte[original.Length];
		stream.ReadExactly(actual);
		Assert.That(actual, Is.EqualTo(original));
		stream.Position = 50;
		stream.WriteByte(255);
		stream.Flush();
		original[50] = 255;
		Assert.That(source.Bytes, Is.EqualTo(original));
		Assert.That(source.WriteCalls, Is.EqualTo(1));
		Assert.That(source.LengthCalls, Is.Zero);
	}

	[Test]
	public void ShrinkingThenRegrowingDoesNotResurrectDirtyOrPersistedBytes([Values] bool flushBeforeShrink) {
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 8, maxMemory: 3 * 8);
		var expected = Enumerable.Range(1, 24).Select(value => (byte)value).ToArray();
		stream.Write(expected);
		if (flushBeforeShrink)
			stream.Flush();
		stream.SetLength(11);
		Assert.That(stream.Length, Is.EqualTo(11));
		Assert.That(source.Length, Is.EqualTo(flushBeforeShrink ? 24 : 0), "A shrink reaches the source with the next save.");
		stream.SetLength(24);
		Array.Clear(expected, 11, 13);
		stream.Position = 0;
		var actual = new byte[24];
		stream.ReadExactly(actual);
		Assert.That(actual, Is.EqualTo(expected));
		stream.Flush();
		Assert.That(source.Bytes, Is.EqualTo(expected));
	}

	[Test]
	public void LengthChangesReachTheSourceOnlyWhenSaved() {
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 4096, maxMemory: 4096);
		for (var length = 1; length <= 1024; length++) {
			stream.SetLength(length);
			stream.Position = length - 1;
			stream.WriteByte(7);
		}
		Assert.That(source.LengthCalls, Is.Zero);
		Assert.That(source.Length, Is.Zero);
		stream.Flush();
		Assert.That(source.Bytes, Is.EqualTo(Enumerable.Repeat((byte)7, 1024).ToArray()));
		Assert.That(source.LengthCalls, Is.Zero, "The saved page grows the source by writing it.");
		stream.SetLength(4);
		stream.SetLength(8);
		Assert.That(source.Length, Is.EqualTo(1024));
		stream.Flush();
		Assert.That(source.Bytes, Is.EqualTo(new byte[] { 7, 7, 7, 7, 0, 0, 0, 0 }));
		Assert.That(source.LengthCalls, Is.EqualTo(1), "Only the net shrink resizes the source.");
	}

	[Test]
	public void PositionsAboveTwoGiBAddressDistinctPages() {
		const long length = (1L << 31) + 64;
		const long offset = (1L << 31) + 15;
		using var source = new SparseStream();
		source.SetLength(length);
		using var stream = new MemoryPagedStream(source, pageSize: 1 << 20, maxMemory: 2 << 20);
		Assert.That(stream.Length, Is.EqualTo(length));
		stream.Position = offset;
		stream.Write(new byte[] { 7, 8, 9 });
		Assert.That(stream.Seek(offset - 1, SeekOrigin.Begin), Is.EqualTo(offset - 1));
		var actual = new byte[4];
		stream.ReadExactly(actual);
		Assert.That(actual, Is.EqualTo(new byte[] { 0, 7, 8, 9 }));
		stream.Position = 15;
		stream.WriteByte(22);
		stream.Flush();
		Assert.That(source.Stored, Is.EquivalentTo(new Dictionary<long, byte> { [15] = 22, [offset] = 7, [offset + 1] = 8, [offset + 2] = 9 }));
		Assert.That(source.Length, Is.EqualTo(length));
	}

	[Test]
	public void FailedSaveRetainsPendingBytesForRetry() {
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 8, maxMemory: 2 * 8);
		var expected = Enumerable.Range(1, 12).Select(value => (byte)value).ToArray();
		stream.Write(expected);
		source.FailWrites = true;
		Assert.That(stream.Flush, Throws.TypeOf<IOException>());
		stream.Position = 0;
		var actual = new byte[12];
		stream.ReadExactly(actual);
		Assert.That(actual, Is.EqualTo(expected));
		source.FailWrites = false;
		stream.Flush();
		Assert.That(source.Bytes, Is.EqualTo(expected));
		Assert.That(source.WriteCalls, Is.EqualTo(3), "One failed write plus two dirty pages on retry.");
	}

	[Test]
	public void FailedEvictionRetainsItsDirtyVictimUntilRetry() {
		using var source = new ObservedStream();
		using var stream = new MemoryPagedStream(source, pageSize: 8, maxMemory: 8);
		stream.Write(Enumerable.Repeat((byte)42, 8).ToArray());
		source.FailWrites = true;
		Assert.That(() => stream.WriteByte(99), Throws.TypeOf<IOException>());
		stream.Position = 0;
		Assert.That(stream.ReadByte(), Is.EqualTo(42), "Failed eviction must retain the only up-to-date copy.");
		source.FailWrites = false;
		stream.Position = 8;
		stream.WriteByte(99);
		stream.Flush();
		Assert.That(source.Bytes, Is.EqualTo(Enumerable.Repeat((byte)42, 8).Append((byte)99).ToArray()));
	}

	[Test]
	public void DisposalHonorsOwnershipEvenWhenSavingFails([Values] bool leaveOpen, [Values] bool failSave) {
		using var source = new ObservedStream();
		var stream = new MemoryPagedStream(source, pageSize: 8, maxMemory: 8, leaveOpen: leaveOpen);
		stream.WriteByte(42);
		source.FailWrites = failSave;
		if (failSave)
			Assert.That(stream.Dispose, Throws.TypeOf<IOException>());
		else
			Assert.That(stream.Dispose, Throws.Nothing);
		Assert.That(source.DisposeCalls, Is.EqualTo(leaveOpen ? 0 : 1));
		Assert.That(stream.CanRead, Is.False);
		Assert.That(stream.CanWrite, Is.False);
		Assert.That(() => stream.ReadByte(), Throws.TypeOf<ObjectDisposedException>());
		stream.Dispose();
		Assert.That(source.DisposeCalls, Is.EqualTo(leaveOpen ? 0 : 1));
		Assert.That(source.WriteCalls, Is.EqualTo(1), "Disposal must not implicitly retry a failed save.");
	}

	[Test]
	public async Task StandardAsyncOperationsObserveTheSameCachedData([Values] bool memoryOverloads) {
		using var source = new ObservedStream();
		await using var stream = new MemoryPagedStream(source, pageSize: 8, maxMemory: 2 * 8);
		var expected = Enumerable.Range(1, 12).Select(value => (byte)value).ToArray();
		if (memoryOverloads)
			await stream.WriteAsync(expected.AsMemory());
		else
			await stream.WriteAsync(expected, 0, expected.Length, CancellationToken.None);
		stream.Position = 0;
		var actual = new byte[expected.Length];
		var count = memoryOverloads ? await stream.ReadAsync(actual.AsMemory()) : await stream.ReadAsync(actual, 0, actual.Length, CancellationToken.None);
		Assert.That(count, Is.EqualTo(expected.Length));
		Assert.That(actual, Is.EqualTo(expected));
		stream.Position = 0;
		using var copy = new MemoryStream();
		await stream.CopyToAsync(copy, 5, CancellationToken.None);
		Assert.That(copy.ToArray(), Is.EqualTo(expected));
		Assert.That(source.WriteCalls, Is.Zero);
		await stream.FlushAsync();
		Assert.That(source.Bytes, Is.EqualTo(expected));
		Assert.That(source.WriteCalls, Is.EqualTo(2));
	}

	#region Inner Types

	private sealed class ObservedStream : StreamDecorator<MemoryStream> {
		public ObservedStream() : base(new MemoryStream()) { }
		public ObservedStream(byte[] content) : base(new MemoryStream()) => InnerStream.Write(content);
		public bool FailWrites { get; set; }
		public int LengthCalls { get; private set; }
		public int WriteCalls { get; private set; }
		public int DisposeCalls { get; private set; }
		public byte[] Bytes => InnerStream.ToArray();
		public override void SetLength(long value) {
			LengthCalls++;
			InnerStream.SetLength(value);
		}
		public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
		public override void Write(ReadOnlySpan<byte> buffer) {
			WriteCalls++;
			if (FailWrites) {
				InnerStream.Write(buffer[..Math.Min(2, buffer.Length)]);
				throw new IOException("Injected write failure");
			}
			InnerStream.Write(buffer);
		}
		public override void Close() => Dispose(true);
		protected override void Dispose(bool disposing) {
			if (!disposing || DisposeCalls != 0)
				return;
			DisposeCalls++;
			base.Dispose(disposing);
		}
	}

	private sealed class SparseStream : Stream {
		public Dictionary<long, byte> Stored { get; } = new();
		private long _length;
		public override bool CanRead => true;
		public override bool CanWrite => true;
		public override bool CanSeek => true;
		public override long Length => _length;
		public override long Position { get; set; }
		public override void Flush() { }
		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));
		public override int Read(Span<byte> buffer) {
			var count = (int)Math.Min(buffer.Length, Math.Max(0, Length - Position));
			for (var index = 0; index < count; index++)
				buffer[index] = Stored.GetValueOrDefault(Position++);
			return count;
		}
		public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
		public override void Write(ReadOnlySpan<byte> buffer) {
			foreach (var value in buffer) {
				if (value == 0)
					Stored.Remove(Position);
				else
					Stored[Position] = value;
				Position++;
			}
			_length = Math.Max(_length, Position);
		}
		public override long Seek(long offset, SeekOrigin origin) => Position = origin switch {
			SeekOrigin.Begin => offset, SeekOrigin.Current => Position + offset, SeekOrigin.End => Length + offset,
			_ => throw new ArgumentOutOfRangeException(nameof(origin))
		};
		public override void SetLength(long value) {
			foreach (var key in Stored.Keys.Where(key => key >= value).ToArray())
				Stored.Remove(key);
			_length = value;
		}
	}

	#endregion
}
