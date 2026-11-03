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
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A text editor with placeholder support and optional typed-value validation.</summary>
[ToolboxBitmap(typeof(TextBox))]
public partial class TextBoxEx : TextBox {
	private IValidator _validator;

	/// <summary>Creates an ordinary text editor; typed validation is opt-in.</summary>
	public TextBoxEx() {
		InitializeComponent();
	}

	/// <summary>Gets or sets the text shown when the editor has no input.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	public string PlaceHolderText {
		get {
			return _placeHolderTextExtender == null ? string.Empty : _placeHolderTextExtender.GetPlaceHolderText(this);
		}
		set {
			// Ordinary editors do not need a placeholder component or a native window.
			if (_placeHolderTextExtender == null) {
				if (value == string.Empty)
					return;

				_components = new Container();
				_placeHolderTextExtender = new PlaceHolderTextExtender(_components);
			}
			_placeHolderTextExtender.SetPlaceHolderText(this, value);
		}
	}

	/// <summary>Gets or sets whether accepted input is formatted when validation completes.</summary>
	[DefaultValue(false)]
	public bool FormatValue {
		get; set;
	}

	/// <summary>Gets or sets the borrowed validator used to convert and validate input.</summary>
	[DefaultValue(null)]
	public IValidator Validator {
		get {
			return _validator;
		}
		set {
			if (ReferenceEquals(_validator, value))
				return;

			if (_validator != null)
				_validator.Changed -= ValidatorChanged;

			_validator = value;
			if (_validator != null)
				_validator.Changed += ValidatorChanged;

			ApplyValidatorRules();
		}
	}

	/// <summary>Gets or sets the typed value, or ordinary text when no validator is assigned.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object Value {
		get {
			return IsValidValue(out var value) ? value : throw new ArgumentOutOfRangeException(nameof(Text));
		}
		set {
			Text = _validator == null
				? value?.ToString() ?? string.Empty
				: _validator.IsStringConversionSupported() ? _validator.ValueToString(value) : _validator.ValueToDisplayString(value);
		}
	}

	/// <summary>Restores a configured cue banner after Windows recreates the editor handle.</summary>
	protected override void OnHandleCreated(EventArgs args) {
		base.OnHandleCreated(args);

		if (_placeHolderTextExtender != null) {
			var placeHolderText = _placeHolderTextExtender.GetPlaceHolderText(this);
			if (!string.IsNullOrEmpty(placeHolderText))
				_placeHolderTextExtender.SetPlaceHolderText(this, placeHolderText);
		}
	}

	/// <summary>Validates input before focus leaves and optionally formats the accepted value.</summary>
	protected override void OnValidating(CancelEventArgs args) {
		base.OnValidating(args);

		if (!IsValidValue(out var value)) {
			args.Cancel = true;
			return;
		}

		if (FormatValue && _validator != null)
			Value = value;
	}

	/// <summary>Tries to convert the current input with the configured validator.</summary>
	public bool IsValidValue(out object convertedValue) {
		if (_validator != null)
			return _validator.IsValidObject(Text, out convertedValue);

		convertedValue = Text;
		return true;
	}

	/// <summary>Detaches borrowed validation events and releases owned designer components.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_validator != null) {
				_validator.Changed -= ValidatorChanged;
				_validator = null;
			}
			_components?.Dispose();
		}
		base.Dispose(disposing);
	}

	// Refresh derived editor rules without taking ownership of the validator.
	private void ValidatorChanged(object sender, EventArgs args) {
		ApplyValidatorRules();
	}

	/// <summary>Applies type-specific editor rules when the validator changes.</summary>
	protected virtual void ApplyValidatorRules() {
	}
}
