// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Sphere10.Framework.Windows.Forms;

[ToolboxItem(false)]
public abstract class NumericBoxBase<T> : TextBoxEx
	where T : struct {
	public event EventHandler ValueChanged {
		add {
			this.Events.AddHandler(_valueChangedEventKey, value);
		}
		remove {
			this.Events.RemoveHandler(_valueChangedEventKey, value);
		}
	}

	private const string _defaultFormatString = "G";
	private const string _defaultNullText = "";
	private T? _value;
	private static readonly object _valueChangedEventKey = new object();
	private int _eventActive = 0;
	private string _lastValidText = string.Empty;
	private int _lastValidSelectionStart = 0;
	private int _lastValidSelectionLength = 0;

	public NumericBoxBase() {
		this.NullText = _defaultNullText;
		this.FormatString = _defaultFormatString;
	}

	[DefaultValue(null)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	[Category("Appearance")]
	[Bindable(BindableSupport.Yes)]
	public new T? Value {
		get => this._value;
		set {
			var previousText = Text;
			this._value = value;
			this.Text = value.HasValue ? this.ToText(value.Value) : this.NullText;
			// A normal text change already publishes the parsed value; unchanged or suppressed text still needs the setter event.
			if (Text == previousText || _eventActive > 0)
				this.OnValueChanged(EventArgs.Empty);
		}
	}

	[Browsable(false)]
	public override string Text {
		get => base.Text;
		set => base.Text = value;
	}

	[DefaultValue(_defaultNullText)]
	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public virtual string NullText {
		get; set;
	}

	[DefaultValue(_defaultFormatString)]
	[Category("Appearance")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public virtual string FormatString {
		get; set;
	}

	protected override void OnTextChanged(EventArgs e) {
		if (_eventActive > 0)
			return;

		try {
			this._eventActive++;
			// See if new value is a valid Money value
			if (!IsValid(this.Text)) {
				this.GoToLastValidText();
				return;
			}

			base.OnTextChanged(e);

			if (!IsValid(this.Text)) {
				this.GoToLastValidText();
				return;
			} else
				StoreAsValidText();

			Debug.Print("Text   : {0}", this.Text);
			this.OnValueChanged(e);
		} finally {
			this._eventActive--;
		}
	}

	public void OnValueChanged(EventArgs e) {
		var handler = base.Events[_valueChangedEventKey] as EventHandler;
		if (handler != null)
			handler(this, e);

		Debug.Print("New value: {0}", this.Value);
	}

	protected override void OnLostFocus(EventArgs e) {
		base.OnLostFocus(e);
		if (this._eventActive > 0)
			return;

		try {
			this._eventActive++;

			this.Text = this.Value.HasValue ? this.ToText(this.Value.Value) : this.NullText;
			this.StoreAsValidText();
		} finally {
			this._eventActive--;
		}
	}

	protected override void OnKeyUp(System.Windows.Forms.KeyEventArgs e) {
		if (this._eventActive > 0)
			return;

		try {
			this._eventActive++;
			if (this.IsValid(this.Text))
				this.StoreAsValidText();
			else
				this.GoToLastValidText();
		} finally {
			this._eventActive--;
		}
	}

	public bool IsValid(string text) {
		if (text == this.NullText)
			return true;

		if (!this.CanParse(this.Text))
			return false;

		return true;
	}

	protected abstract string ToText(T value);

	private void GoToLastValidText() {
		this.Text = this._lastValidText;
		this.SelectionStart = this._lastValidSelectionStart;
		this.SelectionLength = this._lastValidSelectionLength;
	}

	private void StoreAsValidText() {
		this._value = this.CanParse(this.Text)
			? this.DoParse(this.Text)
			: null;

		this._lastValidText = this.Text;
		this._lastValidSelectionStart = this.SelectionStart;
		this._lastValidSelectionLength = this.SelectionLength;
	}

	protected abstract bool CanParse(string text);

	protected abstract T? DoParse(string text);
}
