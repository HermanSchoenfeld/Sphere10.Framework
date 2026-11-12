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

namespace Sphere10.Framework;

/// <summary>A data source whose complete data can be replaced by a fresh acquisition while the current data stays readable.</summary>
public interface IRefreshableDataSource<TItem> : ILoadableDataSource<TItem>, IDisposable, IAsyncDisposable {
	/// <summary>Acquires and publishes fresh data on the calling thread.</summary>
	void Refresh();

	/// <summary>Acquires and publishes fresh data on a worker. Overlapping callers share one acquisition; cancellation ends only this caller's wait.</summary>
	Task RefreshAsync(CancellationToken cancellationToken = default);

	/// <summary>The asynchronous refresh in progress, or a completed task. Reading it starts nothing.</summary>
	Task Completion { get; }

	bool IsDisposed { get; }
}
