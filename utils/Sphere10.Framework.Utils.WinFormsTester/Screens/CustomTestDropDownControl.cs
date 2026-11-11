// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>The custom dropdown example's retained composite selection editor.</summary>
public partial class UserControl1 : UserControl {
	/// <summary>Reports an explicit acceptance of the current checked categories.</summary>
	public event EventHandler SelectionAccepted;

	/// <summary>Creates the original composite surface with actionable selection commands.</summary>
	public UserControl1() {
		InitializeComponent();
	}

	/// <summary>A detached snapshot of the categories currently checked in this editor.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string[] SelectedCategories => _checkedListBox1.CheckedItems.Cast<object>().Select(value => value.ToString()).ToArray();

	// Acceptance belongs to the containing popup, which decides when to close.
	private void ApplyClicked(object sender, EventArgs args) => SelectionAccepted?.Invoke(this, EventArgs.Empty);

	// Reuse the native check-state behavior rather than creating a second selection model.
	private void SelectAllClicked(object sender, EventArgs args) {
		for (var index = 0; index < _checkedListBox1.Items.Count; index++)
			_checkedListBox1.SetItemChecked(index, true);
	}

	// Clearing leaves both independent option checkboxes unchanged.
	private void ClearClicked(object sender, EventArgs args) {
		for (var index = 0; index < _checkedListBox1.Items.Count; index++)
			_checkedListBox1.SetItemChecked(index, false);
	}

	// Calculate size of checked list box while keeping resize paths valid for a small popup.
	private void UserControl1_SizeChanged(object sender, EventArgs args) {
		_checkedListBox1.Size = new Size(Math.Max(1, DisplayRectangle.Width - _checkedListBox1.Left - 5),
			Math.Max(1, DisplayRectangle.Height - _checkedListBox1.Top - _button1.Height - 10));
	}
}
