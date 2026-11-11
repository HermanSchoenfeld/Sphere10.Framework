// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// A <see cref="LoadableBase"/> abstraction that implements its synchronous members using the sync-over-async pattern: each starts its
/// asynchronous counterpart on a worker and waits for it to complete.
/// </summary>
public abstract class AsyncLoadableBase : LoadableBase {

	protected override void LoadInternal() => Task.ExecuteSyncOverAsync(() => LoadInternalAsync(CancellationToken.None));

	protected sealed override void OnLoading() => Task.ExecuteSyncOverAsync(OnLoadingAsync);

	protected sealed override void OnLoaded() => Task.ExecuteSyncOverAsync(OnLoadedAsync);
}
