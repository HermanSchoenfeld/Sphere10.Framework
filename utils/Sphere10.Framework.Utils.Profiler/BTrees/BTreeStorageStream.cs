// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Sphere10.Framework.Utils.Profiler.BTrees;

internal static class BTreeStorageStream {
	public static Stream Open(BTreeProfile profile, string path, long capacity) {
		switch (profile.Storage) {
			case BTreeStorage.StreamMemory: return new MemoryStream();
			case BTreeStorage.FileStream: return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, profile.FileBufferBytes);
			case BTreeStorage.FilePaged:
				return new ExtendedMemoryStream(new FileMappedBuffer(PagedFileDescriptor.From(path, profile.PageSize, profile.CacheBytes)), true);
			case BTreeStorage.TransactionalFile:
				return new TransactionalStream(TransactionalFileDescriptor.From(path, "pages", profile.PageSize, profile.CacheBytes));
			case BTreeStorage.OSMemoryMapped: return new CapacityMappedStream(path, capacity);
			default: throw new ArgumentException("Managed trees have no backing stream.", nameof(profile));
		}
	}

	// A genuinely OS-mapped view. Provision capacity outside measured operations, preserve logical tree length,
	// and truncate capacity on close so ordinary reopening sees the exact serialized node count.
	private sealed class CapacityMappedStream : StreamDecorator<MemoryMappedViewStream> {
		private readonly FileStream _file;
		private readonly MemoryMappedFile _mapping;
		private readonly MemoryMappedViewStream _view;
		private readonly long _capacity;
		private long _length;
		private bool _disposed;

		public CapacityMappedStream(string path, long capacity)
			: this(CreateMapping(path, capacity), capacity) {
		}

		private CapacityMappedStream((FileStream File, MemoryMappedFile Mapping, MemoryMappedViewStream View, long Length) resources, long capacity)
			: base(resources.View) {
			_file = resources.File;
			_mapping = resources.Mapping;
			_view = resources.View;
			_capacity = capacity;
			_length = resources.Length;
		}

		public override long Length => _length;

		public override void SetLength(long value) {
			Guard.ArgumentInRange(value, 0, _capacity, nameof(value));
			_length = value;
			if (Position > value)
				Position = value;
		}

		public override long Seek(long offset, SeekOrigin origin) {
			var target = origin switch { SeekOrigin.Begin => offset, SeekOrigin.Current => Position + offset, SeekOrigin.End => _length + offset,
				_ => throw new ArgumentOutOfRangeException(nameof(origin)) };
			Guard.ArgumentInRange(target, 0, _capacity, nameof(offset));
			return _view.Seek(target, SeekOrigin.Begin);
		}

		public override int Read(byte[] buffer, int offset, int count) => _view.Read(buffer, offset, (int)Math.Min(count, Math.Max(0, _length - Position)));

		public override int Read(Span<byte> buffer) => _view.Read(buffer[..(int)Math.Min(buffer.Length, Math.Max(0, _length - Position))]);

		public override int ReadByte() => Position >= _length ? -1 : _view.ReadByte();

		public override void Write(byte[] buffer, int offset, int count) {
			Guard.Ensure(Position + count <= _capacity, "The provisioned OS-mapped capacity was exhausted.");
			_view.Write(buffer, offset, count);
			_length = Math.Max(_length, Position);
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			Guard.Ensure(Position + buffer.Length <= _capacity, "The provisioned OS-mapped capacity was exhausted.");
			_view.Write(buffer);
			_length = Math.Max(_length, Position);
		}

		public override void WriteByte(byte value) {
			Guard.Ensure(Position < _capacity, "The provisioned OS-mapped capacity was exhausted.");
			_view.WriteByte(value);
			_length = Math.Max(_length, Position);
		}

		public override void Close() => Dispose(true);

		protected override void Dispose(bool disposing) {
			if (_disposed)
				return;
			_disposed = true;
			if (disposing) {
				using (_file) {
					using (_mapping)
						using (_view)
							_view.Flush();
					_file.SetLength(_length);
				}
			}
		}

		private static (FileStream File, MemoryMappedFile Mapping, MemoryMappedViewStream View, long Length) CreateMapping(string path, long capacity) {
			var file = (FileStream)null;
			var mapping = (MemoryMappedFile)null;
			var view = (MemoryMappedViewStream)null;
			try {
				file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
				var length = file.Length;
				Guard.Ensure(length <= capacity, "Existing tree exceeds the provisioned OS-mapped capacity.");
				file.SetLength(capacity);
				mapping = MemoryMappedFile.CreateFromFile(file, null, capacity, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
				view = mapping.CreateViewStream(0, capacity, MemoryMappedFileAccess.ReadWrite);
				return (file, mapping, view, length);
			} catch {
				// Constructor failure retains ownership of every resource not transferred to the stream instance.
				view?.Dispose();
				mapping?.Dispose();
				file?.Dispose();
				throw;
			}
		}
	}
}
