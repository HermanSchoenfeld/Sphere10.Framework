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
using NUnit.Framework;
using Sphere10.Framework.Collections;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BufferSpanTests {

	[Test]
	public void EmptyDestinationRetainsBackendValidation([Values] StorageType storage) {
		using var cleanup = CreateBuffer(storage, out var buffer);
		if (storage == StorageType.Merkle)
			Assert.That(() => buffer.ReadRange(0, Span<byte>.Empty), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("No pages"));
		else
			buffer.ReadRange(0, Span<byte>.Empty);
		Assert.That(buffer.Count, Is.Zero);
	}

	[Test]
	public void EmptySnapshotRetainsExistingPageValidation([Values] StorageType storage) {
		using var cleanup = CreateBuffer(storage, out var buffer);
		if (storage is StorageType.MemoryPaged or StorageType.File or StorageType.Merkle)
			Assert.That(() => { buffer.ReadSpan(0, 0); }, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("No pages"));
		else
			Assert.That(buffer.ReadSpan(0, 0).IsEmpty, Is.True);
		Assert.That(buffer.Count, Is.Zero);
	}

	[Test]
	public void AdapterReadHandlesDestinationOverlappingItsBackingArray() {
		var backing = new byte[] { 1, 2, 3, 4, 5 };
		using var stream = new ExtendedMemoryStream(new BufferAdapter(backing));
		Assert.That(stream.Read(backing.AsSpan(1, 4)), Is.EqualTo(4));
		Assert.That(backing, Is.EqualTo(new byte[] { 1, 1, 2, 3, 4 }));
	}

	[Test]
	public void AdapterSpanAppendRetainsOverriddenScalarAdd() {
		var buffer = new TrackingBufferAdapter();
		buffer.AddRange(new byte[] { 1, 2, 3 }.AsSpan());
		Assert.That(buffer.Additions, Is.EqualTo(3));
		Assert.That(buffer.ReadSpan(0, 3).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void AdapterSpanAppendSnapshotsInputBeforeInvokingOverrides() {
		var source = new byte[] { 1, 2, 3 };
		var buffer = new SourceMutatingBufferAdapter(source);
		buffer.AddRange(source.AsSpan());
		Assert.That(source, Is.EqualTo(new byte[] { 1, 12, 13 }), "The override must actually mutate the unread source elements.");
		Assert.That(buffer.ReadSpan(0, 3).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void AdapterSpanOperationsHaveTheSameBehaviorWhenInherited([Values] bool inherited) {
		BufferAdapter buffer = inherited ? new InheritedBufferAdapter() : new BufferAdapter();
		buffer.AddRange(new byte[] { 1, 2, 3 }.AsSpan());
		buffer.UpdateRange(1, new byte[] { 20 }.AsSpan());
		buffer.InsertRange(2, new byte[] { 8, 9 }.AsSpan());
		var destination = Enumerable.Repeat((byte)255, 7).ToArray();
		buffer.ReadRange(0, destination.AsSpan(1, 5));
		Assert.That(destination, Is.EqualTo(new byte[] { 255, 1, 20, 8, 9, 3, 255 }));
	}

	[TestCase(StorageType.Adapter)]
	[TestCase(StorageType.MemoryPaged)]
	[TestCase(StorageType.Stream)]
	public void CallerStorageReadsRetainExplicitDestinationProjection(StorageType storage) {
		using var backingStream = new MemoryStream();
		IBuffer buffer = storage switch {
			StorageType.Adapter => new InterfaceProjectedBufferAdapter(),
			StorageType.MemoryPaged => new InterfaceProjectedMemoryPagedBuffer(),
			StorageType.Stream => new InterfaceProjectedStreamMappedBuffer(backingStream),
			_ => throw new ArgumentOutOfRangeException(nameof(storage))
		};
		using var bufferCleanup = buffer as IDisposable;
		buffer.AddRange(new byte[] { 1, 2, 3 }.AsSpan());
		var expected = buffer.ReadSpan(0, 3).ToArray();
		Assert.That(expected, Is.EqualTo(new byte[] { 11, 12, 13 }));
		Span<byte> destination = stackalloc byte[3];
		buffer.ReadRange(0, destination);
		Assert.That(destination.ToArray(), Is.EqualTo(expected));
		using var stream = new ExtendedMemoryStream(buffer);
		var arrayDestination = new byte[3];
		Assert.That(stream.Read(arrayDestination, 0, 3), Is.EqualTo(3));
		Assert.That(arrayDestination, Is.EqualTo(expected));
		stream.Position = 0;
		Assert.That(stream.Read(destination), Is.EqualTo(3));
		Assert.That(destination.ToArray(), Is.EqualTo(expected));
	}

	[TestCase(StorageType.Adapter)]
	[TestCase(StorageType.Stream)]
	public void CallerStorageReadsUseTheProjectedLogicalExtent(StorageType storage) {
		using var backingStream = new MemoryStream(new byte[] { 1, 2, 3 });
		IBuffer buffer = storage == StorageType.Adapter ? new ExtendedProjectionBufferAdapter() : new ExtendedProjectionStreamMappedBuffer(backingStream);
		Assert.That(buffer.Count, Is.EqualTo(6));
		Assert.That(storage == StorageType.Adapter ? ((BufferAdapter)buffer).Count : ((StreamMappedBuffer)buffer).Count, Is.EqualTo(3));
		Span<byte> destination = stackalloc byte[3];
		buffer.ReadRange(3, destination);
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 14, 15, 16 }));
		using var stream = new ExtendedMemoryStream(buffer);
		stream.Position = 3;
		var arrayDestination = new byte[3];
		Assert.That(stream.Read(arrayDestination, 0, 3), Is.EqualTo(3));
		Assert.That(arrayDestination, Is.EqualTo(new byte[] { 14, 15, 16 }));
		stream.Position = 3;
		Assert.That(stream.Read(destination), Is.EqualTo(3));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 14, 15, 16 }));
	}

	[Test]
	public void StreamCallerStorageReadRetainsDecoratorReadSpanBehavior([Values(false, true)] bool paged) {
		using var memory = new MemoryPagedBuffer(10, 10);
		IBuffer buffer = paged ? new TransformingPagedBufferDecorator(memory) : new TransformingBufferDecorator(new MemoryBuffer());
		buffer.AddRange(new byte[] { 1, 2, 3 }.AsSpan());
		using var stream = new ExtendedMemoryStream(buffer);
		Span<byte> destination = stackalloc byte[3];
		Assert.That(stream.Read(destination), Is.EqualTo(3));
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 2, 3, 4 }));
	}

	[Test]
	public void StreamMaterializationRetainsCustomCopyToBehavior() {
		var buffer = new CopyingBufferDecorator(new MemoryBuffer());
		buffer.AddRange(new byte[] { 1, 2, 3 }.AsSpan());
		using var stream = new ExtendedMemoryStream(buffer);
		Assert.That(stream.ToArray(), Is.EqualTo(new byte[] { 11, 12, 13 }));
		Assert.That(buffer.CopyCalls, Is.EqualTo(1));
		Assert.That(buffer.ReadSpan(0, 3).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
	}

	[Test]
	public void ReadIntoCallerStoragePreservesUnrequestedBytes([Values] StorageType storage) {
		using var cleanup = CreateBuffer(storage, out var buffer);
		var input = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray();
		buffer.AddRange(input.AsSpan());
		var destination = Enumerable.Repeat((byte)255, 95).ToArray();
		buffer.ReadRange(7, destination.AsSpan(3, 89));
		Assert.That(destination[..3], Is.All.EqualTo(255));
		Assert.That(destination[3..92], Is.EqualTo(input[7..96]));
		Assert.That(destination[92..], Is.All.EqualTo(255));
		buffer.ReadRange(buffer.Count, Span<byte>.Empty);
	}

	[Test]
	public void PagedReadIntoCallerStorageRetainsPageNotificationsAndEvictionSafety() {
		using var buffer = new MemoryPagedBuffer(10, 10);
		var input = Enumerable.Range(0, 40).Select(value => (byte)value).ToArray();
		buffer.AddRange(input.AsSpan());
		var notifications = new List<string>();
		buffer.PageReading += (_, page) => notifications.Add($"reading {page.Number}");
		buffer.PageRead += (_, page) => notifications.Add($"read {page.Number}");
		var destination = new byte[27];
		buffer.ReadRange(7, destination.AsSpan());
		Assert.That(destination, Is.EqualTo(input[7..34]));
		Assert.That(notifications, Is.EqualTo(new[] { "reading 0", "read 0", "reading 1", "read 1", "reading 2", "read 2", "reading 3", "read 3" }));
		buffer.UpdateRange(7, new byte[27].AsSpan());
		Assert.That(destination, Is.EqualTo(input[7..34]), "The caller's bytes must survive subsequent page changes and eviction.");
	}

	[Test]
	public void MaterializedPagedReadAllocatesOnePayloadArray() {
		const int payloadLength = 256 * 1024;
		using var buffer = new MemoryPagedBuffer(64 * 1024, payloadLength);
		var input = new byte[payloadLength];
		new Random(31337).NextBytes(input);
		buffer.AddRange(input.AsSpan());
		_ = buffer.ReadRange(0, payloadLength);
		var before = GC.GetAllocatedBytesForCurrentThread();
		var result = buffer.ReadRange(0, payloadLength);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(result, Is.EqualTo(input));
		Assert.That(allocated, Is.LessThan(payloadLength + 32 * 1024), "Reading must allocate the result, not a growing builder and another copy.");
		TestContext.WriteLine($"Paged ReadRange({payloadLength}) allocated {allocated} bytes.");
	}

	[Test]
	public void InheritedPagedDestinationReadsAvoidPayloadSnapshots([Values] bool inherited) {
		const int payloadLength = 64 * 1024;
		using var buffer = inherited ? new InheritedMemoryPagedBuffer(payloadLength) : new MemoryPagedBuffer(payloadLength, payloadLength);
		var input = new byte[payloadLength];
		new Random(4281).NextBytes(input);
		buffer.AddRange(input.AsSpan());
		var destination = new byte[payloadLength];
		buffer.ReadRange(0, destination.AsSpan());
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var iteration = 0; iteration < 32; iteration++)
			buffer.ReadRange(0, destination.AsSpan());
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(destination, Is.EqualTo(input));
		Assert.That(allocated, Is.LessThan(payloadLength), "Destination reads must not allocate a payload snapshot for each operation, including inherited implementations.");
		TestContext.WriteLine($"Paged destination reads, inherited={inherited}: {allocated} bytes over 32 reads.");
	}

	[Test]
	public void TransactionalCallerStorageReadSurvivesCommitRollbackAndReopen() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var descriptor = TransactionalFileDescriptor.From(Path.Combine(directory, "data.bin"), 10, 10);
		var input = Enumerable.Range(0, 37).Select(value => (byte)value).ToArray();
		using (var buffer = new TransactionalFileMappedBuffer(descriptor)) {
			buffer.AddRange(input.AsSpan());
			buffer.Commit();
			buffer.UpdateRange(7, new byte[25].AsSpan());
			var changed = new byte[25];
			buffer.ReadRange(7, changed.AsSpan());
			Assert.That(changed, Is.All.Zero);
			buffer.Rollback();
			buffer.ReadRange(7, changed.AsSpan());
			Assert.That(changed, Is.EqualTo(input[7..32]));
		}
		using var reopened = new TransactionalFileMappedBuffer(descriptor);
		if (reopened.RequiresLoad)
			reopened.Load();
		var persisted = new byte[input.Length];
		reopened.ReadRange(0, persisted.AsSpan());
		Assert.That(persisted, Is.EqualTo(input));
	}

	private static IDisposable CreateBuffer(StorageType storage, out IBuffer buffer) {
		switch (storage) {
			case StorageType.Memory:
				buffer = new MemoryBuffer();
				return Disposables.None;
			case StorageType.MemoryPaged:
				var paged = new MemoryPagedBuffer(10, 10);
				buffer = paged;
				return paged;
			case StorageType.File:
				var fileName = Tools.FileSystem.GetTempFileName(false);
				var file = new FileMappedBuffer(PagedFileDescriptor.From(fileName, 10, 10));
				buffer = file;
				return new Disposables(file, Tools.Scope.DeleteFileOnDispose(fileName));
			case StorageType.Stream:
				var stream = new MemoryStream();
				buffer = new StreamMappedBuffer(stream);
				return stream;
			case StorageType.Adapter:
				buffer = new BufferAdapter();
				return Disposables.None;
			case StorageType.Decorator:
				buffer = new TestBufferDecorator(new MemoryBuffer());
				return Disposables.None;
			case StorageType.Merkle:
				var merkleBuffer = new MemoryPagedBuffer(10, 10);
				buffer = new MerklePagedBuffer(merkleBuffer, CHF.SHA2_256);
				return merkleBuffer;
			default:
				throw new ArgumentOutOfRangeException(nameof(storage));
		}
	}

	private static ReadOnlySpan<byte> AddProjectionOffset(ReadOnlySpan<byte> bytes) {
		var result = bytes.ToArray();
		for (var index = 0; index < result.Length; index++)
			result[index] += 10;
		return result;
	}

	public enum StorageType {
		Memory,
		MemoryPaged,
		File,
		Stream,
		Adapter,
		Decorator,
		Merkle
	}

	private sealed class TestBufferDecorator : BufferDecorator<IBuffer> {
		public TestBufferDecorator(IBuffer buffer)
			: base(buffer) {
		}
	}

	private sealed class InheritedBufferAdapter : BufferAdapter {
	}

	private sealed class SourceMutatingBufferAdapter : BufferAdapter {
		private readonly byte[] _source;
		private int _additions;

		public SourceMutatingBufferAdapter(byte[] source) {
			_source = source;
		}

		public override void Add(byte value) {
			base.Add(value);
			_additions++;
			if (_additions < _source.Length)
				_source[_additions] += 10;
		}
	}

	private sealed class TrackingBufferAdapter : BufferAdapter {
		public int Additions { get; private set; }

		public override void Add(byte value) {
			Additions++;
			base.Add(value);
		}
	}

	private sealed class CopyingBufferDecorator : BufferDecorator<IBuffer> {
		public CopyingBufferDecorator(IBuffer buffer)
			: base(buffer) {
		}

		public int CopyCalls { get; private set; }

		public override void CopyTo(byte[] destination, int offset) {
			CopyCalls++;
			base.CopyTo(destination, offset);
			for (var index = offset; index < offset + Count; index++)
				destination[index] += 10;
		}
	}

	private sealed class TransformingBufferDecorator : BufferDecorator<IBuffer> {
		public TransformingBufferDecorator(IBuffer buffer)
			: base(buffer) {
		}

		public override ReadOnlySpan<byte> ReadSpan(long index, long count) {
			var result = base.ReadSpan(index, count).ToArray();
			for (var offset = 0; offset < result.Length; offset++)
				result[offset]++;
			return result;
		}
	}

	private sealed class TransformingPagedBufferDecorator : MemoryPagedBufferDecorator<MemoryPagedBuffer> {
		public TransformingPagedBufferDecorator(MemoryPagedBuffer buffer)
			: base(buffer) {
		}

		public override ReadOnlySpan<byte> ReadSpan(long index, long count) {
			var result = base.ReadSpan(index, count).ToArray();
			for (var offset = 0; offset < result.Length; offset++)
				result[offset]++;
			return result;
		}
	}

	private sealed class InterfaceProjectedBufferAdapter : BufferAdapter, IBuffer {
		ReadOnlySpan<byte> IBuffer.ReadSpan(long index, long count) => AddProjectionOffset(base.ReadSpan(index, count));
	}

	private sealed class InterfaceProjectedMemoryPagedBuffer : MemoryPagedBuffer, IBuffer {
		public InterfaceProjectedMemoryPagedBuffer()
			: base(10, 10) {
		}

		ReadOnlySpan<byte> IBuffer.ReadSpan(long index, long count) => AddProjectionOffset(base.ReadSpan(index, count));

		public override void ReadRange(long index, Span<byte> destination)
			=> ((IBuffer)this).ReadSpan(index, destination.Length).CopyTo(destination);
	}

	private sealed class InterfaceProjectedStreamMappedBuffer : StreamMappedBuffer, IBuffer {
		public InterfaceProjectedStreamMappedBuffer(Stream stream)
			: base(stream) {
		}

		ReadOnlySpan<byte> IBuffer.ReadSpan(long index, long count) => AddProjectionOffset(base.ReadSpan(index, count));

		public override void ReadRange(long index, Span<byte> destination)
			=> ((IBuffer)this).ReadSpan(index, destination.Length).CopyTo(destination);
	}

	private sealed class ExtendedProjectionBufferAdapter : BufferAdapter, IBuffer {
		private readonly byte[] _projection = { 11, 12, 13, 14, 15, 16 };

		public ExtendedProjectionBufferAdapter()
			: base(new byte[] { 1, 2, 3 }) {
		}

		long IExtendedCollection<byte>.Count => _projection.Length;
		ReadOnlySpan<byte> IBuffer.ReadSpan(long index, long count) => _projection.AsSpan((int)index, (int)count);
	}

	private sealed class ExtendedProjectionStreamMappedBuffer : StreamMappedBuffer, IBuffer {
		private readonly byte[] _projection = { 11, 12, 13, 14, 15, 16 };

		public ExtendedProjectionStreamMappedBuffer(Stream stream)
			: base(stream) {
		}

		long IExtendedCollection<byte>.Count => _projection.Length;
		ReadOnlySpan<byte> IBuffer.ReadSpan(long index, long count) => _projection.AsSpan((int)index, (int)count);

		public override void ReadRange(long index, Span<byte> destination)
			=> ((IBuffer)this).ReadSpan(index, destination.Length).CopyTo(destination);
	}

	private sealed class InheritedMemoryPagedBuffer : MemoryPagedBuffer {
		public InheritedMemoryPagedBuffer(int pageSize)
			: base(pageSize, pageSize) {
		}
	}

}
