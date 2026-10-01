// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A native application block with platform-specific images and menu placement.</summary>
public interface IWinFormsApplicationBlock : IDisposable, IApplicationBlock {
	new int Position { get; }
	new string Name { get; }
	new IWinFormsApplicationMenu[] Menus { get; }
	Image Image32x32 { get; }
	Image Image8x8 { get; }
	string HelpFileCHM { get; }
	bool ShowInMenuStrip { get; }
	bool ShowInToolStrip { get; }
	new Type DefaultScreen { get; }
	new string DefaultScreenTitle => null;

	string IApplicationBlock.Id => Name;
	string IApplicationBlock.Name => Name;
	int IApplicationBlock.Position => Position;
	Type IApplicationBlock.DefaultScreen => DefaultScreen;
	string IApplicationBlock.DefaultScreenTitle => DefaultScreenTitle;
	IApplicationMenu[] IApplicationBlock.Menus => Menus;
}
