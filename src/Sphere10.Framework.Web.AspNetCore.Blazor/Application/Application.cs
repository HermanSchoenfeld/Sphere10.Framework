// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Projects a circuit's live host and startup plugin definitions without loading or owning those plugins again.</summary>
public class Application : AsyncApplicationBase, IApplication {
	internal const string SettingsMenuId = "sphere10-screen-options-menu";

	private readonly IBlazorPlugin[] _loadedPlugins;
	private IApplicationMenu[] _menus = Array.Empty<IApplicationMenu>();
	private IApplicationMenuItem[] _toolBarItems = Array.Empty<IApplicationMenuItem>();

	/// <summary>Projects plugins already loaded successfully during startup; construction does not load or unload plugins.</summary>
	public Application(IApplicationScreenHost screenHost, IEnumerable<IBlazorPlugin> loadedPlugins)
		: this(screenHost, CaptureLoadedPlugins(loadedPlugins)) {
	}

	/// <summary>Projects the circuit host and successfully loaded startup plugin definitions.</summary>
	internal Application(IApplicationScreenHost screenHost, IEnumerable<ApplicationPluginRegistration<IBlazorPlugin>> pluginRegistrations, IServiceProvider services = null,
		string settingsKey = null)
		: base(services, settingsKey) {
		Guard.ArgumentNotNull(screenHost, nameof(screenHost));
		var registrations = ApplicationHelper.ValidatePluginRegistrations(pluginRegistrations);
		ScreenHost = screenHost;
		// Startup definitions remain application-lifetime owners; settings-bearing plugin values belong to this circuit.
		_loadedPlugins = registrations.Select(registration => registration.Plugin.PluginSettings == null ? registration.Plugin
			: new BlazorPlugin(registration.Plugin, ScreenHost.AvailableBlocks
				.Where(block => registration.Blocks.Cast<IApplicationBlock>().Any(source => source.Id == block.Id)).ToArray())).ToArray();
		var sources = registrations.SelectMany(registration => registration.Blocks.Cast<IApplicationBlock>()
			.Select(block => new KeyValuePair<string, IBlazorPlugin>(block.Id, registration.Plugin))).ToDictionary(contribution => contribution.Key, contribution => contribution.Value);
		foreach (var block in ScreenHost.AvailableBlocks)
			if (sources.TryGetValue(block.Id, out var plugin))
				Guard.Argument(block.Source == null || ReferenceEquals(block.Source, plugin), nameof(pluginRegistrations), "The block already has another source plugin.");
		foreach (var block in ScreenHost.AvailableBlocks)
			if (sources.TryGetValue(block.Id, out var plugin))
				block.Source = _loadedPlugins.Single(owner => owner.Name == plugin.Name);
		ObserveScreenHost();
	}

	/// <inheritdoc />
	public override IApplicationScreenHost ScreenHost { get; }

	/// <inheritdoc />
	public override IBlazorPlugin[] LoadedPlugins => Tools.Array.Clone(_loadedPlugins);

	/// <inheritdoc />
	public override IApplicationBlock[] Blocks => ScreenHost.Blocks;

	/// <inheritdoc />
	public override bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

	/// <summary>The web menu definitions also exposed through the shared application's command-provider contract.</summary>
	public virtual IApplicationMenu[] Menus => Tools.Array.Clone(_menus);

	/// <summary>The web application's toolbar definitions.</summary>
	public virtual IApplicationMenuItem[] ToolBarItems => Tools.Array.Clone(_toolBarItems);

	/// <summary>Runs the full application initialization and its user tasks with this circuit's facade provider bound.</summary>
	public override async Task InitializeAsync(CancellationToken cancellationToken = default) {
		using var settings = Settings is { } provider ? UserSettings.UseProvider(provider) : null;
		await base.InitializeAsync(cancellationToken);
	}

	/// <summary>Keeps the actual provider bound until application finalization and its user tasks have completed.</summary>
	public override async Task FinalizeAsync(CancellationToken cancellationToken = default) {
		using var settings = Settings is { } provider ? UserSettings.UseProvider(provider) : null;
		await base.FinalizeAsync(cancellationToken);
	}

	/// <summary>Synchronous exit is available outside the renderer; interactive callbacks must await ExitAsync.</summary>
	public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (HasExited)
			return true;
		Guard.Ensure(SynchronizationContext.Current == null, "Blazor renderer callbacks must await ExitAsync instead of blocking synchronous exit.");
		return base.Exit(force, cancellationToken);
	}

	/// <summary>Replaces web menu definitions and refreshes the circuit's presentation.</summary>
	public virtual void SetMenus(IEnumerable<SharedUI.IApplicationMenu> menus) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu is IApplicationMenu), nameof(menus), "Blazor menu definitions are required.");
		_menus = definitions.Cast<IApplicationMenu>().Select(ApplicationBlockSnapshot.CreateApplicationMenu).ToArray();
		OnChanged();
	}

	/// <summary>Replaces web toolbar definitions and refreshes the circuit's presentation.</summary>
	public virtual void SetToolBarItems(IEnumerable<SharedUI.IApplicationMenuItem> items) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item is IApplicationMenuItem), nameof(items), "Blazor menu item definitions are required.");
		_toolBarItems = definitions.Cast<IApplicationMenuItem>().Select(ApplicationBlockSnapshot.CreateApplicationItem).ToArray();
		OnChanged();
	}

	/// <inheritdoc />
	protected override void OnScreenHostChanged() {
		OnChanged();
	}

	// The existing application initialization scope captures the provider selected by the UserSettings facade.
	protected override ISettingsProvider GetSettingsProvider() {
		// A known circuit can also be inspected before initialization without capturing another call's provider in the lazy property.
		if (Services?.GetService<ISettingsProvider>() is { } scopedProvider) {
			using var settings = UserSettings.UseProvider(scopedProvider);
			return UserSettings.Provider;
		}
		UserSettings.TryGetProvider(out var provider);
		return provider;
	}

	// Restore actual owner objects before ordinary initializers or operational screens can run.
	protected override async Task<bool> OnInitializeAsync(CancellationToken cancellationToken) {
		var provider = Settings;
		foreach (var plugin in LoadedPlugins)
			plugin.PluginSettings = await RestoreSettingsAsync(plugin.PluginSettings, provider, cancellationToken);
		foreach (var block in ScreenHost.AvailableBlocks) {
			block.BlockSettings = await RestoreSettingsAsync(block.BlockSettings, provider, cancellationToken);
			foreach (var definition in block.ScreenDefinitions) {
				if (definition.Settings == null)
					continue;
				definition.Settings.ID = ApplicationHelper.GetScreenSettingsId(SettingsKey, block, definition, block.Id);
				definition.Settings = await RestoreSettingsAsync(definition.Settings, provider, cancellationToken,
					ApplicationHelper.GetScreenSettingsKey(SettingsKey, block, definition, block.Id));
			}
		}
		if (LoadedPlugins.Any(plugin => plugin.PluginSettings != null)
			|| ScreenHost.AvailableBlocks.Any(block => block.BlockSettings != null || block.ScreenDefinitions.Any(screen => screen.Settings != null)))
			InstallSettingsCommand();
		return await base.OnInitializeAsync(cancellationToken);
	}

	// A typed record keeps the provider's identity and ownership; optional read failures retain usable defaults without writing.
	private async Task<SettingsObject> RestoreSettingsAsync(SettingsObject settings, ISettingsProvider provider, CancellationToken cancellationToken, string legacyKey = null) {
		if (settings == null)
			return null;
		Guard.Ensure(provider != null, "Declared application settings require a settings provider.");
		settings.Provider = provider;
		var id = settings.ID;
		string backup = null;
		try {
			if (await provider.ContainsSettingAsync(settings.GetType(), settings.ID, cancellationToken)) {
				backup = SettingsObjectJsonSerializer.SerializeValues(settings);
				var loaded = await provider.LoadSettingAsync(settings, cancellationToken);
				Guard.Ensure(loaded != null && settings.GetType().IsInstanceOfType(loaded), "Stored settings have the wrong declared type.");
				// Providers may return a cached record; retain the actual root supplied by this circuit owner.
				if (!ReferenceEquals(loaded, settings))
					SettingsObjectJsonSerializer.PopulateValues(SettingsObjectJsonSerializer.SerializeValues(loaded), settings);
				cancellationToken.ThrowIfCancellationRequested();
				settings.Validate().ThrowOnFailure();
				Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
				return settings;
			}

			// The retired browser envelope is read only when no typed root exists.
			var legacyProvider = provider;
			while (legacyProvider is SettingsProviderDecorator decorator)
				legacyProvider = decorator.Inner;
			if (legacyKey != null && legacyProvider is BlazorSettingsProvider browser) {
				var payload = await browser.ReadAsync(legacyKey, cancellationToken);
				if (payload != null) {
					using var record = JsonDocument.Parse(payload);
					Guard.Ensure(record.RootElement.TryGetProperty("values", out var values), "The legacy browser settings contain no values.");
					backup = SettingsObjectJsonSerializer.SerializeValues(settings);
					SettingsObjectJsonSerializer.PopulateValues(values.GetRawText(), settings);
					cancellationToken.ThrowIfCancellationRequested();
					settings.Validate().ThrowOnFailure();
					Validator.ValidateObject(settings, new ValidationContext(settings), validateAllProperties: true);
				}
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
			settings.ID = id;
			settings.Provider = provider;
		}
		return settings;
	}

	// Accepted shutdown saves screen-owned changes while this circuit's existing provider remains available.
	protected override async Task OnFinalizeAsync(CancellationToken cancellationToken) {
		// The base lifecycle only calls this hook after startup begins and has already cleared IsInitialized.
		foreach (var screen in ScreenHost.Screens.Select(session => session.Screen).OfType<ApplicationScreen>())
			await screen.SaveSettingsAsync(cancellationToken);
		await base.OnFinalizeAsync(cancellationToken);
	}

	// Add the preferences action to an existing Options heading without changing its operational commands.
	private void InstallSettingsCommand() {
		const string commandId = "sphere10-screen-options";
		if (Menus.SelectMany(menu => menu.Items).Any(item => item.Id == commandId))
			return;
		var command = new BlazorActionMenuItem {
			Id = commandId,
			Title = "Screen Options...",
			AsyncAction = async (services, token) => {
				// Product identity comes from the configured provider, independently of the host caption.
				var productName = services.GetService<IProductInformationProvider>()?.ProductInformation?.ProductName;
				var parameters = ParameterView.FromDictionary(new Dictionary<string, object> {
					[nameof(ApplicationSettingsDialog.Application)] = this,
					[nameof(ApplicationSettingsDialog.ApplicationTitle)] = string.IsNullOrWhiteSpace(productName)
						? services.GetRequiredService<ApplicationOptions>().Title : productName
				});
				await services.GetRequiredService<Modal.ModalService>().ShowAsync<ApplicationSettingsDialog>(parameters, new ModalOptions { Size = ModalSize.Large }, token);
			}
		};
		var menus = Menus.ToList();
		var existing = menus.FindIndex(menu => string.Equals(menu.Text.Replace("&", string.Empty).Trim(), "Options", StringComparison.OrdinalIgnoreCase));
		if (existing < 0)
			menus.Add(new ApplicationMenu { Id = SettingsMenuId, Text = "Options", Items = new[] { command } });
		else {
			var menu = menus[existing];
			menus[existing] = new ApplicationMenu(ownsItems: false) {
				Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = menu.Items.Concat(new[] { command }).ToArray()
			};
		}
		SetMenus(menus);
	}

	/// <summary>Captures successful startup membership without replaying plugin lifecycle operations.</summary>
	private static ApplicationPluginRegistration<IBlazorPlugin>[] CaptureLoadedPlugins(IEnumerable<IBlazorPlugin> loadedPlugins) {
		Guard.ArgumentNotNull(loadedPlugins, nameof(loadedPlugins));
		return ApplicationHelper.ValidatePlugins(loadedPlugins)
			.Select(plugin => new ApplicationPluginRegistration<IBlazorPlugin>((IBlazorPlugin)plugin, plugin.Blocks)).ToArray();
	}
}
