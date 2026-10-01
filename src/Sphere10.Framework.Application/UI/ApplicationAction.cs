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

namespace Sphere10.Framework.Application.UI;

/// <summary>Executes a synchronous or asynchronous menu action independently of a UI toolkit.</summary>
public class ApplicationAction {
	public Action Action { get; set; }

	public Func<IServiceProvider, CancellationToken, Task> AsyncAction { get; set; }

	public void Execute() {
		Guard.Ensure(AsyncAction == null, "Use ExecuteAsync for an asynchronous action.");
		Guard.Ensure(Action != null, "The action has no callback.");
		Action();
	}

	public async Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(services, nameof(services));
		cancellationToken.ThrowIfCancellationRequested();
		if (AsyncAction != null)
			await AsyncAction(services, cancellationToken);
		else
			Execute();
	}
}
