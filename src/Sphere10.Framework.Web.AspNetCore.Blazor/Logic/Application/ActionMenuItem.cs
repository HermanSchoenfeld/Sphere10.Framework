// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public class ActionMenuItem : ApplicationMenuItem {
	/// <summary>Compatibility callback for synchronous actions.</summary>
	public Action Action { get; init; }

	/// <summary>Executes against the current circuit's provider; registration must not capture scoped service instances.</summary>
	public Func<IServiceProvider, CancellationToken, Task> AsyncAction { get; init; }

	public async Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(services, nameof(services));
		cancellationToken.ThrowIfCancellationRequested();
		if (AsyncAction != null)
			await AsyncAction(services, cancellationToken);
		else {
			Guard.Ensure(Action != null, "The action has no callback.");
			Action();
		}
		NotifySelect();
	}
}
