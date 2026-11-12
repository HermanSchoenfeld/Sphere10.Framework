// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>
/// Switches this window's block selector between its shapes and colour styles, and shows every combination side by side on
/// sample task panes in the default theme.
/// </summary>
public partial class BlockSelectorStylesScreen : ApplicationScreen {
	private const string ThemeChoice = "Follow theme";
	private static readonly string[] SampleBlocks = ["Workspace", "Notes", "Controls", "Data And Collections"];

	public BlockSelectorStylesScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// The window whose block selector these choices restyle; null when the screen is hosted elsewhere.
	private BlockMainForm MainForm => UserInterfaceServices?.PrimaryUIController as BlockMainForm;

	// The choices and samples are built when first shown, never on a design surface.
	protected override void OnShowFirstTime() {
		base.OnShowFirstTime();
		_shapeComboBox.Items.AddRange([ThemeChoice, .. Enum.GetValues<BlockSelectorShape>().Cast<object>()]);
		_colorStyleComboBox.Items.AddRange([ThemeChoice, .. Enum.GetValues<BlockSelectorColorStyle>().Cast<object>()]);
		// Start from the window's current overrides, so the choices describe what the sidebar shows.
		_shapeComboBox.SelectedItem = (object)MainForm?.BlockSelectorShape ?? ThemeChoice;
		_colorStyleComboBox.SelectedItem = (object)MainForm?.BlockSelectorColorStyle ?? ThemeChoice;
		foreach (var shape in Enum.GetValues<BlockSelectorShape>())
			foreach (var colorStyle in Enum.GetValues<BlockSelectorColorStyle>())
				_gallery.Controls.Add(CreateSample(shape, colorStyle));
	}

	private void ShapeChanged(object sender, EventArgs args) {
		if (MainForm is { } mainForm)
			mainForm.BlockSelectorShape = _shapeComboBox.SelectedItem as BlockSelectorShape?;
	}

	private void ColorStyleChanged(object sender, EventArgs args) {
		if (MainForm is { } mainForm)
			mainForm.BlockSelectorColorStyle = _colorStyleComboBox.SelectedItem as BlockSelectorColorStyle?;
	}

	// One captioned sample bar per combination; the screen owns each sample's fonts and block images.
	private Control CreateSample(BlockSelectorShape shape, BlockSelectorColorStyle colorStyle) {
		var sample = new Panel { Size = LogicalToDeviceUnits(new Size(230, 400)), Margin = new Padding(LogicalToDeviceUnits(6)) };
		var caption = new Label { Dock = DockStyle.Top, AutoSize = false, Height = LogicalToDeviceUnits(22), Text = $"{shape} · {colorStyle}", TextAlign = ContentAlignment.MiddleLeft };
		var bar = new ApplicationBar { Dock = DockStyle.Fill, Shape = shape, ColorStyle = colorStyle };
		var pane = new TaskPane { Dock = DockStyle.Fill };
		// Like the main form's generated menus, each group lays out its own items.
		var font = new Font("Tahoma", 8.25F);
		Disposables.Add(font);
		var expando = new Expando { Text = "Layout", Animate = false, AutoLayout = true, Font = font };
		foreach (var item in new[] { "Tab Control", "Expandable Panels", "Appointment Book" })
			expando.Items.Add(new TaskItem { Text = item, BackColor = Color.Transparent, UseVisualStyleBackColor = false });
		pane.Expandos.Add(expando);
		foreach (var (text, index) in SampleBlocks.Select((text, index) => (text, index))) {
			var image = index switch { 0 => Resources.TestBlock32x32, 1 => Resources.Menu32x32, 2 => Resources.Tests32x32, _ => Resources.Tests232x32 };
			Disposables.Add(image);
			bar.AddItem(new ApplicationBar.Item(pane, image, text));
		}
		sample.Controls.Add(bar);
		sample.Controls.Add(caption);
		return sample;
	}
}
