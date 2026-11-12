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

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A TextBox that allows to set the type of value to edit, then you can use the Value property to read and write the specific type.
/// </summary>
public class MaskedTextBoxEx : System.Windows.Forms.MaskedTextBox {
	private bool _formatValue = false;
	private Sphere10.Framework.ComponentModel.IValidator _validator = null;

	/// <summary>
	/// Gets or sets a property to enable or disable the automatic format of the Text when validating the control
	/// </summary>
	[DefaultValue(false)]
	public bool FormatValue {
		get => _formatValue;
		set => _formatValue = value;
	}

	/// <summary>
	/// Gets or sets the Validator class useded to validate the value and convert the text when using the Value property.
	/// You can use the ApplyValidatorRules method to apply the settings of the Validator directly to the ComboBox, for example the list of values.
	/// </summary>
	[DefaultValue(null)]
	public Sphere10.Framework.ComponentModel.IValidator Validator {
		get => _validator;
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
			if (Validator != null) {
				if (Validator.IsStringConversionSupported())
					Text = Validator.ValueToString(value);
				else
					Text = Validator.ValueToDisplayString(value);
			} else {
				if (value == null)
					Text = "";
				else
					Text = value.ToString();
			}
		}
	}

	/// <summary>
	/// Check if the selected value is valid based on the current validator and returns the value.
	/// </summary>
	/// <param name="convertedValue"></param>
	/// <returns></returns>
	public bool IsValidValue(out object convertedValue) {
		if (Validator != null) {
			if (Validator.IsValidObject(this.Text, out convertedValue))
				return true;
			return false;
		}
		convertedValue = this.Text;
		return true;
	}

	#region Generic validation methods

	protected override void OnValidating(CancelEventArgs args) {
		base.OnValidating(args);

		object convertedValue;
		if (IsValidValue(out convertedValue) == false)
			args.Cancel = true;
		else {
			if (FormatValue && Validator != null) {
				if (Validator.IsStringConversionSupported())
					Text = Validator.ValueToString(convertedValue);
				else
					Text = Validator.ValueToDisplayString(convertedValue);
			}
		}
	}

	/// <summary>Detaches events from the borrowed validator before disposing the native editor.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_validator != null) {
				_validator.Changed -= ValidatorChanged;
				_validator = null;
			}
		}
		base.Dispose(disposing);
	}

	void ValidatorChanged(object sender, EventArgs args) => ApplyValidatorRules();

	/// <summary>
	/// Apply the current Validator rules. This method is automatically fired when the Validator change.
	/// </summary>
	protected virtual void ApplyValidatorRules() {
	}

	#endregion
}
