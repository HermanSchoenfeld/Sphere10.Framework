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

/// <summary>Owns one open-page lifetime.</summary>
/// <remarks>Keep one concrete owner and dispose it once.</remarks>
public readonly struct PageScope<TItem> : IScope {
	private readonly CachedItem<IMemoryPage<TItem>> _cachedItem;
	private readonly DynamicStreamPage<TItem> _streamPage;
	private readonly Action _close;
	private readonly bool _previousCanPurge;

	internal PageScope(CachedItem<IMemoryPage<TItem>> cachedItem, bool previousCanPurge) {
		Guard.ArgumentNotNull(cachedItem, nameof(cachedItem));
		_cachedItem = cachedItem;
		_streamPage = null;
		_close = null;
		_previousCanPurge = previousCanPurge;
	}

	internal PageScope(DynamicStreamPage<TItem> streamPage) {
		Guard.ArgumentNotNull(streamPage, nameof(streamPage));
		_cachedItem = null;
		_streamPage = streamPage;
		_close = null;
		_previousCanPurge = false;
	}

	/// <summary>Creates a scope for a custom provider's synchronous page cleanup.</summary>
	public PageScope(Action close) {
		Guard.ArgumentNotNull(close, nameof(close));
		_cachedItem = null;
		_streamPage = null;
		_close = close;
		_previousCanPurge = false;
	}

	public void Dispose() {
		if (_cachedItem is not null)
			_cachedItem.CanPurge = _previousCanPurge;
		else if (_streamPage is not null)
			_streamPage.Close();
		else
			_close();
	}

	public ValueTask DisposeAsync() => new(Task.Run(Dispose));
}
