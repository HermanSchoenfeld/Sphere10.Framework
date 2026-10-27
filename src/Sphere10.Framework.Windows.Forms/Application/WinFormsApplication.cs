// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Adapts the running native form without duplicating block or screen ownership.</summary>
public class WinFormsApplication : AsyncApplicationBase, IWinFormsApplication {
	private readonly BlockMainForm _mainForm;
	private readonly IWinFormsApplicationBlock[] _configuredBlocks;
	private readonly ApplicationPluginRegistration<IWinFormsApplicationPlugin>[] _loadedPluginRegistrations;
	private readonly IWinFormsApplicationPlugin[] _loadedPlugins;
	private readonly int _uiThread;
	private bool _blocksInitialized;

	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks)
		: this(mainForm, blocks, Array.Empty<IWinFormsApplicationPlugin>()) {
	}

	/// <summary>Adapts blocks and plugins that the caller has already loaded; this constructor does not invoke plugin lifecycle methods.</summary>
	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks, IEnumerable<IWinFormsApplicationPlugin> loadedPlugins)
		: this(mainForm, blocks, Tools.UI.ValidatePlugins(loadedPlugins).Cast<IWinFormsApplicationPlugin>()
			.Select(plugin => new ApplicationPluginRegistration<IWinFormsApplicationPlugin>(plugin, plugin.Blocks))) {
	}

	internal WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks,
		IEnumerable<ApplicationPluginRegistration<IWinFormsApplicationPlugin>> loadedPluginRegistrations, IServiceProvider services = null)
		: base(services) {
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
	}

	public IBlockManager BlockManager => _mainForm;

	public override IWinFormsApplicationScreenHost ScreenHost => _mainForm.ScreenHost;

	public override IWinFormsApplicationPlugin[] LoadedPlugins => Tools.Array.Clone(_loadedPlugins);

	public override IWinFormsApplicationBlock[] Blocks => ScreenHost.Blocks;

	public override bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

	public override IWinFormsApplicationMenu[] Menus => base.Menus.Cast<IWinFormsApplicationMenu>().ToArray();

	public override IWinFormsApplicationMenuItem[] ToolBarItems => base.ToolBarItems.Cast<IWinFormsApplicationMenuItem>().ToArray();

	internal Func<Task> ShutdownAsync { get; set; }

	public override void Initialize() {
		EnsureCanInitializeSynchronously();
		WaitOnUIThread(InitializeAsync());
	}

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

	public override Task FinalizeAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (Environment.CurrentManagedThreadId == _uiThread || _mainForm.IsDisposed && HasExited)
			return base.FinalizeAsync(cancellationToken);
		return FinalizeOnUIThreadAsync(cancellationToken);
	}

	public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureExitIsNotReentrant();
		var exit = ExitAsync(force, cancellationToken);
		WaitOnUIThread(exit);
		// Sync-over-async pattern
		return exit.GetAwaiter().GetResult();
	}

	public override Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (HasExited || Environment.CurrentManagedThreadId == _uiThread)
			return base.ExitAsync(force, cancellationToken);
		return ExitOnUIThreadAsync(force, cancellationToken);
	}

	public override void SetMenus(IEnumerable<IApplicationMenu> menus) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu is IWinFormsApplicationMenu), nameof(menus), "WinForms menu definitions are required.");
		_mainForm.SetApplicationCommands(definitions.Cast<IWinFormsApplicationMenu>().ToArray(), ToolBarItems);
		base.SetMenus(definitions);
	}

	public override void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item is IWinFormsApplicationMenuItem), nameof(items), "WinForms menu item definitions are required.");
		_mainForm.SetApplicationCommands(Menus, definitions.Cast<IWinFormsApplicationMenuItem>().ToArray());
		base.SetToolBarItems(definitions);
	}

	protected override void FreeManagedResources() {
		EnsureCanDisposeSynchronously();
		WaitOnUIThread(FreeManagedResourcesAsync().AsTask());
	}

	protected override ValueTask FreeManagedResourcesAsync() =>
		new(Environment.CurrentManagedThreadId == _uiThread || IsDisposed || _mainForm.IsDisposed && HasExited ? DisposeOnUIThreadAsync() : DisposeFromWorkerAsync());

	protected override async Task<bool> OnInitializeAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		if (!_blocksInitialized) {
			await _mainForm.ScreenHost.LoadStartupScreenSettingsAsync(cancellationToken);
			var ordered = Tools.UI.OrderApplicationBlocks(_loadedPluginRegistrations,
				_configuredBlocks.Concat(_mainForm.RegisteredBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance));
			_blocksInitialized = _mainForm.RegisterBlocks(ordered.Cast<IWinFormsApplicationBlock>());
		}
		return _blocksInitialized;
	}

	protected override async Task OnFinalizeAsync(CancellationToken cancellationToken) {
		await _mainForm.ScreenHost.CompleteStartupScreenWritesAsync();
		await base.OnFinalizeAsync(cancellationToken);
	}

	protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) => _mainForm.ConfirmApplicationExitAsync(cancellationToken);

	protected override async Task<bool> CanExitAsync(CancellationToken cancellationToken) =>
		await base.CanExitAsync(cancellationToken) && _mainForm.CanApplicationExit();

	protected override async Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var result = await _mainForm.CloseFromApplicationAsync(force, ShutdownAsync ?? (() => FinalizeAsync()));
		if (result.ShutdownError != null)
			Tools.Exceptions.ExecuteIgnoringException(() => Logger.Exception(result.ShutdownError));
		return result.Closed;
	}

	private async Task<bool> ExitOnUIThreadAsync(bool force, CancellationToken cancellationToken) {
		Task<bool> exit = null;
		await _mainForm.InvokeAsyncEx(token => {
			exit = base.ExitAsync(force, token);
			return Task.CompletedTask;
		}, cancellationToken).ConfigureAwait(false);
		// Closing the main window ends its message loop; completion must not need another UI continuation.
		return await exit.ConfigureAwait(false);
	}

	private async Task InitializeOnUIThreadAsync(CancellationToken cancellationToken) {
		Task initialization = null;
		await _mainForm.InvokeAsyncEx(token => {
			initialization = base.InitializeAsync(token);
			return Task.CompletedTask;
		}, cancellationToken).ConfigureAwait(false);
		await initialization.ConfigureAwait(false);
	}

	private async Task FinalizeOnUIThreadAsync(CancellationToken cancellationToken) {
		Task finalization = null;
		await _mainForm.InvokeAsyncEx(token => {
			finalization = base.FinalizeAsync(token);
			return Task.CompletedTask;
		}, cancellationToken).ConfigureAwait(false);
		await finalization.ConfigureAwait(false);
	}

	private async Task DisposeFromWorkerAsync() {
		Task disposal = null;
		await _mainForm.InvokeAsyncEx(_ => {
			disposal = DisposeOnUIThreadAsync();
			return Task.CompletedTask;
		}).ConfigureAwait(false);
		await disposal.ConfigureAwait(false);
	}

	private async Task DisposeOnUIThreadAsync() {
		using var detach = Tools.Scope.ExecuteOnDispose(() => {
			if (IsDisposed)
				DetachMainForm();
		});
		await base.FreeManagedResourcesAsync();
	}

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

	private void DetachMainForm() {
		ShutdownAsync = null;
		if (ReferenceEquals(_mainForm.Application, this))
			_mainForm.Application = null;
		_mainForm.ApplicationChanged -= OnChanged;
	}

}
