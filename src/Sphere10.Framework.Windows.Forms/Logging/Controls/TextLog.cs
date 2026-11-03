// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms.TextBoxes;

/// <summary>A multiline text surface with native text selection and append behavior.</summary>
public partial class TextLog : UserControlEx {
	/// <summary>Creates the designer-owned text editor without background resources.</summary>
	public TextLog() {
		InitializeComponent();
	}

	/// <summary>Gets or replaces the text displayed by the embedded editor.</summary>
	[Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override string Text {
		get {
			return _textBox == null ? base.Text : _textBox.Text;
		}
		set {
			if (_textBox == null)
				base.Text = value;
			else
				_textBox.Text = value;
		}
	}

	/// <summary>Gets or sets the first selected character using the native editor's range rules.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionStart {
		get {
			return _textBox.SelectionStart;
		}
		set {
			_textBox.SelectionStart = value;
		}
	}

	/// <summary>Gets or sets the selected character count using the native editor's range rules.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionLength {
		get {
			return _textBox.SelectionLength;
		}
		set {
			_textBox.SelectionLength = value;
		}
	}

	/// <summary>Gets the text currently selected in the embedded editor.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string SelectedText {
		get {
			return _textBox.SelectedText;
		}
	}

	/// <summary>Appends text using the existing native editor on its owning UI thread.</summary>
	public void AppendText(string text) {
		_textBox.AppendText(text);
	}

	/// <summary>Preserves the legacy parameterless no-op. Supply text through <see cref="AppendText(string)"/>.</summary>
	public void AppendText() {
	}

	private void TextBoxTextChanged(object sender, EventArgs args) {
		OnTextChanged(args);
	}
}
