// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>A seekable, read-only stream over one address range of another process. Every read fetches only the requested bytes, so a large region is never copied as a whole.</summary>
/// <remarks>The stream borrows the opened process unless told to own it; the caller verifies the process identity. An unreadable page fails the read with the Windows error instead of inventing bytes.</remarks>
public sealed class WindowsProcessMemoryStream : Stream {
	/// <summary>The largest single native read, matching <see cref="WindowsProcess.ReadMemory"/>.</summary>
	public const int MaximumBlockLength = 1048576;

	private readonly WindowsProcess _process;
	private readonly bool _ownsProcess;
	private long _position;

	public WindowsProcessMemoryStream(WindowsProcess process, ulong address, ulong length)
		: this(process, address, length, false) {
	}

	/// <summary>Creates the stream; an owned process is disposed with the stream, so a viewer can hold a region open on its own.</summary>
	public WindowsProcessMemoryStream(WindowsProcess process, ulong address, ulong length, bool ownsProcess) {
		Guard.ArgumentNotNull(process, nameof(process));
		Guard.Argument(length > 0 && length <= long.MaxValue, nameof(length), "Specify a nonempty memory range within the stream length limit.");
		Guard.Argument(address <= ulong.MaxValue - length, nameof(length), "The memory range overflows its address space.");
		_process = process;
		_ownsProcess = ownsProcess;
		BaseAddress = address;
		Length = (long)length;
	}

	/// <summary>The target address of stream position zero.</summary>
	public ulong BaseAddress { get; }

	public override bool CanRead => true;

	public override bool CanSeek => true;

	public override bool CanWrite => false;

	public override long Length { get; }

	public override long Position {
		get => _position;
		set {
			Guard.ArgumentInRange(value, 0, Length, nameof(value));
			_position = value;
		}
	}

	public override int Read(byte[] buffer, int offset, int count) => Task.ExecuteSyncOverAsync(() => ReadAsync(buffer, offset, count));

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
		ValidateBufferArguments(buffer, offset, count);
		var requested = (int)Math.Min(Math.Min(count, Length - _position), MaximumBlockLength);
		if (requested <= 0)
			return 0;
		var address = BaseAddress + (ulong)_position;
		var read = await _process.ReadMemoryAsync(address, requested, cancellationToken).ConfigureAwait(false);
		if (read.Bytes.Length == 0 && read.ErrorCode != 0)
			throw new Win32Exception(read.ErrorCode, $"Windows could not read {requested:N0} bytes at 0x{address:X} in process {_process.Identity.ProcessId}.");
		Array.Copy(read.Bytes, 0, buffer, offset, read.Bytes.Length);
		_position += read.Bytes.Length;
		return read.Bytes.Length;
	}

	public override long Seek(long offset, SeekOrigin origin) {
		var target = origin switch {
			SeekOrigin.Begin => offset,
			SeekOrigin.Current => _position + offset,
			SeekOrigin.End => Length + offset,
			_ => throw new ArgumentOutOfRangeException(nameof(origin))
		};
		Position = target;
		return _position;
	}

	public override void Flush() {
	}

	public override void SetLength(long value) => throw new NotSupportedException("Process memory has a fixed range.");

	public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("Process memory is read only through this stream.");

	protected override void Dispose(bool disposing) {
		if (disposing && _ownsProcess)
			_process.Dispose();
		base.Dispose(disposing);
	}
}
