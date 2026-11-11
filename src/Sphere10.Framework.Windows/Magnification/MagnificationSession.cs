// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Owns the process-level Magnification API lifetime and the full-screen desktop color effect.</summary>
/// <remarks>
/// Create one session per process on the UI thread that will own magnifier windows, and dispose it on that thread.
/// Disposal uninitializes the API, which also removes any full-screen color effect this process applied.
/// </remarks>
public sealed class MagnificationSession : SyncDisposable {
	private readonly int _threadId;
	private bool _disposed;

	public MagnificationSession() {
		_threadId = Environment.CurrentManagedThreadId;
		if (!WinAPI.MAGNIFICATION.MagInitialize())
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not initialize the Magnification API.");
	}

	public bool IsDisposed => _disposed;

	/// <summary>Applies a color transformation to the whole desktop; the identity matrix restores normal colors.</summary>
	public void SetFullscreenColorEffect(ColorMatrix matrix) {
		Guard.ArgumentNotNull(matrix, nameof(matrix));
		EnsureUsable();
		var effect = MagnificationColorEffects.ToNative(matrix);
		if (!WinAPI.MAGNIFICATION.MagSetFullscreenColorEffect(ref effect))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not apply the full-screen color effect. Another magnification application may be active.");
	}

	public ColorMatrix GetFullscreenColorEffect() {
		EnsureUsable();
		if (!WinAPI.MAGNIFICATION.MagGetFullscreenColorEffect(out var effect))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not read the full-screen color effect.");
		return MagnificationColorEffects.ToColorMatrix(effect);
	}

	public void ResetFullscreenColorEffect() => SetFullscreenColorEffect(new ColorMatrix());

	internal void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "The Magnification API must be used on the thread that initialized it.");
	}

	protected override void DisposeInternal() {
		if (_disposed)
			return;
		EnsureUsable();
		_disposed = true;
		if (!WinAPI.MAGNIFICATION.MagUninitialize())
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not uninitialize the Magnification API.");
	}
}
