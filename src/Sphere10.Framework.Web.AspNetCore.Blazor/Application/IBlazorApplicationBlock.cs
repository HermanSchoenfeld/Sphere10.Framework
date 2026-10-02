// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public interface IBlazorApplicationBlock : IApplicationBlock {
	string Title { get; }

	string IApplicationBlock.Name => Title;

	string IconUrl { get; }

	string Tooltip { get; }

	new IBlazorApplicationMenu[] Menus { get; }

	new IBlazorApplicationMenuItem[] ToolBarItems => Array.Empty<IBlazorApplicationMenuItem>();

	IApplicationMenuItem[] IApplicationCommandProvider.ToolBarItems => ToolBarItems;

	Type IApplicationBlock.DefaultScreen => null;

	IApplicationMenu[] IApplicationBlock.Menus => Menus;
}
