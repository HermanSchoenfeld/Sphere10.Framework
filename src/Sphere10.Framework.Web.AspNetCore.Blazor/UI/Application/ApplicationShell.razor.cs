// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;
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
	private string _synchronizedLocation;
	private bool _synchronizeLocation;
	private bool _replaceSynchronizedLocation = true;
	private int _navigationOperations;
	private bool _disposed;
	private readonly string _navigationId = "sphere10-navigation-" + Guid.NewGuid().ToString("N");
	private ElementReference _navigationToggle;
	private bool _navigationOpen;
	private bool _focusNavigationToggle;
	private IDisposable _controllerScope;

	[Inject] public IBlazorApplication Application { get; set; }

	[Inject] public IBlazorApplicationScreenHost ScreenHost { get; set; }

	private IUserInterfaceServices UserInterfaceServices => ScreenHost.UserInterfaceServices;

	private IBlazorApplicationBlock ActiveBlock => ScreenHost.ActiveBlock;
	private IBlazorApplicationBlock[] Blocks => Application.Blocks;

	[Inject] public NavigationManager Navigation { get; set; }

	[Parameter] public bool ShowScreenModeSelector { get; set; }

	/// <summary>Activates a block's default screen when its dock button is selected; false only browses that block's navigation.</summary>
	/// <remarks>Activation uses screen leave guards and synchronizes browser history. A rejected transition preserves the current selection.</remarks>
	[Parameter] public bool ActivateBlockOnSelection { get; set; }

	[Parameter] public string Title { get; set; } = "Application";

	[Parameter] public string NavigationPath { get; set; } = "application";

	/// <summary>Optional selected command in the active block's sidebar; null uses the active screen registration.</summary>
	/// <remarks>Use this for guarded subviews within a retained screen. Update it only after a subview transition succeeds.</remarks>
	[Parameter] public string NavigationActiveItemId { get; set; }

	/// <summary>Optionally maps the currently selected session (or null) to a path within the application base, without query or fragment.</summary>
	/// <remarks>Evaluated when synchronizing selection; custom paths omit the default block, screen and instance query parameters.</remarks>
	[Parameter] public Func<BlazorApplicationScreenSession, string> NavigationUriFactory { get; set; }

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
		_controllerScope = ((IBlazorUserInterfaceServices)UserInterfaceServices).AttachController(this, action => InvokeAsync(action));
	}

	protected override async Task OnParametersSetAsync() {
		ResolveNavigationPath(NavigationPath, nameof(NavigationPath));
		var request = (BlockId, ScreenId, InstanceId);
		if (_lastRequest == request)
			return;
		_navigationOperations++;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _navigationOperations--);
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
			? ActiveBlock ?? Blocks.FirstOrDefault()
			: Blocks.FirstOrDefault(candidate => candidate.Id == request.BlockId);
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
				if (!block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems).OfType<BlazorScreenMenuItem>().Any(item => item.Id == request.ScreenId)
					&& !BlazorApplicationBlockSnapshot.IsDefinitionScreenId(block, request.ScreenId)
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
			SynchronizeLocation(_replaceSynchronizedLocation);
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
		_controllerScope?.Dispose();
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
		if (ActivateBlockOnSelection) {
			await RunInteractionAsync(async token => {
				var session = await ScreenHost.ActivateBlockAsync(block.Id, token);
				return session != null || ScreenHost.ActiveScreen == null && ActiveBlock?.Id == block.Id;
			}, false);
			return;
		}
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

	private Task ExecuteMenuItemAsync(IBlazorApplicationMenuItem item) => RunInteractionAsync(token => UserInterfaceServices.ExecuteMenuItemAsync(item, token), false);

	private IBlazorApplicationMenu[] MergedMenus => Tools.UI.MergeApplicationMenus<IBlazorApplicationMenu, IBlazorApplicationMenuItem>(
		(menu, items) => new BlazorApplicationMenu(ownsItems: false) { Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = items },
		menu => menu.Id, item => item.Id, Tools.UI.GetCommandLayers(Application));

	private IBlazorApplicationMenuItem[] MergedToolBarItems => Tools.UI.MergeApplicationToolBarItems<IBlazorApplicationMenuItem>(
		item => item.Id, Tools.UI.GetCommandLayers(Application));

	private string GetCommandRenderKey(IBlazorApplicationMenuItem item) {
		var applicationItems = Application.Menus.SelectMany(menu => menu.Items).Concat(Application.ToolBarItems);
		return (ContainsCommand(applicationItems, item) ? "application:" : "context:") + item.Id;
	}

	private static bool ContainsCommand(IEnumerable<IBlazorApplicationMenuItem> items, IBlazorApplicationMenuItem command) =>
		items.Any(item => ReferenceEquals(item, command) || item is BlazorApplicationMenuGroup group && ContainsCommand(group.Items, command));

	/// <summary>Executes a custom navigation command through the shell's guarded interaction and browser-history path.</summary>
	/// <remarks>Await this from custom sidebar controls. Successful commands close the navigation drawer and synchronize the active screen URL.</remarks>
	public Task ExecuteCommandAsync(IBlazorApplicationMenuItem item) =>
		RunInteractionAsync(token => UserInterfaceServices.ExecuteMenuItemAsync(item, token), false);

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
		_navigationOperations++;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _navigationOperations--);
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
		var target = NavigationUriFactory == null
			? ResolveNavigationPath(NavigationPath, nameof(NavigationPath)).AbsoluteUri
			: ResolveNavigationPath(NavigationUriFactory(session), nameof(NavigationUriFactory)).AbsoluteUri;
		if (NavigationUriFactory == null) {
			if (session != null)
				target += $"?block={Uri.EscapeDataString(session.Block.Id)}&screen={Uri.EscapeDataString(session.MenuItem.Id)}&instance={session.Id:D}";
			else if (ActiveBlock != null)
				target += $"?block={Uri.EscapeDataString(ActiveBlock.Id)}";
		}
		if (!string.Equals(Navigation.Uri, target, StringComparison.Ordinal)) {
			// This location reflects a transition already accepted by the host; do not run its leave guards twice.
			_synchronizedLocation = target;
			Navigation.NavigateTo(target, new NavigationOptions { ReplaceHistoryEntry = replace });
		}
	}

	private async Task OnBeforeInternalNavigationAsync(LocationChangingContext context) {
		var destination = Navigation.ToAbsoluteUri(context.TargetLocation);
		if (NavigationUriFactory != null) {
			if (string.Equals(destination.AbsoluteUri, _synchronizedLocation, StringComparison.Ordinal))
				return;
		} else {
			var shell = Navigation.ToAbsoluteUri(NavigationPath);
			if (string.Equals(destination.GetLeftPart(UriPartial.Path), shell.GetLeftPart(UriPartial.Path), StringComparison.OrdinalIgnoreCase))
				return;
		}
		if (!await ScreenHost.CanNavigateAsync(context.CancellationToken))
			context.PreventNavigation();
	}

	private Uri ResolveNavigationPath(string path, string parameterName) {
		Guard.ArgumentNotNullOrEmpty(path, parameterName);
		var target = Navigation.ToAbsoluteUri(path);
		Guard.Argument(new Uri(Navigation.BaseUri).IsBaseOf(target) && string.IsNullOrEmpty(target.Query) && string.IsNullOrEmpty(target.Fragment),
			parameterName, "The shell path must be within the application's base URI and have no query or fragment.");
		return target;
	}

	private void CancelRouteRequest() {
		_navigationVersion++;
		_synchronizeLocation = false;
		_replaceSynchronizedLocation = true;
		_routeCancellation?.Cancel();
		_routeCancellation?.Dispose();
		_routeCancellation = null;
	}
	private void OnHostChanged() {
		if (!_disposed)
			_ = InvokeAsync(() => {
				if (_disposed)
					return;
				// Programmatic activation also needs an entity URL. Route requests and shell interactions own their own synchronization.
				if (NavigationUriFactory != null && _navigationOperations == 0 && _routeError == null) {
					_synchronizeLocation = true;
					_replaceSynchronizedLocation = false;
				}
				StateHasChanged();
			});
	}
}
