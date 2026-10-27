// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Owns memory returned by WTS APIs; this allocation must be freed by WTSFreeMemory.</summary>
public sealed class WtsMemoryScope : IScope {
	private IntPtr _memory;
	private int _disposed;

	public WtsMemoryScope(IntPtr memory) { _memory = memory; }

	public IntPtr Pointer {
		get { ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this); return _memory; }
	}

	public void Dispose() {
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;
		var memory = Interlocked.Exchange(ref _memory, IntPtr.Zero);
		if (memory != IntPtr.Zero)
			WinAPI.WTSAPI32.WTSFreeMemory(memory);
	}

	public ValueTask DisposeAsync() => new(Task.Run(Dispose));
}
