// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Projects a circuit's live host and startup plugin definitions without loading or owning those plugins again.</summary>
public class BlazorApplication : AsyncApplicationBase, IBlazorApplication {
	private readonly IBlazorPlugin[] _loadedPlugins;

	/// <summary>Projects plugins already loaded successfully during startup; construction does not load or unload plugins.</summary>
	public BlazorApplication(IBlazorApplicationScreenHost screenHost, IEnumerable<IBlazorPlugin> loadedPlugins)
		: this(screenHost, CaptureLoadedPlugins(loadedPlugins)) {
	}

	internal BlazorApplication(IBlazorApplicationScreenHost screenHost, IEnumerable<ApplicationPluginRegistration<IBlazorPlugin>> pluginRegistrations, IServiceProvider services = null)
		: base(services) {
		Guard.ArgumentNotNull(screenHost, nameof(screenHost));
		var registrations = Tools.UI.ValidatePluginRegistrations(pluginRegistrations);
		_loadedPlugins = registrations.Select(registration => registration.Plugin).ToArray();
		ScreenHost = screenHost;
		var sources = registrations.SelectMany(registration => registration.Blocks.Cast<IBlazorApplicationBlock>()
			.Select(block => (block.Id, registration.Plugin))).ToDictionary(contribution => contribution.Id, contribution => contribution.Plugin);
		foreach (var block in ScreenHost.AvailableBlocks)
			if (sources.TryGetValue(block.Id, out var plugin))
				Guard.Argument(block.Source == null || ReferenceEquals(block.Source, plugin), nameof(pluginRegistrations), "The block already has another source plugin.");
		foreach (var block in ScreenHost.AvailableBlocks)
			if (sources.TryGetValue(block.Id, out var plugin))
				block.Source = plugin;
		ObserveScreenHost();
	}

	public override IBlazorApplicationScreenHost ScreenHost { get; }

	public override IBlazorPlugin[] LoadedPlugins => Tools.Array.Clone(_loadedPlugins);

	public override IBlazorApplicationBlock[] Blocks => ScreenHost.Blocks;

	public override bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

	public override IBlazorApplicationMenu[] Menus => base.Menus.Cast<IBlazorApplicationMenu>().ToArray();

	public override IBlazorApplicationMenuItem[] ToolBarItems => base.ToolBarItems.Cast<IBlazorApplicationMenuItem>().ToArray();

	/// <summary>Synchronous exit is available outside the renderer; interactive callbacks must await ExitAsync.</summary>
	public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		if (HasExited)
			return true;
		Guard.Ensure(SynchronizationContext.Current == null, "Blazor renderer callbacks must await ExitAsync instead of blocking synchronous exit.");
		return base.Exit(force, cancellationToken);
	}

	public override void SetMenus(IEnumerable<IApplicationMenu> menus) {
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu is IBlazorApplicationMenu), nameof(menus), "Blazor menu definitions are required.");
		var projections = definitions.Cast<IBlazorApplicationMenu>().Select(BlazorApplicationBlockSnapshot.CreateApplicationMenu).ToArray();
		base.SetMenus(projections);
	}

	public override void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item is IBlazorApplicationMenuItem), nameof(items), "Blazor menu item definitions are required.");
		var projections = definitions.Cast<IBlazorApplicationMenuItem>().Select(BlazorApplicationBlockSnapshot.CreateApplicationItem).ToArray();
		base.SetToolBarItems(projections);
	}

	protected override void OnScreenHostChanged() => OnChanged();

	private static ApplicationPluginRegistration<IBlazorPlugin>[] CaptureLoadedPlugins(IEnumerable<IBlazorPlugin> loadedPlugins) {
		Guard.ArgumentNotNull(loadedPlugins, nameof(loadedPlugins));
		return Tools.UI.ValidatePlugins(loadedPlugins)
			.Select(plugin => new ApplicationPluginRegistration<IBlazorPlugin>((IBlazorPlugin)plugin, plugin.Blocks)).ToArray();
	}
}
