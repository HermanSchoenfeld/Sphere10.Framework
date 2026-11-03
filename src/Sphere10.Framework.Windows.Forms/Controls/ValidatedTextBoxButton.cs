// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A textBox with a button on the right.
/// </summary>
public partial class ValidatedTextBoxButton : EditableControl {
	/// <summary>
	/// Fired when showing the drop down
	/// </summary>
	public event EventHandler DialogOpen;

	/// <summary>
	/// Fired when closing the dropdown
	/// </summary>
	public event EventHandler DialogClosed;

	private Sphere10.Framework.ComponentModel.IValidator _validator = null;
	private object _value = null;

	/// <summary>
	/// Constructor
	/// </summary>
	public ValidatedTextBoxButton() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		_browseButton.BackColor = Color.FromKnownColor(KnownColor.Control);
		_textBox.TextChanged += new EventHandler(TxtBoxTextChanged);

		SetContentAndButtonLocation(_textBox, _browseButton);
	}

	/// <summary>
	/// Gets or sets the Validator class useded to validate the value and convert the text when using the Value property.
	/// You can use the ApplyValidatorRules method to apply the settings of the Validator directly to the ComboBox, for example the list of values.
	/// </summary>
	[DefaultValue(null)]
	public Sphere10.Framework.ComponentModel.IValidator Validator {
		get {
			return _validator;
		}
		set {
			if (_validator != value) {
				if (_validator != null)
					_validator.Changed -= ValidatorChanged;

				_validator = value;
				if (_validator != null)
					_validator.Changed += ValidatorChanged;
				ApplyValidatorRules();
			}
		}
	}

	/// <summary>
	/// The button in the right of the editor
	/// </summary>
	public System.Windows.Forms.Button Button {
		get {
			return _browseButton;
		}
	}

	public System.Windows.Forms.TextBox TextBox {
		get {
			return _textBox;
		}
	}

	/// <summary>
	/// Gets or sets the typed value for the control, using the Validator class.
	/// If the Validator is ull the Text property is used.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object Value {
		get {
			object convertedValue;
			if (IsValidValue(out convertedValue))
				return convertedValue;
			throw new ArgumentOutOfRangeException("Text");
		}
		set {
			var hadValue = IsValidValue(out var previousValue);
			using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);

			if (Validator != null) {
				if (Validator.IsStringConversionSupported())
					TextBox.Text = Validator.ValueToString(value);
				else
					TextBox.Text = Validator.ValueToDisplayString(value);
			} else {
				if (value == null)
					TextBox.Text = "";
				else
					TextBox.Text = value.ToString();
			}

			_value = value;
			if (!hadValue || !Equals(previousValue, value))
				NotifyStateChanged();
		}
	}

	/// <summary>
	/// Show the dialog
	/// </summary>
	public virtual void ShowDialog() {
		OnDialogOpen(EventArgs.Empty);

		OnDialogClosed(EventArgs.Empty);
	}

	/// <summary>
	/// Check if the selected value is valid based on the current validator and returns the value.
	/// </summary>
	/// <param name="convertedValue"></param>
	/// <returns></returns>
	public bool IsValidValue(out object convertedValue) {
		if (Validator != null) {
			if (_value != null) {
				convertedValue = _value;
				return true;
			}
			if (Validator.IsValidObject(TextBox.Text, out _value)) {
				convertedValue = _value;
				return true;
			}
			convertedValue = null;
			return false;
		}
		convertedValue = _textBox.Text;
		return true;
	}

	/// <summary>
	/// Apply the current Validator rules. This method is automatically fired when the Validator change.
	/// </summary>
	protected virtual void ApplyValidatorRules() {
	}

	/// <summary>The typed value owns its notification after the text cache has been invalidated or replaced.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) {
		return false;
	}

	protected override void OnValidating(CancelEventArgs args) {
		base.OnValidating(args);

		object convertedValue;
		if (IsValidValue(out convertedValue) == false)
			args.Cancel = true;
	}

	/// <summary>
	/// Fired when showing the drop down
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnDialogOpen(EventArgs args) {
		if (DialogOpen != null)
			DialogOpen(this, args);
	}

	/// <summary>
	/// Fired when closing the dropdown
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnDialogClosed(EventArgs args) {
		if (DialogClosed != null)
			DialogClosed(this, args);
	}

	protected override void OnBorderStyleChanged(EventArgs args) {
		base.OnBorderStyleChanged(args);
		SetContentAndButtonLocation(_textBox, _browseButton);
	}

	protected override void OnBackColorChanged(EventArgs args) {
		base.OnBackColorChanged(args);

		if (_textBox != null) {
			if (BackColor == Color.Transparent)
				_textBox.BackColor = Color.FromKnownColor(KnownColor.Window);
			else
				_textBox.BackColor = BackColor;
		}
	}

	protected override void OnForeColorChanged(EventArgs args) {
		base.OnForeColorChanged(args);

		if (_textBox != null)
			_textBox.ForeColor = ForeColor;
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_validator != null) {
				_validator.Changed -= ValidatorChanged;
				_validator = null;
			}
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	private void DownClick(object sender, System.EventArgs args) {
		ShowDialog();
	}

	void ValidatorChanged(object sender, EventArgs args) {
		ApplyValidatorRules();
	}

	// Invalidate the typed cache before observers read the new text through Value.
	void TxtBoxTextChanged(object sender, EventArgs args) {
		_value = null;
		NotifyStateChanged();
	}
}
