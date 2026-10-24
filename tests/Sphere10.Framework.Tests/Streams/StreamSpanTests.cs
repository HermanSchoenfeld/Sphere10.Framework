// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamSpanTests {

	[TestCase(false)]
	[TestCase(true)]
	public void DerivedStreamReadsBeyondLogicalEndRetainBackingValidation(bool fragmented) {
		using Stream stream = fragmented ? new BeyondEndFragmentedStream() : new BeyondEndMemoryStream();
		stream.Position = 8;
		var destination = new byte[] { 99, 99 };
		if (fragmented) {
			Assert.That(stream.Read(destination, 0, destination.Length), Is.Zero);
			Assert.That(stream.Read(destination.AsSpan()), Is.Zero);
		} else {
			Assert.That(() => stream.Read(destination, 0, destination.Length), Throws.InstanceOf<ArgumentException>());
			Assert.That(() => stream.Read(destination.AsSpan()), Throws.InstanceOf<ArgumentException>());
		}
		Assert.That(destination, Is.EqualTo(new byte[] { 99, 99 }));
		Assert.That(stream.Position, Is.EqualTo(8));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FragmentedSpanOperations_CrossFragmentsAndPreserveUntouchedBytes(bool inherited) {
		using var stream = inherited ? new InheritedFragmentedStream() : new FragmentedStream(new ByteArrayStreamFragmentProvider(7));
		stream.Write(new byte[25].AsSpan());
		stream.Position = 5;
		ReadOnlySpan<byte> update = stackalloc byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
		stream.Write(update);
		stream.WriteByte(11);
		stream.Position = 3;
		Span<byte> destination = stackalloc byte[25];
		destination.Fill(0xCC);
		Assert.That(stream.Read(destination), Is.EqualTo(22));
		Assert.That(destination[..22].ToArray(), Is.EqualTo(new byte[] { 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0 }));
		Assert.That(destination[22..].ToArray(), Is.All.EqualTo(0xCC));
		Assert.That(stream.ReadByte(), Is.EqualTo(-1));
	}

	[Test]
	public void FragmentedToArray_ReturnsOwnedBytesWithoutMovingPosition() {
		using var stream = new FragmentedStream(new ByteArrayStreamFragmentProvider(3));
		stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
		stream.Position = 2;
		var result = stream.ToArray();
		result[0] = 99;
		Assert.That(stream.Position, Is.EqualTo(2));
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
	}

	[Test]
	public void FragmentedArrayAndByteOperationsDelegateToVirtualSpanOverrides() {
		using var stream = new InterceptingFragmentedStream();
		stream.Write(new byte[] { 1, 2 }, 0, 2);
		stream.Write(new byte[] { 3, 4 }.AsSpan());
		stream.WriteByte(5);
		Assert.That(stream.ArrayWrites, Is.EqualTo(1));
		Assert.That(stream.SpanWrites, Is.EqualTo(3));
		stream.Position = 0;
		var array = new byte[2];
		Assert.That(stream.Read(array, 0, array.Length), Is.EqualTo(2));
		Assert.That(array, Is.EqualTo(new byte[] { 1, 2 }));
		Span<byte> result = stackalloc byte[2];
		Assert.That(stream.Read(result), Is.EqualTo(2));
		Assert.That(result.ToArray(), Is.EqualTo(new byte[] { 3, 4 }));
		Assert.That(stream.ReadByte(), Is.EqualTo(5));
		Assert.That(stream.ArrayReads, Is.EqualTo(1));
		Assert.That(stream.SpanReads, Is.EqualTo(3));
		Assert.That(stream.ReadByte(), Is.EqualTo(-1));
		Assert.That(stream.SpanReads, Is.EqualTo(4));
	}

	[Test]
	public void FragmentedWrites_DoNotCopyWholeFragment() {
		using var stream = new FragmentedStream(new ByteArrayStreamFragmentProvider(new[] { new byte[65536] }, () => new byte[65536]));
		var value = new byte[] { 42 };
		stream.Write(value, 0, 1);
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 0; i < 100; i++)
			stream.Write(value, 0, 1);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.Out.WriteLine($"FragmentedStream: 100 one-byte writes into a 64 KiB fragment allocated {allocated:N0} bytes.");
		Assert.That(allocated, Is.LessThan(1024));
		stream.Position = 100;
		Assert.That(stream.ReadByte(), Is.EqualTo(42));
	}

	[TestCase(0, 15, 1, 1)]
	[TestCase(0, 15, 1, 4)]
	[TestCase(0, 15, 1, 4096)]
	[TestCase(1, 15, 0, 4)]
	[TestCase(0, 4, 10, 3)]
	[TestCase(10, 4, 0, 3)]
	[TestCase(2, 11, 2, 5)]
	public void ShiftBytes_MatchesOverlappingArrayCopy(int source, int count, int destination, int blockSize) {
		var bytes = new byte[20];
		for (var i = 0; i < bytes.Length; i++)
			bytes[i] = (byte)i;
		var expected = (byte[])bytes.Clone();
		Array.Copy(expected, source, expected, destination, count);
		using var stream = new MemoryStream(bytes);
		Tools.Streams.ShiftBytes(stream, source, count, destination, blockSize);
		Assert.That(stream.ToArray(), Is.EqualTo(expected));
		var overlapsRight = destination >= source && destination - source < count;
		Assert.That(stream.Position, Is.EqualTo(overlapsRight ? source + count : destination + count));
	}

	[Test]
	public void ShiftBytesRetainsTheSingleReadFailureContract() {
		var bytes = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
		using var stream = new ShortReadStream((byte[])bytes.Clone(), 2);
		Assert.That(() => Tools.Streams.ShiftBytes(stream, 0, 6, 6, 4),
			Throws.TypeOf<InternalErrorException>().With.Message.EqualTo("Failed to read bytes from stream"));
		Assert.That(stream.ToArray(), Is.EqualTo(bytes));
		Assert.That(stream.Position, Is.EqualTo(2));
	}

	[Test]
	public void ShiftBytes_UsesBoundedScratchForLargeOverlap() {
		using var stream = new MemoryStream(new byte[4 * 1024 * 1024]);
		Tools.Streams.ShiftBytes(stream, 0, stream.Length - 1, 1, 4096);
		var before = GC.GetAllocatedBytesForCurrentThread();
		Tools.Streams.ShiftBytes(stream, 0, stream.Length - 1, 1, 4096);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.Out.WriteLine($"ShiftBytes: overlapping 4 MiB shift with a 4 KiB block allocated {allocated:N0} bytes.");
		Assert.That(allocated, Is.LessThan(1024));
	}

	[Test]
	public void FragmentProvider_ShrinkAndRegrowClearsReleasedTail() {
		using var stream = new FragmentedStream(new ByteArrayStreamFragmentProvider(7));
		stream.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 0, 10);
		stream.SetLength(3);
		stream.SetLength(10);
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 0, 0, 0, 0, 0, 0, 0 }));
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(17)]
	[TestCase(4097)]
	public void EndianCharacterReads_HandleShortReadsAndMultibyteCharacters(int repetitions) {
		var value = string.Concat(System.Linq.Enumerable.Repeat("A\u00E9\U0001F642", repetitions));
		using var stream = new ShortReadStream(Encoding.UTF8.GetBytes(value), 3);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		var characters = new char[value.Length];
		Assert.That(reader.Read(characters, 0, characters.Length), Is.EqualTo(characters.Length));
		Assert.That(new string(characters), Is.EqualTo(value));
	}

	[TestCase(0)]
	[TestCase(10)]
	[TestCase(256)]
	[TestCase(700)]
	public void EndianStringReads_HandleStackAndHeapBuffers(int length) {
		var value = new string('\u00E9', length);
		using var storage = new MemoryStream();
		using (var writer = new BinaryWriter(storage, Encoding.UTF8, true))
			writer.Write(value);
		using var stream = new ShortReadStream(storage.ToArray(), 3);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(reader.ReadString(), Is.EqualTo(value));
	}

	[Test]
	public void EndianSmallStringWrites_PreserveBinaryEncoding() {
		const string value = "A\u00E9\U0001F642";
		using var expected = new MemoryStream();
		using (var writer = new BinaryWriter(expected, Encoding.UTF8, true))
			writer.Write(value);
		using var actual = new MemoryStream();
		using var endianWriter = new EndianBinaryWriter(EndianBitConverter.Little, actual);
		endianWriter.Write(value);
		Assert.That(actual.ToArray(), Is.EqualTo(expected.ToArray()));
	}

	[Test]
	public void EndianStringReads_RejectNegativeLengthsBeforeAllocatingStackMemory() {
		using var stream = new MemoryStream(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x0F });
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, stream);
		Assert.That(() => reader.ReadString(), Throws.TypeOf<OverflowException>());
	}

	private sealed class BeyondEndFragmentedStream : FragmentedStream {
		public BeyondEndFragmentedStream()
			: base(new ByteArrayStreamFragmentProvider(new[] { new byte[] { 1, 2, 3 } }, () => new byte[3])) {
		}

		public override long Position { get; set; }
	}

	private sealed class BeyondEndMemoryStream : ExtendedMemoryStream {
		public BeyondEndMemoryStream()
			: base(new MemoryBuffer(new byte[] { 1, 2, 3 })) {
		}

		public override long Position { get; set; }
	}

	private sealed class ShortReadStream : MemoryStream {
		private readonly int _maximumRead;

		public ShortReadStream(byte[] bytes, int maximumRead)
			: base(bytes) {
			_maximumRead = maximumRead;
		}

		public override int Read(byte[] buffer, int offset, int count)
			=> base.Read(buffer, offset, Math.Min(count, _maximumRead));

		public override int Read(Span<byte> buffer)
			=> base.Read(buffer[..Math.Min(buffer.Length, _maximumRead)]);
	}

	private sealed class InterceptingFragmentedStream : FragmentedStream {
		public InterceptingFragmentedStream()
			: base(new ByteArrayStreamFragmentProvider(7)) {
		}

		public int ArrayReads { get; private set; }

		public int ArrayWrites { get; private set; }

		public int SpanReads { get; private set; }

		public int SpanWrites { get; private set; }

		public override int Read(byte[] buffer, int offset, int count) {
			ArrayReads++;
			return base.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count) {
			ArrayWrites++;
			base.Write(buffer, offset, count);
		}

		public override int Read(Span<byte> buffer) {
			SpanReads++;
			return base.Read(buffer);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			SpanWrites++;
			base.Write(buffer);
		}
	}

	private sealed class InheritedFragmentedStream() : FragmentedStream(new ByteArrayStreamFragmentProvider(7)) {
	}
}
