// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>A reusable native editor whose independent draft survives tab changes.</summary>
public partial class TabControlTestControl : UserControl {
	/// <summary>Creates the standard/custom draft example without external operations.</summary>
	public TabControlTestControl() {
		InitializeComponent();
		UpdateDraftState();
	}

	// Both native radio choices update only this composite editor's draft mode.
	private void DraftModeChanged(object sender, EventArgs args) => UpdateDraftState();

	// Draft edits report local state without persisting demonstration data as user preferences.
	private void DraftChanged(object sender, EventArgs args) => UpdateDraftState();

	// Returning to the tab preserves the same controls and their current draft.
	private void UpdateDraftState() {
		_draftInput.ReadOnly = !_customMode.Checked;
		_status.Text = _customMode.Checked
			? $"Custom draft · {_draftInput.TextLength} characters. Switch tabs and return to inspect the retained value."
			: "Standard mode · choose Custom draft to edit this page's independent value.";
	}
}
