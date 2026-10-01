// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

/// <summary>
/// Renders a circuit-scoped application workspace. Pass query parameters from the containing routable page
/// to support bookmarks and browser history. Component instances stay mounted until their screens close.
/// </summary>
public partial class ApplicationShell : ComponentBase, IDisposable {
	private (string BlockId, string ScreenId, Guid? InstanceId)? _lastRequest;
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private CancellationTokenSource _routeCancellation;
	private int _navigationVersion;
	private string _routeError;
	private bool _synchronizeLocation;
	private bool _disposed;

	[Inject] public IApplicationScreenHost ScreenHost { get; set; }

	[Inject] public NavigationManager Navigation { get; set; }

	[Parameter] public string Title { get; set; } = "Application";

	[Parameter] public string NavigationPath { get; set; } = "application";

	[Parameter] public string BlockId { get; set; }

	[Parameter] public string ScreenId { get; set; }

	[Parameter] public Guid? InstanceId { get; set; }

	[Parameter] public RenderFragment Header { get; set; }

	[Parameter] public RenderFragment Sidebar { get; set; }

	[Parameter] public RenderFragment Footer { get; set; }

	[Parameter] public RenderFragment EmptyContent { get; set; }

	protected override void OnInitialized() {
		ScreenHost.Changed += OnHostChanged;
	}

	protected override async Task OnParametersSetAsync() {
		Guard.ArgumentNotNullOrEmpty(NavigationPath, nameof(NavigationPath));
		var shellUri = Navigation.ToAbsoluteUri(NavigationPath);
		Guard.Argument(new Uri(Navigation.BaseUri).IsBaseOf(shellUri) && string.IsNullOrEmpty(shellUri.Query) && string.IsNullOrEmpty(shellUri.Fragment),
			nameof(NavigationPath), "The shell path must be within the application's base URI and have no query or fragment.");
		var request = (BlockId, ScreenId, InstanceId);
		if (_lastRequest == request)
			return;
		CancelRouteRequest();
		_lastRequest = request;
		_routeError = null;
		_routeCancellation = new CancellationTokenSource();
		var token = _routeCancellation.Token;
		var version = _navigationVersion;

		var block = string.IsNullOrEmpty(request.BlockId)
			? ScreenHost.ActiveBlock ?? ScreenHost.Blocks.FirstOrDefault()
			: ScreenHost.Blocks.FirstOrDefault(candidate => candidate.Id == request.BlockId);
		if (block == null) {
			if (!string.IsNullOrEmpty(request.BlockId))
				_routeError = "The requested application block was not found.";
			return;
		}

		try {
			var existing = ScreenHost.OpenScreens.FirstOrDefault(session =>
				session.Id == request.InstanceId && session.Block.Id == block.Id && session.MenuItem.Id == request.ScreenId);
			if (existing != null) {
				await ScreenHost.ShowScreenAsync(existing.Id, token);
			} else if (!string.IsNullOrEmpty(request.ScreenId)) {
				if (!block.Menus.SelectMany(menu => menu.Items).OfType<ShowScreenMenuItem>().Any(item => item.Id == request.ScreenId)
					&& !(request.ScreenId == ApplicationBlockSnapshot.DefaultScreenItemId && block.DefaultScreen != null)) {
					_routeError = "The requested screen was not found.";
					return;
				}
				await ScreenHost.ActivateScreenAsync(block.Id, request.ScreenId, token);
			} else {
				await ScreenHost.ActivateBlockAsync(block.Id, token);
			}
			if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion)
				_synchronizeLocation = true;
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			// A newer browser navigation superseded this request, or the shell was disposed.
		}
	}
	protected override Task OnAfterRenderAsync(bool firstRender) {
		if (_synchronizeLocation) {
			_synchronizeLocation = false;
			SynchronizeLocation(true);
		}
		return Task.CompletedTask;
	}

	public void Dispose() {
		_disposed = true;
		_lifetimeCancellation.Cancel();
		_lifetimeCancellation.Dispose();
		CancelRouteRequest();
		ScreenHost.Changed -= OnHostChanged;
	}

	private Task ActivateBlockAsync(IApplicationBlock block) => RunInteractionAsync(async token => {
		var session = await ScreenHost.ActivateBlockAsync(block.Id, token);
		return session != null || ScreenHost.ActiveBlock?.Id == block.Id;
	}, false);

	private Task ExecuteMenuItemAsync(IApplicationMenuItem item) {
		var block = ScreenHost.ActiveBlock;
		return block == null ? Task.CompletedTask : RunInteractionAsync(token => ScreenHost.ExecuteMenuItemAsync(block.Id, item.Id, token), false);
	}

	private Task ShowScreenAsync(Guid id) => RunInteractionAsync(token => ScreenHost.ShowScreenAsync(id, token), false);

	private Task CloseScreenAsync(Guid id) => RunInteractionAsync(token => ScreenHost.CloseScreenAsync(id, token), true);

	private async Task RunInteractionAsync(Func<CancellationToken, Task<bool>> action, bool replace) {
		if (_disposed)
			return;
		CancelRouteRequest();
		var version = _navigationVersion;
		var token = _lifetimeCancellation.Token;
		try {
			if (await action(token) && !_disposed && version == _navigationVersion) {
				_routeError = null;
				SynchronizeLocation(replace);
			}
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			// Disposing the workspace cancels pending menu actions without reopening its route.
		}
	}
	private void SynchronizeLocation(bool replace) {
		if (_disposed)
			return;
		var session = ScreenHost.ActiveScreen;
		var target = Navigation.ToAbsoluteUri(NavigationPath).AbsoluteUri;
		if (session != null)
			target += $"?block={Uri.EscapeDataString(session.Block.Id)}&screen={Uri.EscapeDataString(session.MenuItem.Id)}&instance={session.Id:D}";
		else if (ScreenHost.ActiveBlock != null)
			target += $"?block={Uri.EscapeDataString(ScreenHost.ActiveBlock.Id)}";
		_lastRequest = (ScreenHost.ActiveBlock?.Id, session?.MenuItem.Id, session?.Id);
		if (!string.Equals(Navigation.Uri, target, StringComparison.Ordinal))
			Navigation.NavigateTo(target, new NavigationOptions { ReplaceHistoryEntry = replace });
	}

	private async Task OnBeforeInternalNavigationAsync(LocationChangingContext context) {
		var destination = Navigation.ToAbsoluteUri(context.TargetLocation);
		var shell = Navigation.ToAbsoluteUri(NavigationPath);
		if (string.Equals(destination.GetLeftPart(UriPartial.Path), shell.GetLeftPart(UriPartial.Path), StringComparison.OrdinalIgnoreCase))
			return;
		if (!await ScreenHost.CanNavigateAsync(context.CancellationToken))
			context.PreventNavigation();
	}

	private void CancelRouteRequest() {
		_navigationVersion++;
		_synchronizeLocation = false;
		_routeCancellation?.Cancel();
		_routeCancellation?.Dispose();
		_routeCancellation = null;
	}
	private void OnHostChanged() {
		if (!_disposed)
			_ = InvokeAsync(() => { if (!_disposed) StateHasChanged(); });
	}
}
