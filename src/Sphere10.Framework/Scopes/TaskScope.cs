// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public class TaskScope : AsyncScope {
	private readonly Func<Task> _scopeFinalizer;

	public TaskScope(Func<Task> scopeFinalizer) {
		_scopeFinalizer = scopeFinalizer;
	}

	protected override async ValueTask OnScopeEndAsync() {
		if (_scopeFinalizer != null)
			await _scopeFinalizer();
	}
}


public class TaskScope<T> : TaskScope, IScope<T> {

	public TaskScope(T item, Func<Task> scopeFinalizer)
		: base(scopeFinalizer) {
		Item = item;
	}

	public T Item { get; }
}

