// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>The circuit-local application aggregate over the existing retained-screen host.</summary>
public interface IBlazorApplication : IApplication {
	IBlazorApplicationScreenHost ScreenHost { get; }

	new IBlazorPlugin[] Plugins => LoadedPlugins;

	/// <summary>Compatibility alias for Plugins, including any implicit plugin containing standalone blocks.</summary>
	IBlazorPlugin[] LoadedPlugins { get; }

	new IBlazorPlugin ActivePlugin { get; }

	new IBlazorApplicationBlock[] Blocks { get; }

	new IBlazorApplicationBlock ActiveBlock { get; }

	new IBlazorApplicationScreen ActiveScreen { get; }

	new IBlazorApplicationMenu[] Menus { get; }

	new IBlazorApplicationMenuItem[] ToolBarItems { get; }

	IApplicationPlugin[] IApplication.Plugins => Plugins;

	IApplicationPlugin IApplication.ActivePlugin => ActivePlugin;

	IApplicationBlock[] IApplication.Blocks => Blocks;

	IApplicationBlock IApplication.ActiveBlock => ActiveBlock;

	IApplicationScreen IApplication.ActiveScreen => ActiveScreen;

	IApplicationMenu[] IApplicationCommandProvider.Menus => Menus;

	IApplicationMenuItem[] IApplicationCommandProvider.ToolBarItems => ToolBarItems;
}
