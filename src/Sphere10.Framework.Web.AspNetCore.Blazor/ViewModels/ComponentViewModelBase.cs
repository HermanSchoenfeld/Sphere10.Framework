// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

/// <summary>
/// Base class for component view models.
/// </summary>
public abstract class ComponentViewModelBase {
	/// <summary>
	/// Gets or sets the state change delegate
	/// </summary>
	public Action StateHasChangedDelegate { get; set; }

	/// <summary>Schedules view-model updates on the owning component's renderer.</summary>
	public Func<Action, Task> InvokeAsyncDelegate { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the view model is initialized.
	/// Set to true by calling <see cref="InitAsync"/>
	/// </summary>
	public bool IsInitialized { get; protected set; }

	/// <summary>
	/// Initialize the view model.
	/// </summary>
	/// <returns> a task.</returns>
	public async Task InitAsync() {
		IsInitialized = false;
		await InitCoreAsync();
		IsInitialized = true;
	}

	/// <summary>Runs an update through the renderer when attached to a component.</summary>
	protected Task InvokeAsync(Action action) {
		if (InvokeAsyncDelegate != null)
			return InvokeAsyncDelegate(action);
		action();
		return Task.CompletedTask;
	}

	/// <summary>Override to provide custom initialization logic.</summary>
	protected virtual Task InitCoreAsync() => Task.CompletedTask;
}