// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Provides explicit save and temporary default restoration for a screen-owned grid configuration.</summary>
[ToolboxItem(true)]
public partial class VisualGridConfigurationContextMenu : ContextMenuStrip {

	/// <summary>Creates the native configuration commands for designer or runtime attachment.</summary>
	public VisualGridConfigurationContextMenu() {
		InitializeComponent();
	}

	/// <summary>Lets the supplying screen's component container own this menu's lifetime.</summary>
	public VisualGridConfigurationContextMenu(IContainer container)
		: this() {
		container?.Add(this);
	}

	/// <summary>Chooses whether restoring declared defaults affects only the live view or also persists them.</summary>
	[Category("Behavior")]
	[DefaultValue(VisualGridDefaultsRestorePolicy.RestoreOnly)]
	public VisualGridDefaultsRestorePolicy DefaultsRestorePolicy { get; set; } = VisualGridDefaultsRestorePolicy.RestoreOnly;

	/// <summary>The fixed commands are created by this component's designer, rather than serialized again by each consuming form.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new ToolStripItemCollection Items {
		get {
			return base.Items;
		}
	}

	// Only already configured screen-owned grids can execute these commands; opening never reads storage.
	private void ConfigurationMenuOpening(object sender, CancelEventArgs args) {
		if (DesignMode)
			return;

		var grid = SourceControl as VisualGrid;
		var screen = FindScreen(grid);
		var canConfigure = grid != null && !grid.IsDisposed && !grid.Disposing && screen?.CanConfigureGrid(grid) == true;
		_restoreDefaultsMenuItem.Text = DefaultsRestorePolicy == VisualGridDefaultsRestorePolicy.RestoreAndSave ? "Restore defaults and save" : "Restore defaults";
		_saveConfigurationMenuItem.Enabled = _restoreDefaultsMenuItem.Enabled = canConfigure;
		args.Cancel |= !canConfigure;
	}

	// Capture the source before awaiting because one native menu can be reused by several grids.
	private async void SaveConfigurationClicked(object sender, EventArgs args) {
		var grid = SourceControl as VisualGrid;
		var screen = FindScreen(grid);
		if (screen?.CanConfigureGrid(grid) != true)
			return;

		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => screen.SaveGridConfigurationAsync(grid));
		if (error != null && !grid.IsDisposed && !grid.Disposing)
			await ExceptionDialog.ShowAsync(grid, "Save configuration", error);
	}

	// Capture the configured policy with the source before the awaited restoration can yield.
	private async void RestoreDefaultsClicked(object sender, EventArgs args) {
		var grid = SourceControl as VisualGrid;
		var screen = FindScreen(grid);
		if (screen?.CanConfigureGrid(grid) != true)
			return;

		var policy = DefaultsRestorePolicy;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			await screen.RestoreGridDefaultsAsync(grid);
			if (policy == VisualGridDefaultsRestorePolicy.RestoreAndSave)
				await screen.SaveGridConfigurationAsync(grid);
		});
		if (error != null && !grid.IsDisposed && !grid.Disposing)
			await ExceptionDialog.ShowAsync(grid, "Restore defaults", error);
	}

	// The native SourceControl retains its actual parent chain, including a detached screen's host.
	private static ApplicationScreen FindScreen(Control control) {
		for (var current = control; current != null; current = current.Parent)
			if (current is ApplicationScreen screen)
				return screen;
		return null;
	}
}
