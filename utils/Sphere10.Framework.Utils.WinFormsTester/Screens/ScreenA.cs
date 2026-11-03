// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows retained calendar input and a contextual toolbar owned by this screen instance.</summary>
public partial class ScreenA : WinFormsApplicationScreen {
	/// <summary>Creates the designer-owned calendar example without operational work.</summary>
	public ScreenA() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Preserve the original toolbar-to-input interaction with a meaningful caption and outcome.
	private void ToolStripButton1_Click(object sender, EventArgs args) {
		_radioButton1.Checked = true;
		UpdateSelection();
	}

	// Native date and confirmation changes update the same readable summary.
	private void SelectionChanged(object sender, EventArgs args) {
		UpdateSelection();
	}

	// Display actual user input, leaving it owned by this independent screen.
	private void UpdateSelection() {
		_selection.Text = $"{_monthCalendar1.SelectionStart:d} – {_monthCalendar1.SelectionEnd:d} · confirmed: {_radioButton1.Checked}";
	}
}
