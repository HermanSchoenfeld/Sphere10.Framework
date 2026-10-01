// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components;

/// <summary>Connects a component to its view model and marshals notifications onto the renderer.</summary>
public abstract class ComponentWithViewModel<TViewModel> : ComponentBase, IDisposable where TViewModel : ComponentViewModelBase {
	private bool _disposed;
	private Task _initializationTask;

	[Inject]
	public TViewModel ViewModel { get; set; }

	protected bool IsViewModelInitializationStarted => _initializationTask != null;

	public virtual void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		if (ViewModel != null) {
			ViewModel.StateHasChangedDelegate = null;
			ViewModel.InvokeAsyncDelegate = null;
		}
		if (ViewModel is IDisposable disposable)
			disposable.Dispose();
	}

	protected override void OnInitialized() {
		Guard.Ensure(ViewModel != null, "View model has not been injected successfully.");
		ViewModel.InvokeAsyncDelegate = DispatchAsync;
		ViewModel.StateHasChangedDelegate = () => _ = DispatchAsync(StateHasChanged);
		base.OnInitialized();
	}

	protected override Task OnParametersSetAsync() {
		// Derived components copy their auto-property parameters before initialization reads them.
		_initializationTask ??= ViewModel.InitAsync();
		return _initializationTask;
	}

	private Task DispatchAsync(Action action) {
		if (_disposed)
			return Task.CompletedTask;
		return InvokeAsync(() => {
			if (!_disposed)
				action();
		});
	}
}
