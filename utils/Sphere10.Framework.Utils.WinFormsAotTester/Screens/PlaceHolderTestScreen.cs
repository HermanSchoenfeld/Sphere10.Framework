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

/// <summary>Compares the native placeholder extender and reusable Framework editor properties.</summary>
public partial class PlaceHolderTestScreen : ApplicationScreen {
	/// <summary>Creates both groups with ordinary native editing behavior.</summary>
	public PlaceHolderTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>Applies extender cues only after the owner has a real native window.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (Tools.Runtime.IsDesignMode)
			return;
		_cueTextExtender1.SetPlaceHolderText(_textBox1, "Enter a plain text value");
		_cueTextExtender1.SetPlaceHolderText(_comboBox1, "Choose or type a value");
		_cueTextExtender1.SetPlaceHolderText(_maskedTextBox1, "Enter a formatted value");
		_cueTextExtender1.SetPlaceHolderText(_richTextBox1, "Enter rich text");
		_cueTextExtender1.SetPlaceHolderText(_searchTextBox1, "Search through the extender");
	}

	// Clearing text restores the actual native cue rather than painting an imitation label.
	private void ClearInputsClicked(object sender, EventArgs args) {
		foreach (var control in new Control[] { _textBox1, _comboBox1, _maskedTextBox1, _richTextBox1, _searchTextBox1, _textBoxEx1, _moneyBox1, _searchTextBox2, _comboBoxEx1 })
			control.Text = string.Empty;
		_comboBox1.SelectedIndex = -1;
		_comboBoxEx1.SelectedIndex = -1;
	}
}
