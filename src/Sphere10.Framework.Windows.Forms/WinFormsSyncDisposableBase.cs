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

/// <summary>Keeps synchronous cleanup inline and queues asynchronous cleanup to the creating Windows Forms dispatcher.</summary>
public abstract class WinFormsSyncDisposableBase : SyncDisposable {
	private readonly WindowsFormsSynchronizationContext _synchronizationContext;

	protected WinFormsSyncDisposableBase() {
		_synchronizationContext = SynchronizationContext.Current as WindowsFormsSynchronizationContext ?? new WindowsFormsSynchronizationContext();
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		// The thread's dispatcher outlives an owned modal dialog; synchronous disposal never pumps its queued callbacks.
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_synchronizationContext.Post(static state => {
			var (owner, completion) = ((WinFormsSyncDisposableBase, TaskCompletionSource))state;
			if (Tools.Exceptions.Execute(owner.FreeManagedResources, completion.SetException))
				completion.SetResult();
		}, (this, completion));
		return new ValueTask(completion.Task);
	}
}
