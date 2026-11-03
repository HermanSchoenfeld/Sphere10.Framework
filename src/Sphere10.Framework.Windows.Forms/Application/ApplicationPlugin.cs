// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Uses shared plugin storage and lifecycle while preserving native block identities and ownership.</summary>
public class ApplicationPlugin : ApplicationPluginBase<IApplicationBlock>, IApplicationPlugin {
	private IApplicationMenu[] _menus;
	private IApplicationMenuItem[] _toolBarItems;

	public ApplicationPlugin(string name, IEnumerable<IApplicationBlock> blocks)
		: this(name, blocks, null) {
	}

	public ApplicationPlugin(string name, IEnumerable<IApplicationBlock> blocks, Action<IServiceCollection> configureServices)
		: base(name, blocks, configureServices) {
	}

	/// <summary>Activates this plugin's menu definitions once, independently of the windows displaying them.</summary>
	public virtual IApplicationMenu[] Menus => Tools.Array.Clone(_menus ??= GetMenus());

	/// <summary>Activates this plugin's toolbar definitions once, preserving command identity across windows.</summary>
	public virtual IApplicationMenuItem[] ToolBarItems => Tools.Array.Clone(_toolBarItems ??= GetToolBarItems());

	/// <summary>Supplies this plugin's own menu definitions on first access.</summary>
	protected virtual IApplicationMenu[] GetMenus() {
		return Array.Empty<IApplicationMenu>();
	}

	/// <summary>Supplies this plugin's own toolbar definitions on first access.</summary>
	protected virtual IApplicationMenuItem[] GetToolBarItems() {
		return Array.Empty<IApplicationMenuItem>();
	}

}
