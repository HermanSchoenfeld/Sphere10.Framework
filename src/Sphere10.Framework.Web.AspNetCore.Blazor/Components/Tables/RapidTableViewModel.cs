// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Tables;

/// <summary>Maintains a bounded table of streamed items until cancellation or disposal.</summary>
public class RapidTableViewModel<TItem> : ComponentViewModelBase, IDisposable, IAsyncDisposable {
	private readonly List<TItem> _items = new();
	private CancellationTokenSource _streamCancellation;
	private Task _enumeratorTask = Task.CompletedTask;
	private IAsyncEnumerable<TItem> _activeSource;
	private CancellationToken _activeToken;
	private int _sourceVersion;
	private bool _disposed;

	public IAsyncEnumerable<TItem> Source { get; set; }

	public TItem[] Items => _items.ToArray();

	public CancellationToken CancellationToken { get; set; }

	public int ItemLimit { get; set; } = 25;

	public async Task SetSourceAsync(IAsyncEnumerable<TItem> source, CancellationToken cancellationToken = default) {
		Guard.Ensure(!_disposed, "The table has been disposed.");
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentGT(ItemLimit, 0, nameof(ItemLimit));
		while (_items.Count > ItemLimit)
			_items.RemoveAt(0);
		if (ReferenceEquals(source, _activeSource) && cancellationToken == _activeToken)
			return;

		var version = ++_sourceVersion;
		Source = _activeSource = source;
		CancellationToken = _activeToken = cancellationToken;
		var previousCancellation = _streamCancellation;
		previousCancellation?.Cancel();
		await _enumeratorTask;
		previousCancellation?.Dispose();
		if (_disposed || version != _sourceVersion)
			return;

		_items.Clear();
		_streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_enumeratorTask = EnumerateAsync(source, _streamCancellation.Token);
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		_sourceVersion++;
		_streamCancellation?.Cancel();
		_ = ReleaseStreamAsync();
	}

	public async ValueTask DisposeAsync() {
		Dispose();
		await _enumeratorTask;
	}

	protected override Task InitCoreAsync() => SetSourceAsync(Source, CancellationToken);

	private async Task ReleaseStreamAsync() {
		try {
			await _enumeratorTask;
		} finally {
			_streamCancellation?.Dispose();
		}
	}

	private async Task EnumerateAsync(IAsyncEnumerable<TItem> source, CancellationToken cancellationToken) {
		try {
			await foreach (var item in source.WithCancellation(cancellationToken)) {
				cancellationToken.ThrowIfCancellationRequested();
				await InvokeAsync(() => {
					if (_disposed || cancellationToken.IsCancellationRequested)
						return;
					while (_items.Count >= ItemLimit)
						_items.RemoveAt(0);
					_items.Add(item);
					StateHasChangedDelegate?.Invoke();
				});
			}
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// Navigation, source replacement, and caller cancellation stop enumeration.
		}
	}
}
