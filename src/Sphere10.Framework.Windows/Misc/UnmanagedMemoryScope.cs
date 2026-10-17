// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Owns an HGlobal allocation. Never use for memory returned by an API requiring LocalFree, NetApiBufferFree or another allocator.</summary>
public sealed class UnmanagedMemoryScope : SyncScope, IScope<IntPtr> {
	private IntPtr _pointer;

	public UnmanagedMemoryScope(int size) {
		Guard.ArgumentGT(size, 0, nameof(size));
		Size = size;
		_pointer = Marshal.AllocHGlobal(size);
	}

	private UnmanagedMemoryScope(IntPtr pointer, int size) {
		_pointer = pointer;
		Size = size;
	}

	public IntPtr Item => _pointer;

	public int Size { get; }

	public static UnmanagedMemoryScope FromUnicodeString(string value) {
		Guard.ArgumentNotNull(value, nameof(value));
		var size = checked((value.Length + 1) * sizeof(char));
		return new UnmanagedMemoryScope(Marshal.StringToHGlobalUni(value), size);
	}

	protected override void OnScopeEnd() {
		Marshal.FreeHGlobal(_pointer);
		_pointer = IntPtr.Zero;
	}
}
