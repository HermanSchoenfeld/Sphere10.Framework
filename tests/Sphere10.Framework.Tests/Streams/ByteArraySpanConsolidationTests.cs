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
using Sphere10.Framework.Collections;
using WrapperKind = Sphere10.Framework.Tests.StreamWrapperSpanTests.WrapperKind;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ByteArraySpanConsolidationTests {

	[Test]
	public void WrapperArrayOperationsRetainTheOriginalArrayTransport([Values] WrapperKind kind, [Values(0, 2)] int count) {
		using var inner = new TransportProbeStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { Position = 1 };
		var criticalObject = new CriticalObject();
		using var wrapper = CreateWrapper(kind, inner, criticalObject);
		using var scope = criticalObject.EnterAccessScope();
		var destination = new byte[] { 255, 255, 255, 255, 255, 255 };
		Assert.That(wrapper.Read(destination, 2, count), Is.EqualTo(count));
		Assert.That(destination, Is.EqualTo(count == 0 ? new byte[] { 255, 255, 255, 255, 255, 255 } : new byte[] { 255, 255, 1, 2, 255, 255 }));
		var bounded = kind is WrapperKind.Bounded or WrapperKind.GenericBounded;
		Assert.That(inner.ArrayReads, Is.EqualTo(bounded && count == 0 ? 0 : 1));
		Assert.That(inner.SpanReads, Is.Zero);
		inner.Position = 1;
		wrapper.Write(new byte[] { 255, 8, 9, 255 }, 1, count);
		Assert.That(inner.ToArray(), Is.EqualTo(count == 0 ? new byte[] { 0, 1, 2, 3, 4, 5 } : new byte[] { 0, 8, 9, 3, 4, 5 }));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
		Assert.That(inner.SpanWrites, Is.Zero);
	}

	[Test]
	public void BoundedEmptyArrayReadsKeepTheOriginalArgumentAndPositionShortcut([Values] bool generic, [Values(0, 6)] int position) {
		using var inner = new TransportProbeStream(new byte[6]) { Position = position };
		using Stream wrapper = generic ? new BoundedStream<Stream>(inner, 1, 4) : new BoundedStream(inner, 1, 4);
		Assert.That(wrapper.Read(null, -1, 0), Is.Zero);
		Assert.That(inner.PositionReads, Is.Zero);
		Assert.That(inner.ArrayReads, Is.Zero);
		Assert.That(inner.SpanReads, Is.Zero);
	}

	[Test]
	public void BoundedArrayOperationsCheckBoundsBeforeInnerBufferValidation([Values] bool generic) {
		using var inner = new TransportProbeStream(new byte[6]);
		using Stream wrapper = generic ? new BoundedStream<Stream>(inner, 1, 4) : new BoundedStream(inner, 1, 4);
		Assert.That(() => wrapper.Read(null, -1, 1), Throws.TypeOf<StreamOutOfBoundsException>());
		Assert.That(() => wrapper.Write(null, -1, 1), Throws.TypeOf<StreamOutOfBoundsException>());
		Assert.That(() => wrapper.Read(new byte[1], 0, 1), Throws.TypeOf<StreamOutOfBoundsException>());
		Assert.That(() => wrapper.Write(new byte[1], 0, 1), Throws.TypeOf<StreamOutOfBoundsException>());
		Assert.That(inner.ArrayReads, Is.Zero);
		Assert.That(inner.ArrayWrites, Is.Zero);
		inner.Position = 1;
		Assert.That(() => wrapper.Read(null, 0, 1), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => wrapper.Write(null, 0, 1), Throws.TypeOf<ArgumentNullException>());
		Assert.That(inner.ArrayReads, Is.EqualTo(1));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
		Assert.That(inner.SpanReads, Is.Zero);
		Assert.That(inner.SpanWrites, Is.Zero);
	}

	[Test]
	public void BoundedArrayReadTruncatesBeforeValidatingTheInnerDestination([Values] bool generic) {
		using var inner = new TransportProbeStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { Position = 4 };
		using Stream wrapper = generic ? new BoundedStream<Stream>(inner, 1, 4) : new BoundedStream(inner, 1, 4);
		var destination = new byte[1];
		Assert.That(wrapper.Read(destination, 0, 4), Is.EqualTo(1));
		Assert.That(destination, Is.EqualTo(new byte[] { 4 }));
		Assert.That(inner.ArrayReads, Is.EqualTo(1));
		Assert.That(inner.SpanReads, Is.Zero);
	}

	[Test]
	public void ConcurrentArrayOperationsCheckTheParentLockBeforeInnerBufferValidation([Values(0, 1)] int count) {
		using var inner = new TransportProbeStream(new byte[6]);
		var parent = new CriticalObject();
		using var wrapper = new ConcurrentStream(inner) { ParentCriticalObject = parent };
		var buffer = new byte[1];
		Assert.That(() => wrapper.Read(null, 0, count), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => wrapper.Write(null, 0, count), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => wrapper.Read(buffer, 0, count), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => wrapper.Write(buffer, 0, count), Throws.TypeOf<InvalidOperationException>());
		Assert.That(inner.ArrayReads, Is.Zero);
		Assert.That(inner.ArrayWrites, Is.Zero);
		using (parent.EnterAccessScope()) {
			Assert.That(() => wrapper.Read(null, 0, count), Throws.TypeOf<ArgumentNullException>());
			Assert.That(() => wrapper.Write(null, 0, count), Throws.TypeOf<ArgumentNullException>());
			Assert.That(wrapper.Read(buffer, 0, count), Is.EqualTo(count));
			wrapper.Write(buffer, 0, count);
		}
		Assert.That(inner.ArrayReads, Is.EqualTo(2));
		Assert.That(inner.ArrayWrites, Is.EqualTo(2));
		Assert.That(inner.SpanReads, Is.Zero);
		Assert.That(inner.SpanWrites, Is.Zero);
	}

	[Test]
	public void BoundedReadRetainsOriginalClampNearTheLargestPosition([Values] bool array) {
		using var inner = new LargePositionStream { Position = long.MaxValue - 2 };
		using var wrapper = new BoundedStream(inner, long.MaxValue - 4, 4);
		var destination = new byte[4];
		Assert.That(array ? wrapper.Read(destination, 0, destination.Length) : wrapper.Read(destination.AsSpan()), Is.Zero);
		Assert.That(inner.RequestedCount, Is.EqualTo(2));
	}

	[Test]
	public void NonClosingArrayArgumentValidationBelongsToTheInnerStream([Values] bool generic) {
		using var inner = new TransportProbeStream(new byte[2]);
		using Stream wrapper = generic ? new NonClosingStream<Stream>(inner) : new NonClosingStream(inner);
		Assert.That(() => wrapper.Read(null, 0, 1), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => wrapper.Write(null, 0, 1), Throws.TypeOf<ArgumentNullException>());
		Assert.That(inner.ArrayReads, Is.EqualTo(1));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
		Assert.That(inner.SpanReads, Is.Zero);
		Assert.That(inner.SpanWrites, Is.Zero);
	}

	[Test]
	public void StreamSnapshotRetainsOneShortArrayReadWhileDestinationReadsFill([Values(0, 5)] int count, [Values] bool inherited) {
		using var stream = new TransportProbeStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { MaximumRead = 2 };
		var buffer = inherited ? new InheritedStreamMappedBuffer(stream) : new StreamMappedBuffer(stream);
		var snapshot = buffer.ReadArray(1, count);
		Assert.That(snapshot, Is.EqualTo(count == 0 ? Array.Empty<byte>() : new byte[] { 1, 2 }));
		Assert.That(stream.ArrayReads, Is.EqualTo(1));
		Assert.That(stream.SpanReads, Is.Zero);
		stream.ResetReadCounts();
		var destination = new byte[] { 255, 255, 255, 255, 255, 255, 255 };
		buffer.ReadRange(1, destination.AsSpan(1, count));
		Assert.That(destination, Is.EqualTo(count == 0 ? new byte[] { 255, 255, 255, 255, 255, 255, 255 } : new byte[] { 255, 1, 2, 3, 4, 5, 255 }));
		Assert.That(stream.ArrayReads, Is.Zero);
		Assert.That(stream.SpanReads, Is.EqualTo((count + 1) / 2));
	}

	[Test]
	public void StreamDestinationReadRejectsTruncatedStorageWithoutClearingTheTail([Values] bool inherited) {
		using var stream = new TransportProbeStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { MaximumRead = 2, ReadableLength = 3 };
		var buffer = inherited ? new InheritedStreamMappedBuffer(stream) : new StreamMappedBuffer(stream);
		Assert.That(buffer.ReadArray(1, 4), Is.EqualTo(new byte[] { 1, 2 }));
		var destination = new byte[] { 255, 255, 255, 255, 255, 255 };
		Assert.That(() => buffer.ReadRange(1, destination.AsSpan(1, 4)), Throws.TypeOf<EndOfStreamException>());
		Assert.That(destination, Is.EqualTo(new byte[] { 255, 1, 2, 255, 255, 255 }));
	}

	[Test]
	public void EndianArrayReadsUseTheSpanFillOperationUntilEof([Values] bool snapshot, [Values(0, 6)] int count) {
		using var stream = new TransportProbeStream(new byte[] { 1, 2, 3, 4 }) { MaximumRead = 2 };
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		var destination = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
		if (snapshot) {
			Assert.That(reader.ReadBytes(count), Is.EqualTo(count == 0 ? Array.Empty<byte>() : new byte[] { 1, 2, 3, 4 }));
		} else {
			Assert.That(reader.Read(destination, 1, count), Is.EqualTo(Math.Min(count, 4)));
			Assert.That(destination, Is.EqualTo(count == 0 ? new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 } : new byte[] { 255, 1, 2, 3, 4, 255, 255, 255 }));
		}
		Assert.That(stream.ArrayReads, Is.Zero);
		Assert.That(stream.SpanReads, Is.EqualTo(count == 0 ? 0 : 3));
	}

	[Test]
	public void EndianPublicSpanReadRemainsSingleReadAndStrictReadsFillWithSpans() {
		using var stream = new TransportProbeStream(new byte[] { 1, 2, 3, 4 }) { MaximumRead = 2 };
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		var destination = new byte[] { 255, 255, 255, 255, 255 };
		Assert.That(reader.Read(destination.AsSpan()), Is.EqualTo(2));
		Assert.That(destination, Is.EqualTo(new byte[] { 1, 2, 255, 255, 255 }));
		Assert.That(stream.SpanReads, Is.EqualTo(1));
		stream.Position = 0;
		stream.ResetReadCounts();
		Assert.That(reader.ReadBytesOrThrow(4), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
		Assert.That(stream.SpanReads, Is.EqualTo(2));
		Assert.That(stream.ArrayReads, Is.Zero);
		Assert.That(() => reader.ReadBytesOrThrow(1), Throws.TypeOf<EndOfStreamException>());
	}

	[Test]
	public void EndianArrayValidationAndDisposalOrderArePreserved() {
		using var stream = new TransportProbeStream(new byte[4]);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(() => reader.Read((byte[])null, 0, 0), Throws.TypeOf<ArgumentNullException>());
		Assert.That(() => reader.Read(new byte[1], -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("index"));
		Assert.That(() => reader.Read(new byte[1], 0, -1), Throws.TypeOf<ArgumentOutOfRangeException>().With.Property("ParamName").EqualTo("index"));
		Assert.That(() => reader.Read(new byte[1], 1, 1), Throws.TypeOf<ArgumentException>());
		Assert.That(() => reader.Read(new byte[1], 1, int.MaxValue), Throws.TypeOf<ArgumentException>());
		Assert.That(stream.ArrayReads, Is.Zero);
		Assert.That(stream.SpanReads, Is.Zero);
		reader.Dispose();
		Assert.That(() => reader.Read((byte[])null, -1, -1), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => reader.ReadBytes(-1), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public void EndianArrayWritesDelegateToTheSpanOperation() {
		using var stream = new TransportProbeStream(new byte[] { 0, 1, 2, 3, 4, 5 });
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, stream);
		var source = new byte[] { 255, 8, 9, 255 };
		writer.Write(source, 1, 2);
		writer.Write(source.AsSpan(1, 2));
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 8, 9, 8, 9, 4, 5 }));
		Assert.That(stream.ArrayWrites, Is.Zero);
		Assert.That(stream.SpanWrites, Is.EqualTo(2));
		writer.Dispose();
		Assert.That(() => writer.Write(null, -1, -1), Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public void RawItemReadsPreserveLegacySnapshotsAndFillDestinationSpans([Values] StreamPagedListType type, [Values] bool snapshot) {
		using var stream = new TransportProbeStream { MaximumRead = 2 };
		var list = new StreamPagedList<int>(type, PrimitiveSerializer<int>.Instance, stream, type == StreamPagedListType.Dynamic ? 2 : 16);
		list.AddRange(new[] { 1, 2, 3, 0x41424344 });
		stream.ResetReadCounts();
		var destination = new byte[4];
		if (type == StreamPagedListType.Dynamic && snapshot) {
			Assert.That(() => list.ReadItemBytes(3, 0, null, out destination), Throws.TypeOf<IndexOutOfRangeException>());
			Assert.That(stream.ArrayReads, Is.Zero);
			Assert.That(stream.SpanReads, Is.Zero);
			return;
		}
		if (snapshot) {
			Assert.That(list.ReadItemBytes(3, 0, null, out destination), Is.EqualTo(4));
		} else {
			Assert.That(list.ReadItemBytes(3, 0, destination.AsSpan()), Is.EqualTo(4));
		}
		Assert.That(destination, Is.EqualTo(EndianBitConverter.Little.GetBytes(0x41424344)));
		Assert.That(stream.ArrayReads, Is.Zero);
		Assert.That(stream.SpanReads, Is.EqualTo(2));
	}

	private static Stream CreateWrapper(WrapperKind kind, Stream inner, ICriticalObject criticalObject) => kind switch {
		WrapperKind.Bounded => new BoundedStream(inner, 1, 4),
		WrapperKind.GenericBounded => new BoundedStream<Stream>(inner, 1, 4),
		WrapperKind.NonClosing => new NonClosingStream(inner),
		WrapperKind.GenericNonClosing => new NonClosingStream<Stream>(inner),
		WrapperKind.Concurrent => new ConcurrentStream(inner, criticalObject),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	private sealed class InheritedStreamMappedBuffer : StreamMappedBuffer {
		public InheritedStreamMappedBuffer(Stream stream)
			: base(stream) {
		}
	}

	private sealed class LargePositionStream : MemoryStream {
		public override long Position { get; set; }
		public int RequestedCount { get; private set; }

		public override int Read(byte[] buffer, int offset, int count) {
			RequestedCount = count;
			return 0;
		}

		public override int Read(Span<byte> buffer) {
			RequestedCount = buffer.Length;
			return 0;
		}
	}

	private sealed class TransportProbeStream : Stream {
		private readonly MemoryStream _stream;

		public TransportProbeStream(byte[] bytes = null) {
			_stream = bytes is null ? new MemoryStream() : new MemoryStream(bytes);
		}

		public int MaximumRead { get; set; } = int.MaxValue;
		public long ReadableLength { get; set; } = long.MaxValue;
		public int ArrayReads { get; private set; }
		public int SpanReads { get; private set; }
		public int ArrayWrites { get; private set; }
		public int SpanWrites { get; private set; }
		public int PositionReads { get; private set; }
		public override bool CanRead => _stream.CanRead;
		public override bool CanSeek => _stream.CanSeek;
		public override bool CanWrite => _stream.CanWrite;
		public override long Length => _stream.Length;
		public override long Position {
			get {
				PositionReads++;
				return _stream.Position;
			}
			set => _stream.Position = value;
		}

		public byte[] ToArray() => _stream.ToArray();
		public override void Flush() => _stream.Flush();
		public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
		public override void SetLength(long value) => _stream.SetLength(value);

		public void ResetReadCounts() {
			ArrayReads = 0;
			SpanReads = 0;
		}

		public override int Read(byte[] buffer, int offset, int count) {
			ArrayReads++;
			return _stream.Read(buffer, offset, LimitRead(count));
		}

		public override int Read(Span<byte> buffer) {
			SpanReads++;
			return _stream.Read(buffer[..LimitRead(buffer.Length)]);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			ArrayWrites++;
			_stream.Write(buffer, offset, count);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			SpanWrites++;
			_stream.Write(buffer);
		}

		protected override void Dispose(bool disposing) {
			if (disposing)
				_stream.Dispose();
			base.Dispose(disposing);
		}

		private int LimitRead(int count)
			=> (int)Math.Min(count, Math.Min(MaximumRead, Math.Max(0, ReadableLength - _stream.Position)));
	}
}
