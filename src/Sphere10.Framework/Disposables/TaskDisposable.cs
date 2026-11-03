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

/// <summary>Awaits a supplied asynchronous action when disposed.</summary>
public sealed class TaskDisposable : AsyncDisposable {
	private readonly Func<Task> _scopeFinalizer;

	/// <summary>Retains the asynchronous action to run during managed cleanup.</summary>
	public TaskDisposable(Func<Task> scopeFinalizer) {
		_scopeFinalizer = scopeFinalizer;
	}

	/// <summary>Awaits the retained action when one was supplied.</summary>
	protected override async ValueTask DisposeInternalAsync() {
		if (_scopeFinalizer != null)
			await _scopeFinalizer();
	}
}

