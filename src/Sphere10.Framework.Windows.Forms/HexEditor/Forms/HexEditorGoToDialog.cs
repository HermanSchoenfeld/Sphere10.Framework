// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Asks for a hexadecimal or decimal offset within a document.</summary>
public partial class HexEditorGoToDialog : Form {
	private long _offset;
	private long _maximumOffset;

	public HexEditorGoToDialog() {
		InitializeComponent();
		_maximumOffset = long.MaxValue;
		ShowOffset();
	}

	/// <summary>The offset to prefill, and the accepted offset after OK.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public long Offset {
		get => _offset;
		set {
			Guard.ArgumentNotNegative(value, nameof(value));
			_offset = value;
			ShowOffset();
		}
	}

	/// <summary>The largest offset accepted; larger input is rejected with a message.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public long MaximumOffset {
		get => _maximumOffset;
		set {
			Guard.ArgumentNotNegative(value, nameof(value));
			_maximumOffset = value;
		}
	}

	/// <summary>Whether the input is interpreted as hexadecimal (the default) rather than decimal.</summary>
	[Category("Behavior"), DefaultValue(true)]
	public bool IsHexadecimal {
		get => _hexRadioButton.Checked;
		set {
			_hexRadioButton.Checked = value;
			_decimalRadioButton.Checked = !value;
		}
	}

	private void OkButtonClicked(object sender, EventArgs e) {
		if (!HexEditorViewport.TryParseOffset(_offsetTextBox.Text, IsHexadecimal, out var offset)) {
			_errorLabel.Text = IsHexadecimal ? "Enter a hexadecimal offset such as 1F00 or 0x1F00." : "Enter a non-negative decimal offset.";
			return;
		}
		if (offset > _maximumOffset) {
			_errorLabel.Text = $"The offset must not exceed {(IsHexadecimal ? "0x" + _maximumOffset.ToString("X", CultureInfo.InvariantCulture) : _maximumOffset.ToString("N0", CultureInfo.CurrentCulture))}.";
			return;
		}
		_offset = offset;
		DialogResult = DialogResult.OK;
		Close();
	}

	// Switching the radix reformats the prefilled offset so the text stays consistent with the interpretation.
	private void RadixChanged(object sender, EventArgs e) {
		if (HexEditorViewport.TryParseOffset(_offsetTextBox.Text, !IsHexadecimal, out var current))
			_offset = current;
		ShowOffset();
		_errorLabel.Text = string.Empty;
	}

	private void ShowOffset() {
		_offsetTextBox.Text = IsHexadecimal ? _offset.ToString("X", CultureInfo.InvariantCulture) : _offset.ToString(CultureInfo.InvariantCulture);
		_offsetTextBox.SelectAll();
	}
}
