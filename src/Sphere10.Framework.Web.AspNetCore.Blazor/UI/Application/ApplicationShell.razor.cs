// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

/// <summary>
/// Renders a circuit-scoped application workspace. Pass query parameters from the containing routable page
/// to support bookmarks and browser history. Component instances stay mounted until their screens close.
/// </summary>
public partial class ApplicationShell : ComponentBase, IDisposable, IAsyncDisposable {

	// Identifies one route request without storing presentation state.
	private readonly record struct NavigationRequest(string BlockId, string ScreenId, Guid? InstanceId);

	private NavigationRequest? _lastRequest;
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
	private ActionScope? _controllerScope;
	private IJSObjectReference _legacyStartupModule;
	private string _startupSettingsKey;
	private bool _startupSettingsLoaded;
	private bool _readingStartupSettings;
	private bool _deferredStartup;
	private bool _savingStartupScreen;
	private string _startupSettingsError;
	private Task _startupSettingsLoad = Task.CompletedTask;

	/// <summary>The circuit's application and command owner.</summary>
	[Inject] public IApplication Application { get; set; }

	/// <summary>The host that owns retained screens and navigation.</summary>
	[Inject] public IApplicationScreenHost ScreenHost { get; set; }

	/// <summary>The browser navigation service.</summary>
	[Inject] public NavigationManager Navigation { get; set; }

	/// <summary>Provides browser interop for the legacy preference reader.</summary>
	[Inject] public IJSRuntime JSRuntime { get; set; }

	/// <summary>Shows the switch between tabbed and MDI presentation.</summary>
	[Parameter] public bool ShowScreenModeSelector { get; set; }

	/// <summary>Activates a block's default screen when its dock button is selected; false only browses that block's navigation.</summary>
	/// <remarks>Activation uses screen leave guards and synchronizes browser history. A rejected transition preserves the current selection.</remarks>
	[Parameter] public bool ActivateBlockOnSelection { get; set; }

	/// <summary>The title displayed in the application shell.</summary>
	[Parameter] public string Title { get; set; } = "Application";

	/// <summary>The base route used for synchronized screen navigation.</summary>
	[Parameter] public string NavigationPath { get; set; } = "application";

	/// <summary>Optional stable application-qualified key for this browser's startup screen preference. No key leaves startup unchanged.</summary>
	[Parameter] public string StartupScreenSettingsKey { get; set; }

	/// <summary>Optional selected command in the active block's sidebar; null uses the active screen registration.</summary>
	/// <remarks>Use this for guarded subviews within a retained screen. Update it only after a subview transition succeeds.</remarks>
	[Parameter] public string NavigationActiveItemId { get; set; }

	/// <summary>Optionally maps the currently selected session (or null) to a path within the application base, without query or fragment.</summary>
	/// <remarks>Evaluated when synchronizing selection; custom paths omit the default block, screen and instance query parameters.</remarks>
	[Parameter] public Func<ApplicationScreenSession, string> NavigationUriFactory { get; set; }

	/// <summary>The explicitly requested block route ID.</summary>
	[Parameter] public string BlockId { get; set; }

	/// <summary>The explicitly requested screen route ID.</summary>
	[Parameter] public string ScreenId { get; set; }

	/// <summary>The explicitly requested retained screen instance.</summary>
	[Parameter] public Guid? InstanceId { get; set; }

	/// <summary>Additional branding or content beside the application title.</summary>
	[Parameter] public RenderFragment Header { get; set; }

	/// <summary>Tools beside the command buttons, such as search, theme selection and identity controls.</summary>
	[Parameter] public RenderFragment ToolBarContent { get; set; }

	/// <summary>Brand and endpoint controls above the active block's navigation groups.</summary>
	[Parameter] public RenderFragment SidebarHeader { get; set; }

	/// <summary>Optional custom sidebar content.</summary>
	[Parameter] public RenderFragment Sidebar { get; set; }

	/// <summary>Optional application footer content.</summary>
	[Parameter] public RenderFragment Footer { get; set; }

	/// <summary>Content displayed when no screen is active.</summary>
	[Parameter] public RenderFragment EmptyContent { get; set; }


	// The host's user interaction services.
	private IUserInterfaceServices UserInterfaceServices {
		get {
			return ScreenHost.UserInterfaceServices;
		}
	}

	// The navigation block currently being browsed.
	private IApplicationBlock SelectedBlock {
		get {
			return ScreenHost.SelectedBlock;
		}
	}

	// The blocks exposed by the current application.
	private IApplicationBlock[] Blocks {
		get {
			return Application.Blocks;
		}
	}

	// The toolbar composition for the active command owners.
	private IApplicationMenuItem[] MergedToolBarItems {
		get {
			return ApplicationHelper.MergeApplicationToolBarItems<IApplicationMenuItem>(item => item.Id, GetApplicationCommandLayers());
		}
	}

	/// <summary>Subscribes to retained screen and navigation changes.</summary>
	protected override void OnInitialized() {
		Application.Changed += OnHostChanged;
		ScreenHost.ScreenSelected += OnScreenSelected;
		_controllerScope = ((IBlazorUserInterfaceServices)UserInterfaceServices).AttachController(this, action => InvokeAsync(action));
	}

	/// <summary>Applies route changes through the host's guarded activation path.</summary>
	protected override async Task OnParametersSetAsync() {
		if (Application?.IsExiting == true || Application?.HasExited == true)
			return;

		// Prerender has no browser-owned preference provider. Live circuit startup restores it before any screen factory or query runs.
		if (!Application.IsInitialized && (Application.LoadedPlugins.Any(plugin => plugin.PluginSettings != null)
			|| ScreenHost.AvailableBlocks.Any(block => block.BlockSettings != null || block.ScreenDefinitions.Any(definition => definition.Settings != null))))
			return;
		ResolveNavigationPath(NavigationPath, nameof(NavigationPath));
		var settingsKey = string.IsNullOrWhiteSpace(StartupScreenSettingsKey) ? null : StartupScreenSettingsKey;
		if (_startupSettingsKey != settingsKey) {
			_startupSettingsKey = settingsKey;
			_startupSettingsLoaded = settingsKey == null;
			_startupSettingsError = null;
			ScreenHost.SetStartupScreen(null);
		}
		var request = new NavigationRequest(BlockId, ScreenId, InstanceId);
		if (_lastRequest == request)
			return;
		if (ScreenHost.ActiveScreen == null && string.IsNullOrEmpty(request.ScreenId) && !request.InstanceId.HasValue
			&& request.BlockId == SelectedBlock?.Id && string.Equals(Navigation.Uri, _synchronizedLocation, StringComparison.Ordinal)) {

			// Closing the last tab already committed an empty selection; its own block-only URL must not reopen the block default.
			_lastRequest = request;
			_synchronizedLocation = null;
			return;
		}
		_navigationOperations++;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _navigationOperations--);
		CancelRouteRequest();
		_lastRequest = request;
		_routeError = null;
		_routeCancellation = new CancellationTokenSource();
		var token = _routeCancellation.Token;
		var version = _navigationVersion;
		_deferredStartup = false;

		if (string.IsNullOrEmpty(request.BlockId) && string.IsNullOrEmpty(request.ScreenId) && !request.InstanceId.HasValue) {
			if (_startupSettingsKey != null && !_startupSettingsLoaded) {
				_deferredStartup = true;
				return;
			}
			await Tools.Exceptions.ExecuteCapturingExceptionAsync<OperationCanceledException>(async () => {
				await ScreenHost.InitializeAsync(token);
				if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion)
					_synchronizeLocation = true;
			}, _ => token.IsCancellationRequested);
			return;
		}

		var block = string.IsNullOrEmpty(request.BlockId)
			? SelectedBlock ?? Blocks.FirstOrDefault()
			: Blocks.FirstOrDefault(candidate => candidate.Id == request.BlockId);
		await Tools.Exceptions.ExecuteCapturingExceptionAsync<OperationCanceledException>(async () => {
			if (block == null) {
				await ScreenHost.InitializeAsync(token);
				if (!_disposed && !token.IsCancellationRequested && version == _navigationVersion && !string.IsNullOrEmpty(request.BlockId))
					_routeError = "The requested application block was not found.";
				return;
			}
			var existing = ScreenHost.Screens.FirstOrDefault(session =>
				session.Id == request.InstanceId && session.Block.Id == block.Id && session.MenuItemId == request.ScreenId);
			if (existing != null) {
				await ScreenHost.ShowScreenAsync(existing.Id, token);
			} else if (!string.IsNullOrEmpty(request.ScreenId)) {
				if (!block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems).OfType<BlazorScreenMenuItem>().Any(item => item.Id == request.ScreenId)
					&& !ApplicationBlockSnapshot.IsDefinitionScreenId(block, request.ScreenId)
					&& !(request.ScreenId == ApplicationBlockSnapshot.DefaultScreenItemId && block.DefaultScreen != null)) {
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
		}, capturedException => token.IsCancellationRequested);
	}

	/// <summary>Restores interactive startup preferences before default activation.</summary>
	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (Application?.IsExiting == true || Application?.HasExited == true)
			return;
		if (!_startupSettingsLoaded && !_readingStartupSettings && _startupSettingsKey != null) {
			_startupSettingsLoad = LoadStartupScreenAsync();
			await _startupSettingsLoad;
			if (_disposed)
				return;
			if (!_startupSettingsLoaded && _startupSettingsKey != null) {
				StateHasChanged();
				return;
			}
		}
		if (_synchronizeLocation) {
			_synchronizeLocation = false;
			SynchronizeLocation(_replaceSynchronizedLocation);
		}
		if (_focusNavigationToggle && !_disposed) {
			_focusNavigationToggle = false;
			await _navigationToggle.FocusAsync();
		}
	}

	/// <summary>Cancels pending navigation and removes subscriptions.</summary>
	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		_lifetimeCancellation.Cancel();
		_lifetimeCancellation.Dispose();
		CancelRouteRequest();
		Application.Changed -= OnHostChanged;
		ScreenHost.ScreenSelected -= OnScreenSelected;
		_controllerScope?.Dispose();
	}

	/// <summary>Releases the legacy browser reader after outstanding restoration.</summary>
	public async ValueTask DisposeAsync() {
		Dispose();
		await _startupSettingsLoad;
		if (_legacyStartupModule != null)
			await Tools.Exceptions.ExecuteCapturingExceptionAsync<JSDisconnectedException>(async () => {
				await _legacyStartupModule.DisposeAsync();
			});
	}

	// Restores the typed startup choice, with an existing legacy key as fallback.
	private async Task LoadStartupScreenAsync() {
		_readingStartupSettings = true;
		var key = _startupSettingsKey;
		var superseded = false;
		try {
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				ApplicationStartupScreen identity;
				if (await UserSettings.HasAsync<StartupSettings>(key))
					identity = (await UserSettings.GetAsync<StartupSettings>(key)).StartupScreen;
				else {

					// Keep the original untyped browser key as a read-only migration source.
					_legacyStartupModule ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Application/ApplicationShell.razor.js");
					identity = await _legacyStartupModule.InvokeAsync<ApplicationStartupScreen>("ReadStartupScreen", key);
				}
				if (_disposed || key != _startupSettingsKey) {
					superseded = true;
					return;
				}
				if (!_disposed && key == _startupSettingsKey)
					ScreenHost.SetStartupScreen(identity);
			}, exception => exception is JSException or InvalidOperationException or Newtonsoft.Json.JsonException);
			if (error != null && !_disposed)
				_startupSettingsError = "The startup screen preference could not be read. The application's default screen will be used.";
		} finally {
			_readingStartupSettings = false;
			if (key == _startupSettingsKey)
				_startupSettingsLoaded = true;
		}
		if (superseded || _disposed || Application?.IsExiting == true || Application?.HasExited == true)
			return;
		if (_deferredStartup) {
			_deferredStartup = false;
			try {
				await ScreenHost.InitializeAsync(_lifetimeCancellation.Token);
			} catch (OperationCanceledException) when (_disposed) {
				return;
			}
			_synchronizeLocation = true;
		}
		StateHasChanged();
	}

	// Explains why a screen cannot be remembered as the startup choice.
	private string StartupScreenUnavailableReason(ApplicationScreenSession screen) {
		return _startupSettingsKey == null
			? "Startup preferences are not enabled for this application."
			: !_startupSettingsLoaded ? "Startup preferences are still loading."
			: ScreenHost.GetStartupScreenUnavailableReason(screen);
	}

	/// <summary>Saves a restorable retained screen as this browser's startup choice, retaining the previous choice if storage fails.</summary>
	public async Task MakeStartupScreenAsync(Guid id) {
		var screen = ScreenHost.Screens.FirstOrDefault(candidate => candidate.Id == id);
		if (_disposed || _savingStartupScreen || StartupScreenUnavailableReason(screen) != null)
			return;
		_savingStartupScreen = true;
		var key = _startupSettingsKey;
		var identity = ScreenHost.GetStartupScreenIdentity(screen);
		try {
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				var settings = await UserSettings.GetAsync<StartupSettings>(key);
				settings.StartupScreen = identity;
				await settings.SaveAsync();
				if (!_disposed && key == _startupSettingsKey) {
					ScreenHost.SetStartupScreen(identity);
					_startupSettingsError = null;
				}
			}, exception => exception is JSException or InvalidOperationException or Newtonsoft.Json.JsonException);
			if (error != null && !_disposed)
				_startupSettingsError = "The startup screen preference could not be saved. The previous choice is unchanged.";
		} finally {
			_savingStartupScreen = false;
			if (!_disposed)
				StateHasChanged();
		}
	}

	// Toggles the narrow-layout navigation drawer.
	private void ToggleNavigation() {
		_navigationOpen = !_navigationOpen;
	}

	// Closes the navigation drawer.
	private void CloseNavigation() {
		_navigationOpen = false;
		_focusNavigationToggle = true;
	}

	// Closes navigation when Escape is pressed.
	private void OnShellKeyDown(KeyboardEventArgs args) {
		if (_navigationOpen && args.Key == "Escape")
			CloseNavigation();
	}

	// Browses or activates a block through the shell's interaction path.
	private async Task SelectBlockAsync(IApplicationBlock block) {
		if (_disposed || Application?.IsExiting == true || Application?.HasExited == true)
			return;
		if (ActivateBlockOnSelection) {
			await RunInteractionAsync(async token => {
				var session = await ScreenHost.ActivateBlockAsync(block.Id, token);
				return session != null || ScreenHost.ActiveScreen == null && SelectedBlock?.Id == block.Id;
			}, false);
			return;
		}
		CancelRouteRequest();
		var token = _lifetimeCancellation.Token;
		await Tools.Exceptions.ExecuteCapturingExceptionAsync<OperationCanceledException>(async () => {
			await ScreenHost.SelectBlockAsync(block.Id, token);
			if (!_disposed)
				_routeError = null;
		}, capturedException => token.IsCancellationRequested);
	}

	// Executes a command through guarded navigation.
	private Task ExecuteMenuItemAsync(IApplicationMenuItem item) {
		return RunInteractionAsync(token => UserInterfaceServices.ExecuteMenuItemAsync(item, token), false);
	}

	// Keep existing menu order and reuse a contextual Options heading before adding a heading of our own.
	private IApplicationMenu[] MergedMenus {
		get {
			var menus = ApplicationHelper.MergeApplicationMenus<IApplicationMenu, IApplicationMenuItem>(
				(menu, items) => new ApplicationMenu(ownsItems: false) { Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = items },
				menu => menu.Id, item => item.Id, GetApplicationCommandLayers()).ToList();
			var settings = menus.FirstOrDefault(menu => menu.Id == global::Sphere10.Framework.Web.AspNetCore.Blazor.Application.SettingsMenuId);
			if (settings == null)
				return menus.ToArray();

			menus.Remove(settings);
			var existing = menus.FindIndex(menu => IsMenuHeading(menu, "Options"));
			if (existing >= 0) {
				var menu = menus[existing];
				menus[existing] = new ApplicationMenu(ownsItems: false) {
					Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = menu.Items.Concat(settings.Items).ToArray()
				};
			} else {
				var help = menus.FindIndex(menu => IsMenuHeading(menu, "Help"));
				menus.Insert(help < 0 ? menus.Count : help, settings);
			}
			return menus.ToArray();
		}
	}

	// Menu accelerators and surrounding whitespace do not change a heading's purpose.
	private static bool IsMenuHeading(IApplicationMenu menu, string heading) {
		return string.Equals(menu.Text?.Replace("&", string.Empty).Trim(), heading, StringComparison.OrdinalIgnoreCase);
	}

	// Commands follow the visible screen's owners; sidebar browsing contributes no presentation commands.
	private ApplicationCommandLayer[] GetApplicationCommandLayers() {
		var screen = ScreenHost.ActiveScreen;
		return ApplicationHelper.GetCommandLayers(Application, screen?.Block, screen);
	}

	// Selects a stable render key for a command.
	private string GetCommandRenderKey(IApplicationMenuItem item) {
		var applicationItems = Application.Menus.SelectMany(menu => menu.Items).Concat(Application.ToolBarItems);
		return (ContainsCommand(applicationItems, item) ? "application:" : "context:") + item.Id;
	}

	// Checks whether a command belongs to a visible menu branch.
	private static bool ContainsCommand(IEnumerable<IApplicationMenuItem> items, IApplicationMenuItem command) {
		return items.Any(item => ReferenceEquals(item, command) || item is ApplicationMenuGroup group && ContainsCommand(group.Items, command));
	}

	/// <summary>Executes a custom navigation command through the shell's guarded interaction and browser-history path.</summary>
	/// <remarks>Await this from custom sidebar controls. Successful commands close the navigation drawer and synchronize the active screen URL.</remarks>
	public Task ExecuteCommandAsync(IApplicationMenuItem item) {
		return RunInteractionAsync(token => UserInterfaceServices.ExecuteMenuItemAsync(item, token), false);
	}

	// Accepted batch closure captures preferences after every screen guard has approved removal.
	private Task CloseScreensAsync(Guid[] ids) {
		return RunInteractionAsync(token => CloseWithPreferencesAsync(() => ScreenHost.CloseScreensAsync(ids, token)), true);
	}

	// Updates the host's visual screen order.
	private async Task ReorderScreenAsync((Guid SessionId, int Index) request) {
		if (!_disposed && Application?.IsExiting != true && Application?.HasExited != true)
			await ScreenHost.MoveScreenAsync(request.SessionId, request.Index, _lifetimeCancellation.Token);
	}

	// Changes the host's screen presentation mode.
	private async Task ChangeScreenModeAsync(ChangeEventArgs args) {
		var mode = string.Equals(args.Value?.ToString(), nameof(ScreenMode.SingleView), StringComparison.Ordinal) ? ScreenMode.SingleView : ScreenMode.MultiView;
		await RunInteractionAsync(token => CloseWithPreferencesAsync(() => ScreenHost.TrySetScreenModeAsync(mode, token)), true);
		_screenModeSelectionVersion++;
	}

	// Selects a retained screen through guarded navigation.
	private Task ShowScreenAsync(Guid id) {
		return RunInteractionAsync(token => ScreenHost.ShowScreenAsync(id, token), false);
	}

	// A preference-save failure keeps the retained screen available for correction and retry.
	private Task CloseScreenAsync(Guid id) {
		return RunInteractionAsync(token => CloseWithPreferencesAsync(() => ScreenHost.CloseScreenAsync(id, token)), true);
	}

	// An expected persistence failure preserves the host's live screens and leaves an observable retry path.
	private async Task<bool> CloseWithPreferencesAsync(Func<Task<bool>> close) {
		try {
			return await close();
		} catch (OperationCanceledException) {
			throw;
		} catch (Exception error) {
			_routeError = "The screen could not be closed. " + error.Message;
			Tools.Exceptions.ExecuteIgnoringException(() => Application.Logger.Exception(error));
			return false;
		}
	}

	// Runs a shell action while managing cancellation and navigation state.
	private async Task RunInteractionAsync(Func<CancellationToken, Task<bool>> action, bool replace) {
		if (_disposed || Application?.IsExiting == true || Application?.HasExited == true)
			return;
		_navigationOperations++;
		using var operation = Tools.Scope.ExecuteOnDispose(() => _navigationOperations--);
		CancelRouteRequest();
		var version = _navigationVersion;
		var token = _lifetimeCancellation.Token;
		await Tools.Exceptions.ExecuteCapturingExceptionAsync<OperationCanceledException>(async () => {
			if (await action(token) && !_disposed && version == _navigationVersion) {
				_routeError = null;
				_navigationOpen = false;
				SynchronizeLocation(replace);
			}
		}, capturedException => token.IsCancellationRequested);
	}

	// Updates browser history to describe the active retained screen.
	private void SynchronizeLocation(bool replace) {
		if (_disposed || Application?.IsExiting == true || Application?.HasExited == true)
			return;
		var session = ScreenHost.ActiveScreen;
		var target = NavigationUriFactory == null
			? ResolveNavigationPath(NavigationPath, nameof(NavigationPath)).AbsoluteUri
			: ResolveNavigationPath(NavigationUriFactory(session), nameof(NavigationUriFactory)).AbsoluteUri;
		if (NavigationUriFactory == null) {
			if (session != null)
				target += $"?block={Uri.EscapeDataString(session.Block.Id)}&screen={Uri.EscapeDataString(session.MenuItemId)}&instance={session.Id:D}";
			else if (SelectedBlock != null)
				target += $"?block={Uri.EscapeDataString(SelectedBlock.Id)}";
		}
		if (!string.Equals(Navigation.Uri, target, StringComparison.Ordinal)) {

			// This location reflects a transition already accepted by the host; do not run its leave guards twice.
			_synchronizedLocation = target;
			Navigation.NavigateTo(target, new NavigationOptions { ReplaceHistoryEntry = replace });
		}
	}

	// Runs leave guards before accepting browser navigation.
	private async Task OnBeforeInternalNavigationAsync(LocationChangingContext context) {
		if (Application?.HasExited == true)
			return;
		if (Application?.IsExiting == true) {
			context.PreventNavigation();
			return;
		}
		var destination = Navigation.ToAbsoluteUri(context.TargetLocation);
		if (NavigationUriFactory != null) {
			if (string.Equals(destination.AbsoluteUri, _synchronizedLocation, StringComparison.Ordinal))
				return;
		} else {
			var shell = Navigation.ToAbsoluteUri(NavigationPath);
			if (string.Equals(destination.GetLeftPart(UriPartial.Path), shell.GetLeftPart(UriPartial.Path), StringComparison.OrdinalIgnoreCase))
				return;
		}
		if (!await ScreenHost.CanNavigateAsync(context.CancellationToken)) {
			context.PreventNavigation();
			return;
		}

		// Capture only after leave permission succeeds; disposal and veto callbacks never become save boundaries.
		try {
			foreach (var participant in ScreenHost.Screens.Select(session => session.Screen).OfType<ApplicationScreen>())
				await participant.SaveSettingsAsync(context.CancellationToken);
		} catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested) {
			context.PreventNavigation();
		} catch (Exception error) {
			context.PreventNavigation();
			_routeError = "Preferences could not be saved before leaving. " + error.Message;
			Tools.Exceptions.ExecuteIgnoringException(() => Application.Logger.Exception(error));
			StateHasChanged();
		}
	}

	// Resolves the application's navigation base path.
	private Uri ResolveNavigationPath(string path, string parameterName) {
		Guard.ArgumentNotNullOrEmpty(path, parameterName);
		var target = Navigation.ToAbsoluteUri(path);
		Guard.Argument(new Uri(Navigation.BaseUri).IsBaseOf(target) && string.IsNullOrEmpty(target.Query) && string.IsNullOrEmpty(target.Fragment),
			parameterName, "The shell path must be within the application's base URI and have no query or fragment.");
		return target;
	}

	// Cancels and releases the pending route request.
	private void CancelRouteRequest() {
		_navigationVersion++;
		_synchronizeLocation = false;
		_replaceSynchronizedLocation = true;
		_routeCancellation?.Cancel();
		_routeCancellation?.Dispose();
		_routeCancellation = null;
	}

	// Default registration URLs also follow direct host selection; browsing and dirty notifications do not navigate.
	private void OnScreenSelected(SharedUI.IApplicationScreen screen) {
		if (_disposed || NavigationUriFactory != null || _navigationOperations != 0 || _routeError != null)
			return;
		_ = InvokeAsync(() => {
			if (_disposed)
				return;
			_synchronizeLocation = true;
			_replaceSynchronizedLocation = false;
			StateHasChanged();
		});
	}

	// Schedules command and navigation refresh after host changes.
	private void OnHostChanged() {
		if (!_disposed)
			_ = InvokeAsync(() => {
				if (_disposed)
					return;
				if (Application?.IsExiting == true || Application?.HasExited == true) {
					CancelRouteRequest();
					StateHasChanged();
					return;
				}

				// Programmatic activation also needs an entity URL. Route requests and shell interactions own their own synchronization.
				if (NavigationUriFactory != null && _navigationOperations == 0 && _routeError == null) {
					_synchronizeLocation = true;
					_replaceSynchronizedLocation = false;
				}
				StateHasChanged();
			});
	}
	#region Inner Types

	/// <summary>The shell's startup choice, persisted by the existing user settings provider.</summary>
	public sealed class StartupSettings : SettingsObject {

		/// <summary>The registered screen to restore; null keeps the application's default startup policy.</summary>
		[DefaultValue(null)]
		public ApplicationStartupScreen StartupScreen { get; set; }
	}

	#endregion

}
