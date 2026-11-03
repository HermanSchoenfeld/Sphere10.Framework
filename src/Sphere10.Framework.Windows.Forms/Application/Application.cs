// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Adapts the running native form without duplicating block or screen ownership.</summary>
public class WinFormsApplication : AsyncApplicationBase, IWinFormsApplication {
	private readonly BlockMainForm _mainForm;

	private readonly IWinFormsApplicationBlock[] _configuredBlocks;

	private readonly ApplicationPluginRegistration<IWinFormsApplicationPlugin>[] _loadedPluginRegistrations;

	private readonly IWinFormsApplicationPlugin[] _loadedPlugins;

	private readonly int _uiThread;

	private IWinFormsApplicationMenu[] _menus = Array.Empty<IWinFormsApplicationMenu>();

	private IWinFormsApplicationMenuItem[] _toolBarItems = Array.Empty<IWinFormsApplicationMenuItem>();

	private IWinFormsApplicationMenu[] _cachedMenus;

	private IWinFormsApplicationMenuItem[] _cachedToolBarItems;

	private bool _blocksInitialized;

	/// <summary>Adapts the existing native main form and its configured owners.</summary>
	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks)
		: this(mainForm, blocks, Array.Empty<IWinFormsApplicationPlugin>()) {
	}

	/// <summary>Adapts blocks and plugins that the caller has already loaded; this constructor does not invoke plugin lifecycle methods.</summary>
	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks, IEnumerable<IWinFormsApplicationPlugin> loadedPlugins,
		string settingsKey = null)
		: this(mainForm, blocks, Tools.UI.ValidatePlugins(loadedPlugins).Cast<IWinFormsApplicationPlugin>()
			.Select(plugin => new ApplicationPluginRegistration<IWinFormsApplicationPlugin>(plugin, plugin.Blocks)), null, settingsKey) {
	}

	// Adapts the existing native main form and its configured owners.
	internal WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks,
		IEnumerable<ApplicationPluginRegistration<IWinFormsApplicationPlugin>> loadedPluginRegistrations, IServiceProvider services = null,
		string settingsKey = null)
		: base(services, settingsKey ?? services?.GetKeyedService<string>(nameof(WinFormsApplicationSettingsExtensions.AddApplicationSettings))) {
		Guard.ArgumentNotNull(mainForm, nameof(mainForm));
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		Guard.ArgumentNotNull(loadedPluginRegistrations, nameof(loadedPluginRegistrations));
		_mainForm = mainForm;
		_uiThread = Environment.CurrentManagedThreadId;
		_loadedPluginRegistrations = Tools.UI.ValidatePluginRegistrations(loadedPluginRegistrations);
		_loadedPlugins = _loadedPluginRegistrations.Select(registration => registration.Plugin).ToArray();
		var contributedBlocks = _loadedPluginRegistrations.SelectMany(registration => registration.Blocks).Cast<IWinFormsApplicationBlock>();
		_configuredBlocks = blocks.Concat(contributedBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
		Guard.Argument(_configuredBlocks.All(block => block != null), nameof(blocks), "Block definitions cannot be null.");
		foreach (var registration in _loadedPluginRegistrations)
			foreach (var block in registration.Blocks)
				Guard.Argument(block.Source == null || ReferenceEquals(block.Source, registration.Plugin), nameof(loadedPluginRegistrations), "The block already has another source plugin.");
		foreach (var registration in _loadedPluginRegistrations)
			foreach (var block in registration.Blocks)
				block.Source = registration.Plugin;
		ObserveScreenHost();
		_mainForm.Application = this;
		_mainForm.ApplicationChanged += OnChanged;
		_mainForm.CaptureApplicationCommands();
	}

	public IBlockManager BlockManager {
		get {
			return _mainForm;
		}
	}

	public override IWinFormsApplicationScreenHost ScreenHost {
		get {
			return _mainForm.ScreenHost;
		}
	}

	public override IWinFormsApplicationPlugin[] LoadedPlugins {
		get {
			return Tools.Array.Clone(_loadedPlugins);
		}
	}

	public override IWinFormsApplicationBlock[] Blocks {
		get {
			return ScreenHost.Blocks;
		}
	}

	public override bool HasUnsavedChanges {
		get {
			return ScreenHost.HasUnsavedChanges;
		}
	}

	/// <summary>Activates this application's fixed commands, including its actual main form's native commands.</summary>
	public virtual IWinFormsApplicationMenu[] Menus {
		get {
			return Tools.Array.Clone(_cachedMenus ??= GetMenus());
		}
	}

	/// <summary>Activates the application's fixed toolbar command collection.</summary>
	public virtual IWinFormsApplicationMenuItem[] ToolBarItems {
		get {
			return Tools.Array.Clone(_cachedToolBarItems ??= GetToolBarItems());
		}
	}

	/// <summary>The actual provider captured by the application's existing lifecycle owner.</summary>
	internal IServiceProvider ApplicationServices {
		get {
			return Services;
		}
	}

	// The main form already displays its designer controls; only these additional definitions need generated views.
	internal IWinFormsApplicationMenu[] AdditionalMenus {
		get {
			_ = Menus;
			return Tools.Array.Clone(_menus);
		}
	}

	internal IWinFormsApplicationMenuItem[] AdditionalToolBarItems {
		get {
			_ = ToolBarItems;
			return Tools.Array.Clone(_toolBarItems);
		}
	}

	internal Func<Task> ShutdownAsync { get; set; }

	/// <summary>Creates the application command definitions once after configuration.</summary>
	protected virtual IWinFormsApplicationMenu[] GetMenus() {
		return _mainForm.Menus.Concat(_menus).ToArray();
	}

	/// <summary>Creates toolbar definitions while retaining the main form's original native items.</summary>
	protected virtual IWinFormsApplicationMenuItem[] GetToolBarItems() {
		return _mainForm.ToolBarItems.Concat(_toolBarItems).ToArray();
	}

	/// <summary>Runs application initialization on its owning UI thread.</summary>
	public override void Initialize() {
		EnsureCanInitializeSynchronously();
		WaitOnUIThread(InitializeAsync());
	}

	/// <summary>Initializes configured owners through the existing asynchronous lifecycle.</summary>
	public override Task InitializeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (Environment.CurrentManagedThreadId == _uiThread)
			return base.InitializeAsync(cancellationToken);
		return InitializeOnUIThreadAsync(cancellationToken);
	}

#pragma warning disable CS0465 // Implements the existing synchronous application lifecycle contract.
	public override void Finalize() {
		EnsureCanFinalizeSynchronously();
		WaitOnUIThread(FinalizeAsync());
	}

#pragma warning restore CS0465

	/// <summary>Finalizes the application on its owning UI thread.</summary>
	public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (Environment.CurrentManagedThreadId == _uiThread || _mainForm.IsDisposed && HasExited)
			return base.FinalizeAsync(cancellationToken);
		return FinalizeOnUIThreadAsync(cancellationToken);
	}

	/// <summary>Runs the existing native exit and guard sequence to completion.</summary>
	public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureExitIsNotReentrant();
		var exit = ExitAsync(force, cancellationToken);
		WaitOnUIThread(exit);
		// Sync-over-async pattern
		return exit.GetAwaiter().GetResult();
	}

	/// <summary>Requests native exit through the existing asynchronous lifecycle.</summary>
	public override Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (HasExited || Environment.CurrentManagedThreadId == _uiThread)
			return base.ExitAsync(force, cancellationToken);
		return ExitOnUIThreadAsync(force, cancellationToken);
	}

	/// <summary>Configures additional application menus before command activation; definitions retain their existing owner.</summary>
	public virtual void SetMenus(IEnumerable<IApplicationMenu> menus) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(menus, nameof(menus));
		EnsureCommandsNotActivated();
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu is IWinFormsApplicationMenu), nameof(menus), "WinForms menu definitions are required.");
		var formMenus = _mainForm.Menus;
		var nativeMenus = definitions.Cast<IWinFormsApplicationMenu>().Where(menu => !formMenus.Contains(menu, ReferenceEqualityComparer.Instance)).ToArray();
		ValidateCommands(nativeMenus.SelectMany(menu => menu.Items).ToArray(), nameof(menus));
		_menus = nativeMenus;
		OnChanged();
	}

	/// <summary>Configures additional application toolbar commands before activation.</summary>
	public virtual void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(items, nameof(items));
		EnsureCommandsNotActivated();
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item is IWinFormsApplicationMenuItem), nameof(items), "WinForms menu item definitions are required.");
		var formItems = _mainForm.ToolBarItems;
		var nativeItems = definitions.Cast<IWinFormsApplicationMenuItem>().Where(item => !formItems.Contains(item, ReferenceEqualityComparer.Instance)).ToArray();
		ValidateCommands(nativeItems, nameof(items));
		_toolBarItems = nativeItems;
		OnChanged();
	}

	/// <summary>Completes native cleanup using the established synchronous UI pump.</summary>
	protected override void DisposeInternal() {
		EnsureCanDisposeSynchronously();
		WaitOnUIThread(DisposeInternalAsync().AsTask());
	}

	/// <summary>Releases application resources on the owning UI thread.</summary>
	protected override ValueTask DisposeInternalAsync() {
		return new(Environment.CurrentManagedThreadId == _uiThread || IsDisposed || _mainForm.IsDisposed && HasExited ? DisposeOnUIThreadAsync() : DisposeFromWorkerAsync());
	}

	/// <summary>Restores actual settings before registering blocks and activating screens.</summary>
	protected override async Task<bool> OnInitializeAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		if (!_blocksInitialized) {
			// Load actual owner settings before registration creates permanent or default screens.
			await InitializeSettingsAsync(cancellationToken);
			await _mainForm.ScreenHost.LoadStartupScreenSettingsAsync(cancellationToken);
			var ordered = Tools.UI.OrderApplicationBlocks(_loadedPluginRegistrations,
				_configuredBlocks.Concat(_mainForm.RegisteredBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance));
			_blocksInitialized = _mainForm.RegisterBlocks(ordered.Cast<IWinFormsApplicationBlock>());
		}
		return _blocksInitialized;
	}

	/// <summary>Saves retained native screen settings before application finalizers run.</summary>
	protected override async Task OnFinalizeAsync(CancellationToken cancellationToken) {
		await _mainForm.ScreenHost.CompleteStartupScreenWritesAsync();
		await _mainForm.ScreenHost.SaveScreenSettingsAsync(_mainForm.ScreenHost.Screens, cancellationToken);
		await base.OnFinalizeAsync(cancellationToken);
	}

	/// <summary>Delegates exit confirmation to the actual main form.</summary>
	protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) {
		return _mainForm.ConfirmApplicationExitAsync(cancellationToken);
	}

	/// <summary>Combines application lifecycle checks with native form exit guards.</summary>
	protected override async Task<bool> CanExitAsync(CancellationToken cancellationToken) {
		return await base.CanExitAsync(cancellationToken) && _mainForm.CanApplicationExit();
	}

	/// <summary>Closes the actual main form after its approved shutdown completes.</summary>
	protected override async Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var result = await _mainForm.CloseFromApplicationAsync(force, ShutdownAsync ?? (() => FinalizeAsync()));
		if (result.ShutdownError != null)
			Tools.Exceptions.ExecuteIgnoringException(() => Logger.Exception(result.ShutdownError));
		return result.Closed;
	}

	// Marshals an exit request without requiring a continuation after the main message loop ends.
	private async Task<bool> ExitOnUIThreadAsync(bool force, CancellationToken cancellationToken) {
		Task<bool> exit = null;
		await _mainForm.InvokeAsyncEx(token => {
			exit = base.ExitAsync(force, token);
			return Task.CompletedTask;
		}, cancellationToken).ConfigureAwait(false);
		// Closing the main window ends its message loop; completion must not need another UI continuation.
		return await exit.ConfigureAwait(false);
	}

	// Starts application initialization on the captured native UI thread.
	private async Task InitializeOnUIThreadAsync(CancellationToken cancellationToken) {
		Task initialization = null;
		await _mainForm.InvokeAsyncEx(token => {
			initialization = base.InitializeAsync(token);
			return Task.CompletedTask;
		}, cancellationToken).ConfigureAwait(false);
		await initialization.ConfigureAwait(false);
	}

	// Starts application finalization on the captured native UI thread.
	private async Task FinalizeOnUIThreadAsync(CancellationToken cancellationToken) {
		Task finalization = null;
		await _mainForm.InvokeAsyncEx(token => {
			finalization = base.FinalizeAsync(token);
			return Task.CompletedTask;
		}, cancellationToken).ConfigureAwait(false);
		await finalization.ConfigureAwait(false);
	}

	// Dispatches owned native disposal from a worker to the UI thread.
	private async Task DisposeFromWorkerAsync() {
		Task disposal = null;
		await _mainForm.InvokeAsyncEx(_ => {
			disposal = DisposeOnUIThreadAsync();
			return Task.CompletedTask;
		}).ConfigureAwait(false);
		await disposal.ConfigureAwait(false);
	}

	// Releases the existing application lifecycle and main-form subscriptions.
	private async Task DisposeOnUIThreadAsync() {
		using var detach = Tools.Scope.ExecuteOnDispose(() => {
			if (IsDisposed)
				DetachMainForm();
		});
		await base.DisposeInternalAsync();
	}

	// Pumps native messages until an established synchronous lifecycle call completes.
	private void WaitOnUIThread(Task operation) {
		if (Environment.CurrentManagedThreadId == _uiThread)
			while (!operation.IsCompleted) {
				System.Windows.Forms.Application.DoEvents();
				if (!operation.IsCompleted)
					Thread.Sleep(1);
			}
		// Sync-over-async pattern
		operation.GetAwaiter().GetResult();
	}

	// Removes borrowed main-form callbacks without changing its command ownership.
	private void DetachMainForm() {
		ShutdownAsync = null;
		if (ReferenceEquals(_mainForm.Application, this))
			_mainForm.Application = null;
		_mainForm.ApplicationChanged -= OnChanged;
	}

	// Restore the existing application's actual plugin, block and screen settings without a parallel catalog.
	private async Task InitializeSettingsAsync(CancellationToken cancellationToken) {
		var blocks = _configuredBlocks.Concat(_mainForm.RegisteredBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
		foreach (var plugin in _loadedPlugins.Concat(blocks.Select(block => block.Source).OfType<IWinFormsApplicationPlugin>())
			.Distinct<IWinFormsApplicationPlugin>(ReferenceEqualityComparer.Instance))
			plugin.PluginSettings = await LoadSettingsAsync(plugin.PluginSettings, plugin.PluginSettings?.ID, cancellationToken);
		foreach (var block in blocks) {
			block.BlockSettings = await LoadSettingsAsync(block.BlockSettings, block.BlockSettings?.ID, cancellationToken);
			foreach (var definition in block.ScreenDefinitions)
				if (definition.Settings != null)
					definition.Settings = await LoadSettingsAsync(definition.Settings, Tools.UI.GetScreenSettingsId(SettingsKey, block, definition), cancellationToken);
		}
	}

	// Preserve supplied defaults and provider ownership; an optional damaged record must not prevent normal startup.
	private async Task<SettingsObject> LoadSettingsAsync(SettingsObject settings, object id, CancellationToken cancellationToken) {
		if (settings == null)
			return null;
		settings.ID = id;
		var provider = settings.Provider ?? Settings;
		settings.Provider = provider;
		if (provider == null)
			return settings;
		string backup = null;
		try {
			if (await provider.ContainsSettingAsync(settings.GetType(), id, cancellationToken)) {
				backup = SettingsObjectJsonSerializer.SerializeValues(settings);
				var loaded = await provider.LoadSettingAsync(settings, cancellationToken);
				Guard.Ensure(loaded != null && settings.GetType().IsInstanceOfType(loaded), "Stored settings have the wrong declared type.");
				// Cached providers may return an earlier instance; retain the actual instance supplied by this owner.
				if (!ReferenceEquals(loaded, settings))
					SettingsObjectJsonSerializer.PopulateValues(SettingsObjectJsonSerializer.SerializeValues(loaded), settings);
				cancellationToken.ThrowIfCancellationRequested();
				settings.Validate().ThrowOnFailure();
				Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
			}
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			if (backup != null)
				SettingsObjectJsonSerializer.PopulateValues(backup, settings);
			throw;
		} catch (Exception error) {
			if (backup != null)
				SettingsObjectJsonSerializer.PopulateValues(backup, settings);
			Logger.Exception(error);
		} finally {
			// Population restores values; the owner retains its original record identity and outer provider.
			settings.ID = id;
			settings.Provider = provider;
		}
		return settings;
	}

	/// <summary>Uses the application's existing user provider, including manually hosted native applications.</summary>
	protected override ISettingsProvider GetSettingsProvider() {
		return WinFormsApplicationSettingsExtensions.ResolveSettingsProvider(Services);
	}

	// Commands are configuration; an activated owner has a stable collection for all window presentations.
	private void EnsureCommandsNotActivated() {
		Guard.Ensure(_cachedMenus == null && _cachedToolBarItems == null, "Command membership is read-only after activation.");
	}

	// Native views can execute actions and separators; navigation commands retain their canonical block registration.
	private static void ValidateCommands(IWinFormsApplicationMenuItem[] items, string argumentName) {
		Guard.Argument(items.All(item => item is IWinFormsLinkMenuItem or IApplicationMenuSeparator), argumentName,
			"Application menus and toolbars require native commands or separators.");
		Guard.Argument(items.OfType<IWinFormsScreenMenuItem>().All(item => item.Parent?.Parent != null), argumentName,
			"Application screen commands must belong to a block menu.");
	}
}
