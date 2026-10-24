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

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamWrapperSpanTests {

	[Test]
	public void WrapperChainSpanCallsUseTheExistingArrayTransport([Values] bool genericBounded, [Values] bool genericNonClosing) {
		using var inner = new ArrayTransportStream(new byte[] { 0, 1, 2, 3, 4, 5 });
		var criticalObject = new CriticalObject();
		var concurrent = new ConcurrentStream(inner, criticalObject);
		Stream bounded = genericBounded
			? new BoundedStream<ConcurrentStream>(concurrent, 1, 4) { UseRelativeAddressing = true }
			: new BoundedStream(concurrent, 1, 4) { UseRelativeAddressing = true };
		using Stream wrapper = genericNonClosing ? new NonClosingStream<Stream>(bounded) : new NonClosingStream(bounded);
		using var scope = criticalObject.EnterAccessScope();
		wrapper.Position = 0;
		wrapper.Write(new byte[] { 7, 8, 9 }.AsSpan());
		wrapper.Position = 0;
		Span<byte> destination = stackalloc byte[6];
		destination.Fill(0xCC);
		Assert.That(wrapper.Read(destination), Is.EqualTo(4));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 7, 8, 9, 4, 0xCC, 0xCC }));
		Assert.That(wrapper.Position, Is.EqualTo(4));
		Assert.That(inner.ArrayReads, Is.EqualTo(1));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
	}

	[Test]
	public void SpanReadsPreserveShortReadCountsAndDestinationTail([Values] WrapperKind kind) {
		using var inner = new ArrayTransportStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { MaximumRead = 2, Position = 1 };
		var criticalObject = new CriticalObject();
		using var wrapper = CreateWrapper(kind, inner, criticalObject);
		using var scope = criticalObject.EnterAccessScope();
		Span<byte> destination = stackalloc byte[4];
		destination.Fill(0xCC);
		Assert.That(wrapper.Read(destination), Is.EqualTo(2));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 0xCC, 0xCC }));
		Assert.That(inner.Position, Is.EqualTo(3));
		Assert.That(inner.ArrayReads, Is.EqualTo(1));
	}

	[Test]
	public void SpanWritesPreserveBytesOutsideTheSource([Values] WrapperKind kind) {
		using var inner = new ArrayTransportStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { Position = 1 };
		var criticalObject = new CriticalObject();
		using var wrapper = CreateWrapper(kind, inner, criticalObject);
		using var scope = criticalObject.EnterAccessScope();
		wrapper.Write(new byte[] { 8, 9 }.AsSpan());
		Assert.That(inner.ToArray(), Is.EqualTo(new byte[] { 0, 8, 9, 3, 4, 5 }));
		Assert.That(inner.Position, Is.EqualTo(3));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
	}

	[Test]
	public void BoundedReadsTruncateAtBoundary([Values] bool generic, [Values] bool relative, [Values(0, 1, 4, 5, 6)] int position) {
		using var inner = new ArrayTransportStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { Position = position };
		using var wrapper = CreateBounded(inner, generic, relative);
		var destination = new byte[] { 0xCC, 0xCC, 0xCC };
		if (position < 1 || position > 5) {
			Assert.That(() => wrapper.Read(destination.AsSpan()), Throws.TypeOf<StreamOutOfBoundsException>());
			Assert.That(inner.ArrayReads, Is.Zero);
		} else {
			var expectedCount = Math.Min(destination.Length, 5 - position);
			Assert.That(wrapper.Read(destination.AsSpan()), Is.EqualTo(expectedCount));
			for (var i = 0; i < destination.Length; i++)
				Assert.That(destination[i], Is.EqualTo(i < expectedCount ? position + i : 0xCC));
			Assert.That(inner.ArrayReads, Is.EqualTo(1));
			Assert.That(inner.Position, Is.EqualTo(position + expectedCount));
		}
	}

	[Test]
	public void BoundedEmptyReadsDoNotAccessTheInnerStream([Values] bool generic, [Values(0, 1, 5, 6)] int position) {
		using var inner = new ArrayTransportStream(new byte[6]) { Position = position };
		using var wrapper = CreateBounded(inner, generic, false);
		Assert.That(wrapper.Read(Span<byte>.Empty), Is.Zero);
		Assert.That(inner.PositionReads, Is.Zero);
		Assert.That(inner.ArrayReads, Is.Zero);
	}

	[Test]
	public void BoundedWritesValidateTheEntireRangeIncludingEmptyWrites([Values] bool generic, [Values] bool relative, [Values(0, 1, 4, 5, 6)] int position, [Values(0, 2)] int count) {
		using var inner = new ArrayTransportStream(new byte[6]) { Position = position };
		using var wrapper = CreateBounded(inner, generic, relative);
		var source = new byte[] { 7, 8 };
		if (position < 1 || position > 5 || position + count > 5) {
			Assert.That(() => wrapper.Write(source.AsSpan(0, count)), Throws.TypeOf<StreamOutOfBoundsException>());
			Assert.That(inner.ArrayWrites, Is.Zero);
			Assert.That(inner.Position, Is.EqualTo(position));
			Assert.That(inner.ToArray(), Is.All.Zero);
		} else {
			wrapper.Write(source.AsSpan(0, count));
			Assert.That(inner.ArrayWrites, Is.EqualTo(1));
			Assert.That(inner.Position, Is.EqualTo(position + count));
			for (var i = 0; i < inner.Length; i++)
				Assert.That(inner.ToArray()[i], Is.EqualTo(i >= position && i < position + count ? source[i - position] : 0));
		}
	}

	[Test]
	public void NonClosingSpanOperationsRetainInnerOwnership([Values] bool generic) {
		using var inner = new ArrayTransportStream(new byte[] { 1, 2 });
		Stream wrapper = generic ? new NonClosingStream<ArrayTransportStream>(inner) : new NonClosingStream(inner);
		wrapper.Write(new byte[] { 3 }.AsSpan());
		wrapper.Close();
		wrapper.Dispose();
		Assert.That(inner.CanRead, Is.True);
		inner.Position = 0;
		var destination = new byte[2];
		Assert.That(inner.Read(destination, 0, destination.Length), Is.EqualTo(2));
		Assert.That(destination, Is.EqualTo(new byte[] { 3, 2 }));
	}

	[Test]
	public void ConcurrentSpanOperationsRequireTheParentLockEvenWhenEmpty([Values(0, 1)] int count) {
		using var inner = new ArrayTransportStream(new byte[] { 1, 2 });
		var parent = new CriticalObject();
		using var wrapper = new ConcurrentStream(inner) { ParentCriticalObject = parent };
		var bytes = new byte[1];
		Assert.That(() => wrapper.Read(bytes.AsSpan(0, count)), Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => wrapper.Write(bytes.AsSpan(0, count)), Throws.TypeOf<InvalidOperationException>());
		Assert.That(inner.ArrayReads, Is.Zero);
		Assert.That(inner.ArrayWrites, Is.Zero);
		using (parent.EnterAccessScope()) {
			Assert.That(wrapper.IsLocked, Is.True);
			Assert.That(wrapper.Read(bytes.AsSpan(0, count)), Is.EqualTo(count));
			wrapper.Write(bytes.AsSpan(0, count));
		}
		Assert.That(inner.ArrayReads, Is.EqualTo(1));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
		Assert.That(wrapper.IsLocked, Is.False);
		Assert.That(() => wrapper.Read(bytes.AsSpan(0, count)), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void DerivedWrapperArrayOperationsDoNotInvokeSpanOverrides([Values] WrapperKind kind, [Values(0, 2)] int count) {
		using var inner = new MemoryStream(new byte[] { 1, 2, 3, 4 });
		var criticalObject = new CriticalObject();
		var calls = new StreamCalls();
		using var wrapper = CreateDerivedWrapper(kind, inner, criticalObject, calls);
		using var scope = criticalObject.EnterAccessScope();
		var bytes = new byte[count];
		Assert.That(wrapper.Read(bytes, 0, count), Is.EqualTo(count));
		wrapper.Position = 0;
		wrapper.Write(bytes, 0, count);
		Assert.That(calls.Reads, Is.EqualTo(1));
		Assert.That(calls.Writes, Is.EqualTo(1));
		Assert.That(calls.SpanReads, Is.Zero);
		Assert.That(calls.SpanWrites, Is.Zero);
	}

	[Test]
	public void DerivedWrapperSpanFallbackInvokesArrayOverrides([Values] WrapperKind kind, [Values(0, 2)] int count) {
		using var inner = new MemoryStream(new byte[] { 1, 2, 3, 4 });
		var criticalObject = new CriticalObject();
		var calls = new StreamCalls();
		using var wrapper = CreateDerivedWrapper(kind, inner, criticalObject, calls);
		using var scope = criticalObject.EnterAccessScope();
		var bytes = new byte[count];
		Assert.That(wrapper.Read(bytes.AsSpan()), Is.EqualTo(count));
		wrapper.Position = 0;
		wrapper.Write(bytes.AsSpan());
		Assert.That(calls.SpanReads, Is.EqualTo(1));
		Assert.That(calls.SpanWrites, Is.EqualTo(1));
		Assert.That(calls.Reads, Is.EqualTo(1));
		Assert.That(calls.Writes, Is.EqualTo(1));
	}

	[Test]
	public void DerivedWrappersInheritArrayTransportForSpanCalls([Values] WrapperKind kind, [Values(0, 2)] int count) {
		using var inner = new ArrayTransportStream(new byte[] { 0, 1, 2, 3, 4, 5 }) { Position = 1 };
		var criticalObject = new CriticalObject();
		using var wrapper = CreateInheritedWrapper(kind, inner, criticalObject);
		using var scope = criticalObject.EnterAccessScope();
		var destination = new byte[] { 0xCC, 0xCC };
		Assert.That(wrapper.Read(destination.AsSpan(0, count)), Is.EqualTo(count));
		Assert.That(destination, Is.EqualTo(count == 0 ? new byte[] { 0xCC, 0xCC } : new byte[] { 1, 2 }));
		wrapper.Position = 1;
		wrapper.Write(new byte[] { 8, 9 }.AsSpan(0, count));
		var bounded = kind is WrapperKind.Bounded or WrapperKind.GenericBounded;
		Assert.That(inner.ArrayReads, Is.EqualTo(bounded && count == 0 ? 0 : 1));
		Assert.That(inner.ArrayWrites, Is.EqualTo(1));
		Assert.That(inner.ToArray(), Is.EqualTo(count == 0 ? new byte[] { 0, 1, 2, 3, 4, 5 } : new byte[] { 0, 8, 9, 3, 4, 5 }));
	}

	private static Stream CreateWrapper(WrapperKind kind, ArrayTransportStream inner, ICriticalObject criticalObject) => kind switch {
		WrapperKind.Bounded => new BoundedStream(inner, 1, 4),
		WrapperKind.GenericBounded => new BoundedStream<ArrayTransportStream>(inner, 1, 4),
		WrapperKind.NonClosing => new NonClosingStream(inner),
		WrapperKind.GenericNonClosing => new NonClosingStream<ArrayTransportStream>(inner),
		WrapperKind.Concurrent => new ConcurrentStream(inner, criticalObject),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	private static Stream CreateBounded(ArrayTransportStream inner, bool generic, bool relative) => generic
		? new BoundedStream<ArrayTransportStream>(inner, 1, 4) { UseRelativeAddressing = relative }
		: new BoundedStream(inner, 1, 4) { UseRelativeAddressing = relative };

	private static Stream CreateDerivedWrapper(WrapperKind kind, MemoryStream inner, ICriticalObject criticalObject, StreamCalls calls) => kind switch {
		WrapperKind.Bounded => new InterceptingBoundedStream(inner, calls),
		WrapperKind.GenericBounded => new InterceptingGenericBoundedStream(inner, calls),
		WrapperKind.NonClosing => new InterceptingNonClosingStream(inner, calls),
		WrapperKind.GenericNonClosing => new InterceptingGenericNonClosingStream(inner, calls),
		WrapperKind.Concurrent => new InterceptingConcurrentStream(inner, criticalObject, calls),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	private static Stream CreateInheritedWrapper(WrapperKind kind, ArrayTransportStream inner, ICriticalObject criticalObject) => kind switch {
		WrapperKind.Bounded => new InheritedBoundedStream(inner),
		WrapperKind.GenericBounded => new InheritedGenericBoundedStream(inner),
		WrapperKind.NonClosing => new InheritedNonClosingStream(inner),
		WrapperKind.GenericNonClosing => new InheritedGenericNonClosingStream(inner),
		WrapperKind.Concurrent => new InheritedConcurrentStream(inner, criticalObject),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};

	public enum WrapperKind {
		Bounded,
		GenericBounded,
		NonClosing,
		GenericNonClosing,
		Concurrent
	}

	private sealed class StreamCalls {
		public int Reads;
		public int Writes;
		public int SpanReads;
		public int SpanWrites;
	}

	private sealed class ArrayTransportStream : Stream {
		private readonly MemoryStream _stream;

		public ArrayTransportStream(byte[] bytes) {
			_stream = new MemoryStream(bytes);
		}

		public int MaximumRead { get; set; } = int.MaxValue;
		public int ArrayReads { get; private set; }
		public int ArrayWrites { get; private set; }
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
		public override int Read(byte[] buffer, int offset, int count) {
			ArrayReads++;
			return _stream.Read(buffer, offset, Math.Min(count, MaximumRead));
		}

		public override void Write(byte[] buffer, int offset, int count) {
			ArrayWrites++;
			_stream.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) => throw new InvalidOperationException("Wrapper span fallbacks must preserve array transport.");

		public override void Write(ReadOnlySpan<byte> buffer) => throw new InvalidOperationException("Wrapper span fallbacks must preserve array transport.");

		protected override void Dispose(bool disposing) {
			if (disposing)
				_stream.Dispose();
			base.Dispose(disposing);
		}
	}

	private sealed class InterceptingBoundedStream(MemoryStream inner, StreamCalls calls) : BoundedStream(inner, 0, inner.Length) {
		public override int Read(byte[] buffer, int offset, int count) {
			calls.Reads++;
			return base.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			calls.Writes++;
			base.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			calls.SpanReads++;
			return base.Read(buffer);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			calls.SpanWrites++;
			base.Write(buffer);
		}
	}

	private sealed class InterceptingGenericBoundedStream(MemoryStream inner, StreamCalls calls) : BoundedStream<MemoryStream>(inner, 0, inner.Length) {
		public override int Read(byte[] buffer, int offset, int count) {
			calls.Reads++;
			return base.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			calls.Writes++;
			base.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			calls.SpanReads++;
			return base.Read(buffer);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			calls.SpanWrites++;
			base.Write(buffer);
		}
	}

	private sealed class InterceptingNonClosingStream(MemoryStream inner, StreamCalls calls) : NonClosingStream(inner) {
		public override int Read(byte[] buffer, int offset, int count) {
			calls.Reads++;
			return base.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			calls.Writes++;
			base.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			calls.SpanReads++;
			return base.Read(buffer);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			calls.SpanWrites++;
			base.Write(buffer);
		}
	}

	private sealed class InterceptingGenericNonClosingStream(MemoryStream inner, StreamCalls calls) : NonClosingStream<MemoryStream>(inner) {
		public override int Read(byte[] buffer, int offset, int count) {
			calls.Reads++;
			return base.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			calls.Writes++;
			base.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			calls.SpanReads++;
			return base.Read(buffer);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			calls.SpanWrites++;
			base.Write(buffer);
		}
	}

	private sealed class InterceptingConcurrentStream(MemoryStream inner, ICriticalObject criticalObject, StreamCalls calls) : ConcurrentStream(inner, criticalObject) {
		public override int Read(byte[] buffer, int offset, int count) {
			calls.Reads++;
			return base.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			calls.Writes++;
			base.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			calls.SpanReads++;
			return base.Read(buffer);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			calls.SpanWrites++;
			base.Write(buffer);
		}
	}

	private sealed class InheritedBoundedStream(ArrayTransportStream inner) : BoundedStream(inner, 1, 4) {
	}

	private sealed class InheritedGenericBoundedStream(ArrayTransportStream inner) : BoundedStream<ArrayTransportStream>(inner, 1, 4) {
	}

	private sealed class InheritedNonClosingStream(ArrayTransportStream inner) : NonClosingStream(inner) {
	}

	private sealed class InheritedGenericNonClosingStream(ArrayTransportStream inner) : NonClosingStream<ArrayTransportStream>(inner) {
	}

	private sealed class InheritedConcurrentStream(ArrayTransportStream inner, ICriticalObject criticalObject) : ConcurrentStream(inner, criticalObject) {
	}
}
