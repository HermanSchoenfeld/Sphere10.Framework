// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationBlockBuilder : ApplicationBlockBuilderBase<WinFormsApplicationBlockBuilder, IWinFormsApplicationMenu, WinFormsApplicationMenuBuilder, WinFormsApplicationBlock> {
	private WinFormsApplicationBlock _block;
	private ApplicationScreenDefinition _generatedDefaultScreenDefinition;
	private Image _image32x32;
	private Image _image8x8;
	private string _helpFile;
	private bool _showInMenuStrip;
	private bool _showInToolStrip;

	public WinFormsApplicationBlockBuilder WithImage32x32(Image image) {
		_image32x32 = image;
		if (_block != null)
			_block.Image32x32 = image;
		return this;
	}

	public WinFormsApplicationBlockBuilder WithImage8x8(Image image) {
		_image8x8 = image;
		if (_block != null)
			_block.Image8x8 = image;
		return this;
	}

	public WinFormsApplicationBlockBuilder WithHelpFile(string helpFile) {
		_helpFile = helpFile;
		if (_block != null)
			_block.HelpFileCHM = helpFile;
		return this;
	}

	public WinFormsApplicationBlockBuilder ShowInMenuStrip(bool show = true) {
		_showInMenuStrip = show;
		if (_block != null)
			_block.ShowInMenuStrip = show;
		return this;
	}

	public WinFormsApplicationBlockBuilder ShowInToolStrip(bool show = true) {
		_showInToolStrip = show;
		if (_block != null)
			_block.ShowInToolStrip = show;
		return this;
	}

	public override WinFormsApplicationBlock Build() {
		if (_block == null)
			return base.Build();
		Guard.Ensure(!string.IsNullOrEmpty(_block.Name), "Block name is required");
		ResolveDefaultScreen();
		foreach (var definition in ScreenDefinitions)
			_block.AddScreenDefinition(definition);
		Tools.UI.GetScreenDefinitions(new IApplicationBlock[] { _block });
		return _block;
	}

	protected override void OnConfigurationChanged(string propertyName) {
		if (_block == null)
			return;
		switch (propertyName) {
			case nameof(Name):
				_block.Name = Name;
				break;
			case nameof(Position):
				_block.Position = Position;
				break;
			case nameof(DefaultScreenDefinition):
				UpdateDefaultScreenDefinition();
				break;
		}
	}

	protected override void OnMenuAdded(IWinFormsApplicationMenu menu) {
		_block?.AddMenu(menu);
		if (_generatedDefaultScreenDefinition != null && IsMenuDefinition(_generatedDefaultScreenDefinition))
			_generatedDefaultScreenDefinition = null;
	}

	protected override IEnumerable<ApplicationScreenDefinition> GetDefaultScreenDefinitions() =>
		base.GetDefaultScreenDefinitions().Concat((_block?.ScreenDefinitions ?? Array.Empty<ApplicationScreenDefinition>())
			.Where(definition => !ReferenceEquals(definition, _generatedDefaultScreenDefinition)));

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(WinFormsApplicationScreen));

	protected override WinFormsApplicationBlock CreateBlock(IReadOnlyList<IWinFormsApplicationMenu> menus) {
		if (_block != null)
			return _block;
		_block = new WinFormsApplicationBlock(Name, _showInToolStrip, _showInMenuStrip, _image32x32, _image8x8, _helpFile, null) {
			Position = Position,
			DefaultScreenDefinition = DefaultScreenDefinition
		};
		foreach (var menu in menus)
			_block.AddMenu(menu);
		if (DefaultScreenDefinition != null)
			UpdateDefaultScreenDefinition();
		return _block;
	}

	private bool IsMenuDefinition(ApplicationScreenDefinition definition) =>
		_block != null && definition != null && definition.Activators.Any(item => ReferenceEquals(item.Parent?.Parent, _block));

	private void UpdateDefaultScreenDefinition() {
		var previousGenerated = _generatedDefaultScreenDefinition;
		_block.DefaultScreenDefinition = DefaultScreenDefinition;
		_generatedDefaultScreenDefinition = IsTypeBasedDefault && !IsMenuDefinition(DefaultScreenDefinition) ? DefaultScreenDefinition : null;
		if (previousGenerated != null && !ScreenDefinitions.Any(definition => ReferenceEquals(definition, previousGenerated)))
			_block.RemoveScreenDefinition(previousGenerated);
	}
}
