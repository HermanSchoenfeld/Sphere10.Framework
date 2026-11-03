// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates individual, combined and zero-valued flags with a live bit-mask readout.</summary>
public partial class FlagsCheckedBoxListScreen : WinFormsApplicationScreen {
	/// <summary>Creates the list without selecting operational data or writing preferences.</summary>
	public FlagsCheckedBoxListScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_flagsCheckedListBox.EnumType = typeof(Test1Enum);
		UpdateSelection();
	}

	// ItemChecked is the existing post-mutation event, unlike the native ItemCheck preview.
	private void FlagsItemChecked(object sender, ItemCheckEventArgs args) {
		if (IsDisposed || Disposing)
			return;
		UpdateSelection();
		_outputTextBox.AppendText(_selectionSummary.Text + Environment.NewLine);
	}

	// The zero value is explicitly distinct from an absent selection.
	private void ClearFlagsClicked(object sender, EventArgs args) {
		_flagsCheckedListBox.SelectedEnum = Test1Enum.Zero;
		UpdateSelection();
	}

	// The original default combines the first and third flags.
	private void DefaultFlagsClicked(object sender, EventArgs args) {
		_flagsCheckedListBox.SelectedEnum = Test1Enum.Default;
		UpdateSelection();
	}

	// Combined members retain the same component flags as individual choices.
	private void AllFlagsClicked(object sender, EventArgs args) {
		_flagsCheckedListBox.SelectedEnum = Test1Enum.All;
		UpdateSelection();
	}

	// There is an issue dealing with flags that have value '0': display absence and zero separately.
	private void UpdateSelection() {
		var selected = _flagsCheckedListBox.SelectedEnum;
		_selectionSummary.Text = selected == null ? "No checked value · Mask: 0" : $"{selected} · Mask: {Convert.ToInt64(selected)} (0x{Convert.ToInt64(selected):X})";
	}

	[Flags]
	private enum Test1Enum {
		Zero = 0,
		Flag1 = 1 << 0,
		Flag2 = 1 << 1,
		Flag3 = 1 << 2,
		[Description("Flags 1 & 2")]
		Flag1And2 = Flag1 | Flag2,
		All = Flag1 | Flag2 | Flag3,
		Default = Flag1 | Flag3
	}
}
