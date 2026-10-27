// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Adapts asynchronous managed cleanup to synchronous disposal outside the caller's synchronization context.</summary>
public abstract class AsyncDisposable : Disposable {

	// Sync-over-async pattern
	protected sealed override void FreeManagedResources() => Task.Run(() => FreeManagedResourcesAsync().AsTask()).GetAwaiter().GetResult();
}

