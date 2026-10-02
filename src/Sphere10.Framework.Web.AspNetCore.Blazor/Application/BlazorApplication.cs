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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Projects a circuit's live host and startup plugin definitions without loading or owning those plugins again.</summary>
public class BlazorApplication : ApplicationBase, IBlazorApplication {
	private readonly IBlazorPlugin[] _plugins;
	private readonly IBlazorPlugin _implicitPlugin;

	public BlazorApplication(IBlazorApplicationScreenHost screenHost, IEnumerable<IBlazorPlugin> plugins) {
		Guard.ArgumentNotNull(screenHost, nameof(screenHost));
		Guard.ArgumentNotNull(plugins, nameof(plugins));
		_plugins = Tools.UI.ValidatePlugins(plugins).Cast<IBlazorPlugin>().ToArray();
		ScreenHost = screenHost;
		_implicitPlugin = new StandalonePlugin(Tools.UI.GetImplicitPluginName(_plugins), GetStandaloneBlocks);
		ScreenHost.Changed += OnChanged;
	}

	public IBlazorApplicationScreenHost ScreenHost { get; }

	/// <summary>Registered definitions plus the circuit's implicit plugin while standalone blocks exist.</summary>
	public override IBlazorPlugin[] Plugins => _implicitPlugin.Blocks.Length == 0 ? Tools.Array.Clone(_plugins) : _plugins.Append(_implicitPlugin).ToArray();

	/// <summary>Compatibility alias for Plugins. Reading it never loads plugin services again.</summary>
	public IBlazorPlugin[] LoadedPlugins => Plugins;

	public override IBlazorPlugin ActivePlugin => (IBlazorPlugin)base.ActivePlugin;

	public override IBlazorApplicationBlock[] Blocks => ScreenHost.Blocks;

	public override IBlazorApplicationBlock ActiveBlock => ScreenHost.ActiveBlock;

	public override IBlazorApplicationScreen ActiveScreen => ScreenHost.ActiveScreen?.Screen;

	public override bool HasUnsavedChanges => ScreenHost.HasUnsavedChanges;

	public override IBlazorApplicationMenu[] Menus => base.Menus.Cast<IBlazorApplicationMenu>().ToArray();

	public override IBlazorApplicationMenuItem[] ToolBarItems => base.ToolBarItems.Cast<IBlazorApplicationMenuItem>().ToArray();

	public override void SetMenus(IEnumerable<IApplicationMenu> menus) {
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu is IBlazorApplicationMenu), nameof(menus), "Blazor menu definitions are required.");
		base.SetMenus(definitions.Cast<IBlazorApplicationMenu>().Select(BlazorApplicationBlockSnapshot.CreateMenu));
	}

	public override void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item is IBlazorApplicationMenuItem), nameof(items), "Blazor menu item definitions are required.");
		base.SetToolBarItems(definitions.Cast<IBlazorApplicationMenuItem>().Select(BlazorApplicationBlockSnapshot.CreateItem));
	}

	protected override void FreeManagedResources() {
		ScreenHost.Changed -= OnChanged;
		base.FreeManagedResources();
	}

	private IBlazorApplicationBlock[] GetStandaloneBlocks() {
		var ownedIds = _plugins.SelectMany(plugin => plugin.Blocks).Select(block => block.Id).ToHashSet(StringComparer.Ordinal);
		return ScreenHost.Blocks.Where(block => !ownedIds.Contains(block.Id)).ToArray();
	}

	/// <summary>Groups the host's otherwise unowned blocks without loading services or owning their lifecycle.</summary>
	private sealed class StandalonePlugin : ApplicationPlugin, IBlazorPlugin {
		private readonly Func<IBlazorApplicationBlock[]> _getBlocks;

		public StandalonePlugin(string name, Func<IBlazorApplicationBlock[]> getBlocks)
			: base(name, Array.Empty<IApplicationBlock>()) {
			_getBlocks = getBlocks;
		}

		public new IBlazorApplicationBlock[] Blocks => _getBlocks();

		public IServiceProvider IoCContainer => null;

		IApplicationBlock[] IApplicationPlugin.Blocks => Blocks;
	}
}
