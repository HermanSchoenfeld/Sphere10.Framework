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
/// A <see cref="SaveableBase"/> abstraction that implements its synchronous members using the sync-over-async pattern: each starts its
/// asynchronous counterpart on a worker and waits for it to complete.
/// </summary>
public abstract class AsyncSaveableBase : SaveableBase {

	protected override void SaveInternal() => Task.ExecuteSyncOverAsync(() => SaveInternalAsync(CancellationToken.None));

	protected sealed override void OnSaving() => Task.ExecuteSyncOverAsync(OnSavingAsync);

	protected sealed override void OnSaved() => Task.ExecuteSyncOverAsync(OnSavedAsync);
}
