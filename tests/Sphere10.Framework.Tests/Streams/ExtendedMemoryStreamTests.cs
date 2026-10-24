// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;
using Sphere10.Framework.NUnit;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable]
public class ExtendedMemoryStreamTests {

	[Test]
	public void Empty([Values] InnerListType listType) {
		using (CreateTestStream(listType, 10, out var stream)) {
			var expected = new MemoryStream();
			Assert.That(stream.Position, Is.EqualTo(expected.Position));
			Assert.That(stream.Length, Is.EqualTo(expected.Length));
			Assert.That(stream.ToArray(), Is.EqualTo(expected.ToArray()));
		}
	}

	[Test]
	public void ReadAll([Values] InnerListType listType) {
		using (CreateTestStream(listType, 10, out var stream)) {
			var expected = new MemoryStream();
			var data = new Random(31337).NextBytes(100);
			stream.WriteBytes(data);
			expected.WriteBytes(data);
			Assert.That(stream.ReadBytes(100), Is.EqualTo(expected.ReadBytes(100)));
			Assert.That(stream.Position, Is.EqualTo(expected.Position));
			Assert.That(stream.Length, Is.EqualTo(expected.Length));
			Assert.That(stream.ToArray(), Is.EqualTo(expected.ToArray()));
		}
	}

	[Test]
	public void RemoveAll([Values] InnerListType listType) {
		using (CreateTestStream(listType, 10, out var stream)) {
			var expected = new MemoryStream();
			var data = new Random(31337).NextBytes(100);
			stream.WriteBytes(data);
			expected.WriteBytes(data);
			stream.SetLength(0);
			expected.SetLength(0);
			Assert.That(stream.Position, Is.EqualTo(expected.Position));
			Assert.That(stream.Length, Is.EqualTo(expected.Length));
			Assert.That(stream.ToArray(), Is.EqualTo(expected.ToArray()));
		}
	}

	[Test]
	public void SeekEnd_Empty([Values] InnerListType listType) {
		using (CreateTestStream(listType, 10, out var stream)) {
			var expected = new MemoryStream();
			stream.Seek(0, SeekOrigin.End);
			expected.Seek(0, SeekOrigin.End);
			Assert.That(stream.Position, Is.EqualTo(expected.Position));
		}
	}

	[Test]
	public void SeekEnd_NotEmpty([Values] InnerListType listType) {
		using (CreateTestStream(listType, 10, out var stream)) {
			var expected = new MemoryStream();
			var data = new Random(31337).NextBytes(100);
			stream.WriteBytes(data);
			expected.WriteBytes(data);
			stream.Seek(0, SeekOrigin.End);
			expected.Seek(0, SeekOrigin.End);
			Assert.That(stream.Position, Is.EqualTo(expected.Position));
		}
	}

	[Test]
	public void IntegrationTests([Values] InnerListType listType, [Values(0, 3, 111, 9371)] int maxSize) {
		var RNG = new Random(maxSize);
		using (CreateTestStream(listType, maxSize, out var stream))
			AssertEx.StreamIntegrationTests(maxSize, stream, RNG: RNG);
	}

	[Test]
	public void SpanWritesOverwriteThenAppendAndReadToEnd([Values] InnerListType listType) {
		using var cleanup = CreateTestStream(listType, 100, out var stream);
		stream.Write(new byte[] { 1, 2, 3, 4 }.AsSpan());
		stream.Position = 2;
		ReadOnlySpan<byte> replacement = stackalloc byte[] { 8, 9, 10, 11 };
		stream.Write(replacement);
		stream.Position = 1;
		Span<byte> destination = stackalloc byte[8];
		destination.Fill(255);
		Assert.That(stream.Read(destination), Is.EqualTo(5));
		Assert.That(destination[..5].ToArray(), Is.EqualTo(new byte[] { 2, 8, 9, 10, 11 }));
		Assert.That(destination[5..].ToArray(), Is.All.EqualTo(255));
		Assert.That(stream.Read(destination), Is.Zero);
		Assert.That(stream.ReadByte(), Is.EqualTo(-1));
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 1, 2, 8, 9, 10, 11 }));
	}

	[Test]
	public void SetLengthClearsTruncatedAndNewBytes([Values] InnerListType listType) {
		using var cleanup = CreateTestStream(listType, 20000, out var stream);
		var input = new byte[20000];
		input.AsSpan().Fill(255);
		stream.Write(input.AsSpan());
		stream.SetLength(13);
		stream.SetLength(20000);
		var result = stream.ToArray();
		Assert.That(result[..13], Is.All.EqualTo(255));
		Assert.That(result[13..], Is.All.EqualTo(0));
		Assert.That(stream.Position, Is.EqualTo(13));
	}

	[Test]
	public void GrowingMemoryStreamInvalidatesExistingBufferEnumerator() {
		var buffer = new MemoryBuffer();
		buffer.AddRange(new byte[] { 1, 2 }.AsSpan());
		using var enumerator = buffer.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		using var stream = new ExtendedMemoryStream(buffer);
		stream.SetLength(3);
		Assert.That(() => enumerator.MoveNext(), Throws.TypeOf<InvalidOperationException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SpanReadAvoidsPayloadSizedScratchAllocation(bool inherited) {
		const int payloadLength = 256 * 1024;
		using var buffer = new MemoryPagedBuffer(64 * 1024, payloadLength);
		using var stream = inherited ? new InheritedExtendedMemoryStream(buffer) : new ExtendedMemoryStream(buffer);
		var payload = new byte[payloadLength];
		new Random(31337).NextBytes(payload);
		stream.Write(payload.AsSpan());
		stream.Position = 0;
		var destination = new byte[payloadLength];
		stream.ReadExactly(destination.AsSpan());
		stream.Position = 0;
		var before = GC.GetAllocatedBytesForCurrentThread();
		var read = stream.Read(destination.AsSpan());
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(read, Is.EqualTo(payloadLength));
		Assert.That(destination, Is.EqualTo(payload));
		Assert.That(allocated, Is.LessThan(32 * 1024), "Caller-owned storage should avoid any payload-sized temporary array.");
		TestContext.WriteLine($"ExtendedMemoryStream.Read(Span) for {payloadLength} bytes allocated {allocated} bytes.");
	}

	[Test]
	public void ToArrayAllocatesOnePayloadArray() {
		const int payloadLength = 64 * 1024;
		using var stream = new ExtendedMemoryStream();
		var payload = new byte[payloadLength];
		new Random(31337).NextBytes(payload);
		stream.Write(payload.AsSpan());
		_ = stream.ToArray();
		var before = GC.GetAllocatedBytesForCurrentThread();
		var result = stream.ToArray();
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(result, Is.EqualTo(payload));
		Assert.That(allocated, Is.LessThan(payloadLength + 1024), "Materialization should not enumerate bytes through allocating single-item reads.");
		TestContext.WriteLine($"ExtendedMemoryStream.ToArray({payloadLength}) allocated {allocated} bytes.");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void SingleByteMemoryOperationsDoNotAllocatePerByteArrays(bool inherited) {
		var buffer = new MemoryBuffer(4096, 4096);
		using var stream = inherited ? new InheritedExtendedMemoryStream(buffer) : new ExtendedMemoryStream(buffer);
		stream.WriteByte(7);
		stream.Position = 0;
		Assert.That(stream.ReadByte(), Is.EqualTo(7));
		stream.Position = 0;
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var index = 0; index < 4096; index++)
			stream.WriteByte((byte)index);
		stream.Position = 0;
		var checksum = 0;
		for (var index = 0; index < 4096; index++)
			checksum += stream.ReadByte();
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(checksum, Is.EqualTo(522240));
		Assert.That(allocated, Is.LessThan(1024), "Byte operations must use the span path instead of allocating a one-byte array each time.");
		TestContext.WriteLine($"8192 byte stream operations allocated {allocated} bytes.");
	}

	[Test]
	public void ArrayAndByteOperationsDelegateToVirtualSpanOverrides() {
		using var stream = new TrackingExtendedMemoryStream();
		stream.Write(new byte[] { 1, 2 }, 0, 2);
		stream.Write(new byte[] { 3, 4 }.AsSpan());
		stream.WriteByte(5);
		Assert.That(stream.ArrayWrites, Is.EqualTo(1));
		Assert.That(stream.SpanWrites, Is.EqualTo(3));
		stream.Position = 0;
		var array = new byte[2];
		Assert.That(stream.Read(array, 0, array.Length), Is.EqualTo(2));
		Assert.That(array, Is.EqualTo(new byte[] { 1, 2 }));
		Span<byte> destination = stackalloc byte[2];
		Assert.That(stream.Read(destination), Is.EqualTo(2));
		Assert.That(stream.ReadByte(), Is.EqualTo(5));
		Assert.That(stream.ArrayReads, Is.EqualTo(1));
		Assert.That(stream.SpanReads, Is.EqualTo(3));
		Assert.That(stream.ReadByte(), Is.EqualTo(-1));
		Assert.That(stream.SpanReads, Is.EqualTo(4));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 3, 4 }));
	}

	private IDisposable CreateTestStream(InnerListType listType, int maxSize, out ExtendedMemoryStream stream) {
		var pageSize = Math.Max(1, maxSize / 5);
		var maxOpenPages = 2;
		var disposables = new Disposables();

		switch (listType) {
			case InnerListType.MemoryBuffer:
				stream = new ExtendedMemoryStream(new MemoryBuffer());
				return Disposables.None;
			case InnerListType.MemoryPagedBuffer:
				var memPagedBuffer = new MemoryPagedBuffer(pageSize, maxOpenPages * pageSize);
				stream = new ExtendedMemoryStream(memPagedBuffer);
				return new Disposables(stream, memPagedBuffer);
			case InnerListType.BinaryFile:
				var tmpFile = Tools.FileSystem.GetTempFileName(false);
				var binaryFile = new FileMappedBuffer(PagedFileDescriptor.From(tmpFile, pageSize, maxOpenPages * pageSize));
				stream = new ExtendedMemoryStream(binaryFile);
				return new Disposables(stream, binaryFile,  Tools.Scope.DeleteFileOnDispose(tmpFile));
			case InnerListType.TransactionalBinaryFile:
				var baseDir = Tools.FileSystem.GetTempEmptyDirectory(true);
				var fileName = Path.Combine(baseDir, "File.dat");
				var transactionalBinaryFile = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From( fileName, baseDir, pageSize, maxOpenPages * pageSize));
				stream = new ExtendedMemoryStream(transactionalBinaryFile);
				return new Disposables(stream, transactionalBinaryFile, Tools.Scope.DeleteDirOnDispose(baseDir));
			default:
				throw new ArgumentOutOfRangeException(nameof(listType), listType, null);
		}
	}


	public enum InnerListType {
		MemoryBuffer,
		MemoryPagedBuffer,
		BinaryFile,
		TransactionalBinaryFile
	}

	private sealed class TrackingExtendedMemoryStream : ExtendedMemoryStream {
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

	private sealed class InheritedExtendedMemoryStream(IBuffer buffer) : ExtendedMemoryStream(buffer) {
	}
}

