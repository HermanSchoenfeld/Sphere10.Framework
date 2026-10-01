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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorActionMenuItem : BlazorApplicationMenuItem {
	private readonly ApplicationAction _action = new();

	public Action Action {
		get => _action.Action;
		init => _action.Action = value;
	}

	/// <summary>Resolves scoped services at execution time; registration must not capture scoped instances.</summary>
	public Func<IServiceProvider, CancellationToken, Task> AsyncAction {
		get => _action.AsyncAction;
		init => _action.AsyncAction = value;
	}

	public async Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken = default) {
		await _action.ExecuteAsync(services, cancellationToken);
		NotifySelect();
	}
}
