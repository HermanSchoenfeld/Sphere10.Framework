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
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Adapts the running native form without duplicating block or screen ownership.</summary>
public class WinFormsApplication : ApplicationBase, IWinFormsApplication {
	private readonly BlockMainForm _mainForm;
	private readonly IWinFormsApplicationBlock[] _configuredBlocks;
	private readonly ApplicationPluginRegistration<IWinFormsApplicationPlugin>[] _loadedPluginRegistrations;
	private readonly IWinFormsApplicationPlugin[] _loadedPlugins;
	private bool _initialized;

	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks)
		: this(mainForm, blocks, Array.Empty<IWinFormsApplicationPlugin>()) {
	}

	/// <summary>Adapts blocks and plugins that the caller has already loaded; this constructor does not invoke plugin lifecycle methods.</summary>
	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks, IEnumerable<IWinFormsApplicationPlugin> loadedPlugins)
		: this(mainForm, blocks, Tools.UI.ValidatePlugins(loadedPlugins).Cast<IWinFormsApplicationPlugin>()
			.Select(plugin => new ApplicationPluginRegistration<IWinFormsApplicationPlugin>(plugin, plugin.Blocks))) {
	}

	internal WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks,
		IEnumerable<ApplicationPluginRegistration<IWinFormsApplicationPlugin>> loadedPluginRegistrations) {
		Guard.ArgumentNotNull(mainForm, nameof(mainForm));
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		Guard.ArgumentNotNull(loadedPluginRegistrations, nameof(loadedPluginRegistrations));
		_mainForm = mainForm;
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
		_mainForm.ApplicationChanged += OnChanged;
	}

	public IBlockManager BlockManager => _mainForm;

	public override IWinFormsApplicationScreenHost ScreenHost => _mainForm.ScreenHost;

	public override IWinFormsApplicationPlugin[] LoadedPlugins => Tools.Array.Clone(_loadedPlugins);

	public override IWinFormsApplicationBlock[] Blocks => ScreenHost.Blocks;

	public override bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

	public override IWinFormsApplicationMenu[] Menus => base.Menus.Cast<IWinFormsApplicationMenu>().ToArray();

	public override IWinFormsApplicationMenuItem[] ToolBarItems => base.ToolBarItems.Cast<IWinFormsApplicationMenuItem>().ToArray();

	public void Initialize() {
		Guard.Ensure(!IsDisposed, "The application has been disposed.");
		if (_initialized)
			return;
		var ordered = Tools.UI.OrderApplicationBlocks(_loadedPluginRegistrations,
			_configuredBlocks.Concat(_mainForm.RegisteredBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance));
		_initialized = _mainForm.RegisterBlocks(ordered.Cast<IWinFormsApplicationBlock>());
	}

	public override void SetMenus(IEnumerable<IApplicationMenu> menus) {
		Guard.Ensure(!IsDisposed, "The application has been disposed.");
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu is IWinFormsApplicationMenu), nameof(menus), "WinForms menu definitions are required.");
		_mainForm.SetApplicationCommands(definitions.Cast<IWinFormsApplicationMenu>().ToArray(), ToolBarItems);
		base.SetMenus(definitions);
	}

	public override void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		Guard.Ensure(!IsDisposed, "The application has been disposed.");
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item is IWinFormsApplicationMenuItem), nameof(items), "WinForms menu item definitions are required.");
		_mainForm.SetApplicationCommands(Menus, definitions.Cast<IWinFormsApplicationMenuItem>().ToArray());
		base.SetToolBarItems(definitions);
	}

	protected override void FreeManagedResources() {
		_mainForm.ApplicationChanged -= OnChanged;
		base.FreeManagedResources();
	}

}
