// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;

namespace Sphere10.Framework;

/// <summary>
/// A collection of bytes that is mapped over a file and accessed via intelligent memory-mapped paging.
/// <remarks>
/// By varying the pageSize and in inMemoryPages arguments the performance of this list can by tuned to meet the specific use case.
/// In cases where appending is more demanded, the page sizes should be larger so they are swapped less often.
/// In cases where random accessing the list, the page sizes should be smaller so they can be loaded faster.
/// In general, the more pages allowed in memory the less frequently they will be swapped to streams.
/// The buffer is a <see cref="MemoryPagedBuffer"/> over the file it opens, which it owns and closes when disposed.
/// </remarks>
/// </summary>
public sealed class FileMappedBuffer : MemoryPagedBuffer, IFilePagedList<byte> {

	public FileMappedBuffer(PagedFileDescriptor fileDescriptor, FileAccessMode accessMode = FileAccessMode.Default)
		: base(FileAccessHelper.Open(fileDescriptor, accessMode, out _, out _), fileDescriptor.PageSize, fileDescriptor.MaxMemory, leaveOpen: false, autoLoad: false,
			pageReapPolicy: fileDescriptor.PageReapPolicy) {
		FileDescriptor = fileDescriptor;
		AccessMode = accessMode;
		if (accessMode.HasFlag(FileAccessMode.AutoLoad))
			Load();
	}

	public FileAccessMode AccessMode { get; }

	public PagedFileDescriptor FileDescriptor { get; }

	public override bool IsReadOnly => AccessMode.IsReadOnly();

	protected override void FlushBackingStream() => ((FileStream)BackingStream).Flush(true);
}
