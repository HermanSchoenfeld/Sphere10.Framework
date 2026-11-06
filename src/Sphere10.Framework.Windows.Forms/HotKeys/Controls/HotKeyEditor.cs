// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits a global shortcut as a regular key plus optional Ctrl, Alt and Shift modifiers, matching <see cref="GlobalHotKey"/>'s accepted combinations.</summary>
public partial class HotKeyEditor : UserControlEx {
	/// <summary>Raised after the user changes the shortcut.</summary>
	public event EventHandler ShortcutChanged;

	private Keys _shortcut = Keys.None;

	public HotKeyEditor() {
		InitializeComponent();
		_keyComboBox.AllowEmptyOption = true;
		_keyComboBox.EmptyOptionText = "(none)";
		_keyComboBox.Filter = key => IsEditableKey((Keys)key);
		_keyComboBox.EnumType = typeof(Keys);
	}

	/// <summary>The edited shortcut; <see cref="Keys.None"/> means no shortcut.</summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Keys Shortcut {
		get => _shortcut;
		set {
			_shortcut = value;
			if (Loaded)
				Refresh();
		}
	}

	/// <summary>Whether a key accepted by this editor appears in the key list.</summary>
	public static bool IsEditableKey(Keys key) {
		var keyCode = key & Keys.KeyCode;
		if (keyCode != key || keyCode == Keys.None)
			return false;
		if (keyCode >= Keys.A && keyCode <= Keys.Z || keyCode >= Keys.D0 && keyCode <= Keys.D9 || keyCode >= Keys.NumPad0 && keyCode <= Keys.NumPad9)
			return true;
		if (keyCode >= Keys.F1 && keyCode <= Keys.F11)
			return true;
		return keyCode.IsIn(Keys.Space, Keys.Insert, Keys.Delete, Keys.Home, Keys.End, Keys.PageUp, Keys.PageDown, Keys.Up, Keys.Down, Keys.Left, Keys.Right,
			Keys.Add, Keys.Subtract, Keys.Multiply, Keys.Divide, Keys.Decimal, Keys.Pause, Keys.Scroll, Keys.PrintScreen,
			Keys.OemMinus, Keys.Oemplus, Keys.Oemcomma, Keys.OemPeriod, Keys.OemQuestion, Keys.Oemtilde, Keys.OemOpenBrackets, Keys.OemCloseBrackets, Keys.OemPipe, Keys.OemSemicolon, Keys.OemQuotes);
	}

	/// <summary>Formats a shortcut as "Ctrl+Alt+Key" text for captions.</summary>
	public static string Describe(Keys shortcut) {
		if (shortcut == Keys.None)
			return "None";
		var text = string.Empty;
		if ((shortcut & Keys.Control) != 0)
			text += "Ctrl+";
		if ((shortcut & Keys.Alt) != 0)
			text += "Alt+";
		if ((shortcut & Keys.Shift) != 0)
			text += "Shift+";
		return text + (shortcut & Keys.KeyCode);
	}

	protected override void CopyModelToUI() {
		_controlCheckBox.Checked = (_shortcut & Keys.Control) != 0;
		_altCheckBox.Checked = (_shortcut & Keys.Alt) != 0;
		_shiftCheckBox.Checked = (_shortcut & Keys.Shift) != 0;
		var keyCode = _shortcut & Keys.KeyCode;
		_keyComboBox.SelectedEnum = IsEditableKey(keyCode) ? keyCode : null;
	}

	protected override void CopyUIToModel() {
		var shortcut = _keyComboBox.SelectedEnum is Keys key ? key : Keys.None;
		if (shortcut == Keys.None) {
			_shortcut = Keys.None;
			return;
		}
		if (_controlCheckBox.Checked)
			shortcut |= Keys.Control;
		if (_altCheckBox.Checked)
			shortcut |= Keys.Alt;
		if (_shiftCheckBox.Checked)
			shortcut |= Keys.Shift;
		_shortcut = shortcut;
	}

	protected override void OnStateChanged() {
		base.OnStateChanged();
		ShortcutChanged?.Invoke(this, EventArgs.Empty);
	}
}
