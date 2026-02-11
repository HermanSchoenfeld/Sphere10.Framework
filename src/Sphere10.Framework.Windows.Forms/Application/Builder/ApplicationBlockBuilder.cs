// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public class ApplicationBlockBuilder {
	private readonly ApplicationBlock _block;

	public ApplicationBlockBuilder() {
		_block = new ApplicationBlock();
	}

	public ApplicationBlockBuilder WithName(string name) {
		_block.Name = name;
		return this;
	}

	public ApplicationBlockBuilder WithImage32x32(Image image) {
		_block.Image32x32 = image;
		return this;
	}

	public ApplicationBlockBuilder WithImage8x8(Image image) {
		_block.Image8x8 = image;
		return this;
	}

	public ApplicationBlockBuilder WithHelpFile(string helpFile) {
		_block.HelpFileCHM = helpFile;
		return this;
	}

	public ApplicationBlockBuilder ShowInMenuStrip(bool show = true) {
		_block.ShowInMenuStrip = show;
		return this;
	}

	public ApplicationBlockBuilder ShowInToolStrip(bool show = true) {
		_block.ShowInToolStrip = show;
		return this;
	}

	public ApplicationBlockBuilder WithDefaultScreen(Type screenType) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		Tools.Values.SetProperty(_block, nameof(ApplicationBlock.DefaultScreen), screenType);
		return this;
	}

	public ApplicationBlockBuilder WithDefaultScreen<TScreen>() {
		Tools.Values.SetProperty(_block, nameof(ApplicationBlock.DefaultScreen), typeof(TScreen));
		return this;
	}

	public ApplicationBlockBuilder AddMenu(Action<MenuBuilder> menuBuild) {
		var menuBuilder = new MenuBuilder();
		menuBuild(menuBuilder);
		var menu = menuBuilder.Build();
		_block.AddMenu(menu);
		return this;
	}

	public ApplicationBlockBuilder AddMenu(IMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		_block.AddMenu(menu);
		return this;
	}

	public ApplicationBlock Build() {
		Guard.Ensure(!string.IsNullOrEmpty(_block.Name), "Block name is required");
		return _block;
	}
}
