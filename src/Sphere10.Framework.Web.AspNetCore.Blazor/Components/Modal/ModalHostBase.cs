// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Modal;

/// <summary>Renders modal content and manages an isolated JavaScript module for its host element.</summary>
public abstract class ModalHostBase<TComponent, TResult> : ComponentBase, IAsyncDisposable where TComponent : ComponentBase {
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private Task<IJSObjectReference> _moduleTask;
	private RenderFragment _activeContent;
	private bool _showing;
	private bool _disposed;

	protected ElementReference ModalElement;

	[Parameter]
	public string Id { get; set; } = "modal";

	[Parameter]
	public RenderFragment Content { get; set; }

	[Inject]
	private IJSRuntime JsRuntime { get; set; }

	protected RenderFragment DisplayedContent => _activeContent ?? Content;

	/// <summary>The normal dismissal result returned when the renderer removes this host.</summary>
	protected virtual TResult CanceledResult => default;

	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		_lifetimeCancellation.Cancel();
		_lifetimeCancellation.Dispose();
		if (_moduleTask == null)
			return;
		try {
			var module = await _moduleTask;
			await module.DisposeAsync();
		} catch (OperationCanceledException) {
			// Disposing the host cancels a pending JavaScript import.
		} catch (JSDisconnectedException) {
			// The browser observer removes the backdrop when the host leaves the DOM.
		}
	}

	protected async Task<TResult> ShowCoreAsync<T>(IDictionary<string, object> parameters) where T : TComponent {
		Guard.Ensure(!_disposed, "The modal host has been disposed.");
		Guard.Ensure(!_showing, "The modal host is already showing a dialog.");
		_showing = true;
		var cancellationToken = _lifetimeCancellation.Token;
		IJSObjectReference module = null;
		await using var contentCleanup = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			_activeContent = null;
			_showing = false;
			if (!_disposed)
				await InvokeAsync(StateHasChanged);
		});
		await using var visibilityCleanup = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			try {
				if (module != null && !_disposed)
					await module.InvokeVoidAsync("hide", ModalElement);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				// Teardown can interrupt an in-flight hide request.
			} catch (JSDisconnectedException) {
				// A disconnected circuit cannot perform browser cleanup.
			}
		});
		var rendered = new TaskCompletionSource<TComponent>(TaskCreationOptions.RunContinuationsAsynchronously);
		async Task<bool> CloseAsync() {
			if (_disposed)
				return true;
			try {
				return await RequestCloseAsync(await rendered.Task.WaitAsync(cancellationToken));
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				return true;
			}
		}
		_activeContent = builder => {
			builder.OpenComponent<CascadingValue<Func<Task<bool>>>>(0);
			builder.AddAttribute(1, nameof(CascadingValue<Func<Task<bool>>>.Name), "CloseModal");
			builder.AddAttribute(2, nameof(CascadingValue<Func<Task<bool>>>.Value), (Func<Task<bool>>)CloseAsync);
			builder.AddAttribute(3, nameof(CascadingValue<Func<Task<bool>>>.ChildContent), (RenderFragment)(content => {
				content.OpenComponent<T>(0);
				content.AddMultipleAttributes(1, parameters);
				content.AddComponentReferenceCapture(2, component => rendered.TrySetResult((TComponent)component));
				content.CloseComponent();
			}));
			builder.CloseComponent();
		};
		try {
			await InvokeAsync(StateHasChanged);
			var component = await rendered.Task.WaitAsync(cancellationToken);
			await WaitUntilRenderedAsync(component).WaitAsync(cancellationToken);
			_moduleTask ??= JsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, "./_content/Sphere10.Framework.Web.AspNetCore.Blazor/js/modal.js").AsTask();
			module = await _moduleTask.WaitAsync(cancellationToken);
			await module.InvokeVoidAsync("show", cancellationToken, ModalElement);
			return await GetResultAsync(component).WaitAsync(cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// Host removal is ordinary dismissal, not a failed Blazor event callback.
			return CanceledResult;
		}
	}

	protected abstract Task WaitUntilRenderedAsync(TComponent component);

	protected abstract Task<TResult> GetResultAsync(TComponent component);

	protected abstract Task<bool> RequestCloseAsync(TComponent component);
}
