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

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Adapts the running native form without duplicating block or screen ownership.</summary>
public class WinFormsApplication : ApplicationBase, IWinFormsApplication {
	private readonly BlockMainForm _mainForm;
	private readonly IWinFormsApplicationBlock[] _configuredBlocks;
	private readonly IWinFormsApplicationPlugin[] _plugins;
	private readonly IWinFormsApplicationPlugin _implicitPlugin;
	private bool _initialized;

	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks)
		: this(mainForm, blocks, Array.Empty<IWinFormsApplicationPlugin>()) {
	}

	public WinFormsApplication(BlockMainForm mainForm, IEnumerable<IWinFormsApplicationBlock> blocks, IEnumerable<IWinFormsApplicationPlugin> plugins) {
		Guard.ArgumentNotNull(mainForm, nameof(mainForm));
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		Guard.ArgumentNotNull(plugins, nameof(plugins));
		_mainForm = mainForm;
		var definitions = Tools.UI.ValidatePlugins(plugins).Cast<IWinFormsApplicationPlugin>().ToArray();
		var groupedBlocks = definitions.SelectMany(plugin => plugin.Blocks).ToArray();
		_configuredBlocks = blocks.Concat(groupedBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
		Guard.Argument(_configuredBlocks.All(block => block != null), nameof(blocks), "Block definitions cannot be null.");
		_plugins = definitions;
		_implicitPlugin = new ImplicitApplicationPlugin(Tools.UI.GetImplicitPluginName(definitions), GetUngroupedBlocks);
		_ = Plugins;
		_mainForm.ApplicationChanged += OnChanged;
	}

	public IBlockManager BlockManager => _mainForm;

	public IWinFormsApplicationScreenHost ScreenHost => _mainForm.ScreenHost;

	public override IWinFormsApplicationPlugin[] Plugins {
		get {
			var plugins = GetUngroupedBlocks().Length == 0 ? _plugins : _plugins.Append(_implicitPlugin);
			return Tools.UI.ValidatePlugins(plugins).Cast<IWinFormsApplicationPlugin>().ToArray();
		}
	}

	public override IWinFormsApplicationPlugin ActivePlugin => (IWinFormsApplicationPlugin)base.ActivePlugin;

	public override IWinFormsApplicationBlock[] Blocks => _mainForm.RegisteredBlocks;

	public override IWinFormsApplicationBlock ActiveBlock => _mainForm.ActiveBlock;

	public override IWinFormsApplicationScreen ActiveScreen => _mainForm.ActiveScreen;

	public override bool HasUnsavedChanges => ScreenHost.Screens.Any(screen => ((IApplicationScreen)screen).HasUnsavedChanges);

	public override IWinFormsApplicationMenu[] Menus => base.Menus.Cast<IWinFormsApplicationMenu>().ToArray();

	public override IWinFormsApplicationMenuItem[] ToolBarItems => base.ToolBarItems.Cast<IWinFormsApplicationMenuItem>().ToArray();

	public void Initialize() {
		Guard.Ensure(!IsDisposed, "The application has been disposed.");
		if (_initialized)
			return;
		var ordered = Tools.UI.OrderApplicationBlocks(Plugins, _configuredBlocks.Concat(_mainForm.RegisteredBlocks).Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance));
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

	private IWinFormsApplicationBlock[] GetUngroupedBlocks() {
		var registered = _mainForm.RegisteredBlocks;
		var candidates = _initialized ? registered : _configuredBlocks.Concat(registered);
		var grouped = _plugins.SelectMany(plugin => plugin.Blocks).ToArray();
		return candidates.Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance)
			.Where(block => !grouped.Any(definition => ReferenceEquals(definition, block))).ToArray();
	}

	protected override void FreeManagedResources() {
		_mainForm.ApplicationChanged -= OnChanged;
		base.FreeManagedResources();
	}

	private sealed class ImplicitApplicationPlugin : WinFormsApplicationPlugin {
		private readonly Func<IWinFormsApplicationBlock[]> _getBlocks;

		public ImplicitApplicationPlugin(string name, Func<IWinFormsApplicationBlock[]> getBlocks)
			: base(name, Array.Empty<IWinFormsApplicationBlock>()) {
			_getBlocks = getBlocks;
		}

		public override IWinFormsApplicationBlock[] Blocks {
			get => _getBlocks();
			init => base.Blocks = value;
		}
	}
}
