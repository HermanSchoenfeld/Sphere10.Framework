// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

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
	private int _screenModeSelectionVersion;
	private string _routeError;
	private bool _synchronizeLocation;
	private bool _disposed;
	private readonly string _navigationId = "sphere10-navigation-" + Guid.NewGuid().ToString("N");
	private ElementReference _navigationToggle;
	private bool _navigationOpen;
	private bool _focusNavigationToggle;

	[Inject] public IBlazorApplication Application { get; set; }

	[Inject] public IBlazorApplicationScreenHost ScreenHost { get; set; }

	[Inject] public NavigationManager Navigation { get; set; }

	[Parameter] public bool ShowScreenModeSelector { get; set; }

	[Parameter] public string Title { get; set; } = "Application";

	[Parameter] public string NavigationPath { get; set; } = "application";

	[Parameter] public string BlockId { get; set; }

	[Parameter] public string ScreenId { get; set; }

	[Parameter] public Guid? InstanceId { get; set; }

	/// <summary>Additional branding or content beside the application title.</summary>
	[Parameter] public RenderFragment Header { get; set; }

	/// <summary>Tools beside the command buttons, such as search, theme selection and identity controls.</summary>
	[Parameter] public RenderFragment ToolBarContent { get; set; }

	/// <summary>Brand and endpoint controls above the active block's navigation groups.</summary>
	[Parameter] public RenderFragment SidebarHeader { get; set; }

	[Parameter] public RenderFragment Sidebar { get; set; }

	[Parameter] public RenderFragment Footer { get; set; }

	[Parameter] public RenderFragment EmptyContent { get; set; }

	protected override void OnInitialized() {
		Application.Changed += OnHostChanged;
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

		if (string.IsNullOrEmpty(request.BlockId) && string.IsNullOrEmpty(request.ScreenId) && !request.InstanceId.HasValue) {
			try {
				await ScreenHost.InitializeAsync(token);
				if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion)
					_synchronizeLocation = true;
			} catch (OperationCanceledException) when (token.IsCancellationRequested) {
				// A newer route superseded startup.
			}
			return;
		}

		var block = string.IsNullOrEmpty(request.BlockId)
			? ScreenHost.ActiveBlock ?? ScreenHost.Blocks.FirstOrDefault()
			: ScreenHost.Blocks.FirstOrDefault(candidate => candidate.Id == request.BlockId);
		try {
			if (block == null) {
				await ScreenHost.InitializeAsync(token);
				if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion && !string.IsNullOrEmpty(request.BlockId))
					_routeError = "The requested application block was not found.";
				return;
			}
			var existing = ScreenHost.Screens.FirstOrDefault(session =>
				session.Id == request.InstanceId && session.Block.Id == block.Id && session.MenuItem.Id == request.ScreenId);
			if (existing != null) {
				await ScreenHost.ShowScreenAsync(existing.Id, token);
			} else if (!string.IsNullOrEmpty(request.ScreenId)) {
				if (!block.Menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>().Any(item => item.Id == request.ScreenId)
					&& !(request.ScreenId == BlazorApplicationBlockSnapshot.DefaultScreenItemId && block.DefaultScreen != null)) {
					await ScreenHost.InitializeAsync(token);
					if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion)
						_routeError = "The requested screen was not found.";
					return;
				}
				await ScreenHost.ActivateScreenAsync(block.Id, request.ScreenId, token);
			} else {
				await ScreenHost.ActivateBlockAsync(block.Id, token);
			}
			await ScreenHost.InitializeAsync(token);
			if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion)
				_synchronizeLocation = true;
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			// A newer browser navigation superseded this request, or the shell was disposed.
		}
	}
	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (_synchronizeLocation) {
			_synchronizeLocation = false;
			SynchronizeLocation(true);
		}
		if (_focusNavigationToggle && !_disposed) {
			_focusNavigationToggle = false;
			await _navigationToggle.FocusAsync();
		}
	}

	public void Dispose() {
		_disposed = true;
		_lifetimeCancellation.Cancel();
		_lifetimeCancellation.Dispose();
		CancelRouteRequest();
		Application.Changed -= OnHostChanged;
	}

	private void ToggleNavigation() => _navigationOpen = !_navigationOpen;

	private void CloseNavigation() {
		_navigationOpen = false;
		_focusNavigationToggle = true;
	}

	private void OnShellKeyDown(KeyboardEventArgs args) {
		if (_navigationOpen && args.Key == "Escape")
			CloseNavigation();
	}

	private async Task SelectBlockAsync(IBlazorApplicationBlock block) {
		if (_disposed)
			return;
		CancelRouteRequest();
		var token = _lifetimeCancellation.Token;
		try {
			await ScreenHost.SelectBlockAsync(block.Id, token);
			if (!_disposed)
				_routeError = null;
		} catch (OperationCanceledException) when (token.IsCancellationRequested) {
			// Closing the shell cancels pending navigation selection.
		}
	}

	private Task ExecuteMenuItemAsync(IBlazorApplicationMenuItem item) {
		var block = ScreenHost.ActiveBlock;
		return block == null ? Task.CompletedTask : RunInteractionAsync(token => ScreenHost.ExecuteMenuItemAsync(block.Id, item.Id, token), false);
	}

	private IBlazorApplicationBlock CommandBlock => ScreenHost.ActiveScreen?.Block ?? ScreenHost.ActiveBlock;

	private IBlazorApplicationMenu[] MergedMenus => Tools.UI.MergeMenus<IBlazorApplicationMenu, IBlazorApplicationMenuItem>(
		(menu, items) => new BlazorApplicationMenu { Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = items },
		Application.Menus, CommandBlock?.Menus ?? Array.Empty<IBlazorApplicationMenu>(),
		ScreenHost.ActiveScreen?.Screen?.Menus ?? Array.Empty<IBlazorApplicationMenu>());

	private IBlazorApplicationMenuItem[] MergedToolBarItems => Tools.UI.MergeMenuItems(
		Application.ToolBarItems, CommandBlock?.ToolBarItems ?? Array.Empty<IBlazorApplicationMenuItem>(),
		ScreenHost.ActiveScreen?.Screen?.ToolBarItems ?? Array.Empty<IBlazorApplicationMenuItem>());

	private Task ExecuteCommandAsync(IBlazorApplicationMenuItem item) =>
		RunInteractionAsync(token => ScreenHost.ExecuteMenuItemAsync(item, token), false);

	private Task CloseScreensAsync(Guid[] ids) => RunInteractionAsync(token => ScreenHost.CloseScreensAsync(ids, token), true);

	private async Task ReorderScreenAsync((Guid SessionId, int Index) request) =>
		await ScreenHost.MoveScreenAsync(request.SessionId, request.Index, _lifetimeCancellation.Token);

	private async Task ChangeScreenModeAsync(ChangeEventArgs args) {
		var mode = string.Equals(args.Value?.ToString(), nameof(ScreenMode.SingleView), StringComparison.Ordinal) ? ScreenMode.SingleView : ScreenMode.MultiView;
		await RunInteractionAsync(token => ScreenHost.TrySetScreenModeAsync(mode, token), true);
		_screenModeSelectionVersion++;
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
				_navigationOpen = false;
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
		_lastRequest = (session?.Block.Id ?? ScreenHost.ActiveBlock?.Id, session?.MenuItem.Id, session?.Id);
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
