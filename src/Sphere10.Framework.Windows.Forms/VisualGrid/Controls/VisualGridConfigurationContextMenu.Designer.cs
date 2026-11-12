// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class VisualGridConfigurationContextMenu {
	private ToolStripMenuItem _saveConfigurationMenuItem;
	private ToolStripMenuItem _restoreDefaultsMenuItem;

	// Creates the static commands on the standard native context-menu designer surface.
	private void InitializeComponent() {
		_saveConfigurationMenuItem = new ToolStripMenuItem();
		_restoreDefaultsMenuItem = new ToolStripMenuItem();
		SuspendLayout();
		//
		// _saveConfigurationMenuItem
		//
		_saveConfigurationMenuItem.Name = "_saveConfigurationMenuItem";
		_saveConfigurationMenuItem.Text = "Save configuration";
		_saveConfigurationMenuItem.ToolTipText = "Save this grid's current paging and column layout in this screen's user settings.";
		_saveConfigurationMenuItem.Click += SaveConfigurationClicked;
		//
		// _restoreDefaultsMenuItem
		//
		_restoreDefaultsMenuItem.Name = "_restoreDefaultsMenuItem";
		_restoreDefaultsMenuItem.Text = "Restore defaults";
		_restoreDefaultsMenuItem.ToolTipText = "Restore this grid's declared default paging and column layout.";
		_restoreDefaultsMenuItem.Click += RestoreDefaultsClicked;
		//
		// VisualGridConfigurationContextMenu
		//
		Items.AddRange(new ToolStripItem[] { _saveConfigurationMenuItem, _restoreDefaultsMenuItem });
		Name = "VisualGridConfigurationContextMenu";
		Opening += ConfigurationMenuOpening;
		ResumeLayout(false);
	}
}
