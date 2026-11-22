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

/// <summary>
/// Base class for displaying a control beneath an expanded VisualGrid row.
/// </summary>
/// <remarks>
/// Return a derived instance from <see cref="VisualGrid.CreateDetail"/> to display an owned control beneath an expanded row.
/// The grid applies each prepared context on the control's creating UI thread. The adapter releases a rejected disposable
/// context at once and disposes the control when the detail closes. A replaced context and the last context are released
/// after the nested grid's page read and detail cleanup have drained, and asynchronous disposal waits for those releases.
/// </remarks>
public abstract class VisualGridDetailBase : WinFormsSyncDisposableBase {
	private readonly int _ownerThread = Environment.CurrentManagedThreadId;
	private Task _disposalCompletion = Task.CompletedTask;
	private object _prepared;

	protected VisualGridDetailBase(Control content) {
		Guard.ArgumentNotNull(content, nameof(content));
		Content = content;
	}

	public Control Content { get; }

	public virtual VisualGrid Grid => Content as VisualGrid;

	// A nested grid may still be reading a page or draining canceled child queries against a context's source.
	private Task NestedWorkCompletion
		=> Grid is { } grid ? Task.WhenAll(grid.DetailsDisposalCompletion, grid.RefreshCompletion.IgnoringExceptions()) : Task.CompletedTask;

	/// <summary>Publishes detached values on the creating UI thread. This method must not perform native or database collection.</summary>
	public async Task ApplyAsync(object prepared, CancellationToken cancellationToken = default) {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "Publish row details on their creating UI thread.");
		var original = _prepared;
		var adopted = false;
		try {
			ObjectDisposedException.ThrowIf(Content.IsDisposed || Content.Disposing, this);
			cancellationToken.ThrowIfCancellationRequested();
			await ApplyCoreAsync(prepared, cancellationToken);
			ObjectDisposedException.ThrowIf(Content.IsDisposed || Content.Disposing, this);
			_prepared = prepared;
			adopted = true;
			// Rows removed by this update can leave child queries running against the replaced context's source.
			if (!ReferenceEquals(original, prepared))
				_disposalCompletion = ReleasePreparedAfterAsync(Task.WhenAll(_disposalCompletion, NestedWorkCompletion), original);
		} finally {
			if (!adopted && !ReferenceEquals(original, prepared))
				await DisposePreparedAsync(prepared);
		}
	}

	public virtual object CaptureSelectedIdentity() => null;

	public virtual void RestoreSelectedIdentity(object identity) {
	}

	public override async ValueTask DisposeAsync() {
		await base.DisposeAsync().ConfigureAwait(false);
		// A prior synchronous disposal may still have nested preparation to drain.
		await _disposalCompletion.ConfigureAwait(false);
	}

	protected override void DisposeInternal() {
		Content.Dispose();
		// Capture the grid's pending work on its dispatcher before awaiting it elsewhere.
		_disposalCompletion = ReleasePreparedAfterAsync(Task.WhenAll(_disposalCompletion, NestedWorkCompletion), _prepared);
		_prepared = null;
	}

	protected abstract Task ApplyCoreAsync(object prepared, CancellationToken cancellationToken);

	private static async Task ReleasePreparedAfterAsync(Task nestedWork, object prepared) {
		// Earlier releases and the nested grid's pending work finish before this context's source is released.
		try {
			await nestedWork.ConfigureAwait(false);
		} finally {
			await DisposePreparedAsync(prepared).ConfigureAwait(false);
		}
	}

	private static async Task DisposePreparedAsync(object prepared) {
		if (prepared is IAsyncDisposable asynchronous)
			await asynchronous.DisposeAsync().ConfigureAwait(false);
		else if (prepared is IDisposable disposable)
			disposable.Dispose();
	}
}
