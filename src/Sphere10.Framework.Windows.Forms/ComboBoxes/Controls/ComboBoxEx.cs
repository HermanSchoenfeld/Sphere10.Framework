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
using System.Windows.Forms;
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A combo editor with placeholder support and optional typed-value validation.</summary>
public partial class ComboBoxEx : ComboBox {
	private IValidator _validator;

	/// <summary>Creates an ordinary combo editor; typed validation is opt-in.</summary>
	public ComboBoxEx() {
		InitializeComponent();
	}

	/// <summary>Gets or sets the text shown when the editor has no input.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	public string PlaceHolderText {
		get {
			return _placeHolderTextExtender.GetPlaceHolderText(this);
		}
		set {
			_placeHolderTextExtender.SetPlaceHolderText(this, value);
		}
	}

	/// <summary>Gets or sets whether accepted input is formatted when validation completes.</summary>
	[DefaultValue(false)]
	public bool FormatValue {
		get; set;
	}

	/// <summary>Gets or sets the borrowed validator and applies its standard values.</summary>
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

	/// <summary>Gets or sets the selected typed value, using display formatting when configured.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object Value {
		get {
			if (IsValidValue(out var value))
				return value;
			throw new ArgumentOutOfRangeException(nameof(Text));
		}
		set {
			Text = _validator == null ? value?.ToString() ?? string.Empty : _validator.ValueToDisplayString(value);
		}
	}

	/// <summary>Validates selection or input and optionally formats the accepted value.</summary>
	protected override void OnValidating(CancelEventArgs args) {
		base.OnValidating(args);

		if (!IsValidValue(out var value)) {
			args.Cancel = true;
			return;
		}

		if (FormatValue && _validator != null)
			Text = _validator.ValueToDisplayString(value);
	}

	/// <summary>Tries the bound value, selected item, then text in that precedence order.</summary>
	public bool IsValidValue(out object convertedValue) {
		var value = SelectedValue ?? SelectedItem ?? Text;
		if (_validator != null)
			return _validator.IsValidObject(value, out convertedValue);

		convertedValue = value;
		return true;
	}

	/// <summary>Uses the same validator for dropdown item captions and the selected value.</summary>
	protected override void OnFormat(ListControlConvertEventArgs args) {
		base.OnFormat(args);
		if (args.DesiredType == typeof(string) && _validator != null)
			args.Value = _validator.ValueToDisplayString(args.ListItem);
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

	// Apply changed standard values only while this editor owns the subscription.
	private void ValidatorChanged(object sender, EventArgs args) {
		ApplyValidatorRules();
	}

	/// <summary>Reloads ordered standard values and selects the validator's text-entry mode.</summary>
	protected virtual void ApplyValidatorRules() {
		Items.Clear();
		if (_validator?.StandardValues == null)
			return;

		foreach (var value in _validator.StandardValues)
			Items.Add(value);

		DropDownStyle = _validator.IsStringConversionSupported() ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
	}
}
