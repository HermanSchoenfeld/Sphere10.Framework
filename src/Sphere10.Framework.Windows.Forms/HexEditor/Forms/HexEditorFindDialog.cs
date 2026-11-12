// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Asks for a hexadecimal byte sequence or UTF-8 text to find, and the search direction.</summary>
public partial class HexEditorFindDialog : Form {
	public HexEditorFindDialog() {
		InitializeComponent();
	}

	/// <summary>The entered pattern text.</summary>
	[Category("Behavior"), DefaultValue("")]
	public string PatternText {
		get => _patternTextBox.Text;
		set => _patternTextBox.Text = value ?? string.Empty;
	}

	/// <summary>Whether the pattern is hexadecimal byte pairs (the default) rather than text.</summary>
	[Category("Behavior"), DefaultValue(true)]
	public bool IsHexPattern {
		get => _hexRadioButton.Checked;
		set {
			_hexRadioButton.Checked = value;
			_textRadioButton.Checked = !value;
		}
	}

	/// <summary>Whether the search proceeds toward the start of the document.</summary>
	[Category("Behavior"), DefaultValue(false)]
	public bool SearchBackward {
		get => _backwardRadioButton.Checked;
		set {
			_backwardRadioButton.Checked = value;
			_forwardRadioButton.Checked = !value;
		}
	}

	/// <summary>The accepted search after OK, or null.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorSearch Search { get; private set; }

	// The pattern is validated before the dialog closes so a typo keeps the input in place.
	private void FindButtonClicked(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<ArgumentException>(() => {
			Search = IsHexPattern ? HexEditorSearch.ForHex(_patternTextBox.Text) : HexEditorSearch.ForText(_patternTextBox.Text, Encoding.UTF8);
		}, out var error);
		if (error != null) {
			_errorLabel.Text = error.Message;
			return;
		}
		DialogResult = DialogResult.OK;
		Close();
	}

	private void PatternKindChanged(object sender, EventArgs e) {
		_errorLabel.Text = string.Empty;
		_patternLabel.Text = IsHexPattern ? "Hexadecimal &bytes (e.g. DE AD BE EF):" : "&Text (UTF-8):";
	}
}
