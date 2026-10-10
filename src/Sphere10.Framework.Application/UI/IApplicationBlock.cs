// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>UI-independent application block metadata. Platform adapters retain ownership of presentation resources.</summary>
public interface IApplicationBlock : IApplicationCommandProvider {
	/// <summary>The application currently registering this block, or null when it is not registered.</summary>
	IApplication Parent { get; set; }

	/// <summary>The startup plugin that contributed this block, or null for a standalone block.</summary>
	IApplicationPlugin Source { get; set; }

	string Name { get; }

	int Position { get; }

	new IApplicationMenu[] Menus { get; }

	IApplicationMenu[] IApplicationCommandProvider.Menus => Menus;

	ApplicationScreenDefinition[] ScreenDefinitions { get; }

	ApplicationScreenDefinition DefaultScreenDefinition { get; }
}

public interface IApplicationBlock<out TMenu, out TItem> : IApplicationBlock, IApplicationCommandProvider<TMenu, TItem>
	where TMenu : class, IApplicationMenu
	where TItem : class, IApplicationMenuItem {
	new TMenu[] Menus { get; }
	IApplicationMenu[] IApplicationBlock.Menus => Menus;
	IApplicationMenu[] IApplicationCommandProvider.Menus => Menus;
}
