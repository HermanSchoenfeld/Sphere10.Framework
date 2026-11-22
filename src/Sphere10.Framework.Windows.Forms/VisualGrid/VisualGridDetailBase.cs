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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns one general native detail surface; nested grids are one adapter rather than a requirement.</summary>
public abstract class VisualGridDetail : IDisposable, IAsyncDisposable {
	private readonly int _ownerThread = Environment.CurrentManagedThreadId;
	private bool _disposed;
	private object _prepared;

	protected VisualGridDetail(Control content) {
		Guard.ArgumentNotNull(content, nameof(content));
		Content = content;
	}

	public Control Content { get; }

	public virtual VisualGrid Grid => Content as VisualGrid;

	/// <summary>Publishes detached values on the creating UI thread. This method must not perform native or database collection.</summary>
	public async Task ApplyAsync(object prepared, CancellationToken cancellationToken = default) {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Publish row details on their creating UI thread.");
		var original = _prepared;
		var adopted = false;
		using var reject = Tools.Scope.ExecuteOnDispose(() => {
			if (!adopted && !ReferenceEquals(original, prepared) && prepared is IDisposable disposable)
				disposable.Dispose();
		});
		ObjectDisposedException.ThrowIf(_disposed, this);
		cancellationToken.ThrowIfCancellationRequested();
		await ApplyCoreAsync(prepared, cancellationToken);
		ObjectDisposedException.ThrowIf(_disposed, this);
		_prepared = prepared;
		adopted = true;
		if (!ReferenceEquals(original, prepared) && original is IDisposable previous)
			previous.Dispose();
	}

	public virtual object CaptureSelectedIdentity() => null;

	public virtual void RestoreSelectedIdentity(object identity) {
	}

	public virtual void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		Content.Dispose();
		if (_prepared is IDisposable disposable)
			disposable.Dispose();
		_prepared = null;
	}

	public virtual async ValueTask DisposeAsync() {
		Dispose();
		if (Grid != null)
			await Grid.DetailsDisposalCompletion.ConfigureAwait(false);
	}

	protected abstract Task ApplyCoreAsync(object prepared, CancellationToken cancellationToken);
}
