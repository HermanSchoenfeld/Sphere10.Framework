// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows animated ExplorerBar groups containing commands, filters and tree navigation.</summary>
public partial class ExpandoTesterScreen : ApplicationScreen {
	/// <summary>Creates the complete ExplorerBar example on its native design surface.</summary>
	public ExpandoTesterScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Task commands share one visible event log rather than being inert sample captions.
	private void TaskClicked(object sender, EventArgs args) => _outputTextBox.AppendText(((Control)sender).Text + Environment.NewLine);

	// A contained native checkbox remains an ordinary independently editable input.
	private void FilterChanged(object sender, EventArgs args) => _outputTextBox.AppendText("Include archived categories: " + _checkBox1.Checked + Environment.NewLine);

	// The tree retains its own native selection behavior inside the expando.
	private void CategorySelected(object sender, TreeViewEventArgs args) => _outputTextBox.AppendText("Category selected: " + args.Node.FullPath + Environment.NewLine);

	// Whole-pane actions use the same actual group state as an individual caption click.
	private void ExpandAllClicked(object sender, EventArgs args) => _expando1.Collapsed = _expando2.Collapsed = _expando3.Collapsed = false;

	// The empty/reference group also participates in repeated collapse and expansion.
	private void CollapseAllClicked(object sender, EventArgs args) => _expando1.Collapsed = _expando2.Collapsed = _expando3.Collapsed = true;
}
