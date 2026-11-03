// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework;

// This class shows how to use a disposable resource.
// The resource is first initialized and passed to
// the constructor, but it could also be
// initialized in the constructor.
// The lifetime of the resource does not 
// exceed the lifetime of this instance.
// This type does not need a finalizer because it does not
// directly create a native resource like a file handle
// or memory in the unmanaged heap.


/// <summary>Coordinates synchronous or asynchronous managed cleanup with once-only unmanaged cleanup.</summary>
public abstract class Disposable : IAsyncDisposable, IDisposable {
	private bool _disposed;

	/// <summary>Releases managed and unmanaged resources, then suppresses finalization.</summary>
	public virtual void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this); // Use SupressFinalize in case a subclass of this type implements a finalizer.
	}

	/// <summary>Awaits managed cleanup before releasing unmanaged resources and suppressing finalization.</summary>
	public virtual async ValueTask DisposeAsync() {
		if (_disposed)
			return;

		await DisposeInternalAsync().ConfigureAwait(false);

		Dispose(disposing: false);

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
		GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
	}

	/// <summary>Performs the remaining cleanup and marks disposal complete only after cleanup succeeds.</summary>
	protected virtual void Dispose(bool disposing) {
		if (_disposed)
			return;

		// If you need thread safety, use a lock around these 
		// operations, as well as in your methods that use the resource.

		if (disposing)
			DisposeInternal();

		FreeUnmanagedResources();

		_disposed = true;
	}

	/// <summary>Releases managed resources asynchronously; direction-specific bases provide the opposite adapter.</summary>
	protected abstract ValueTask DisposeInternalAsync();

	/// <summary>Releases managed resources synchronously; direction-specific bases provide the opposite adapter.</summary>
	protected abstract void DisposeInternal();

	/// <summary>Releases native resources after managed cleanup has completed.</summary>
	protected virtual void FreeUnmanagedResources() {
	}

}

