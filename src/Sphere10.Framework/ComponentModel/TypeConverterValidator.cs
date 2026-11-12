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

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// A string editor that use a TypeConverter for conversion.
/// </summary>
public class TypeConverterValidator : ValidatorBase {
	// TypeConverter
	private System.ComponentModel.TypeConverter _typeConverter;

	// Constructor
	/// <summary>
	/// Constructor. Initialize the Validator with a null TypeConverter.
	/// </summary>
	public TypeConverterValidator() => _typeConverter = null;

	/// <summary>
	/// Constructor. If the Type doesn't implements a TypeConverter no conversion is made.
	/// </summary>
	/// <param name="type"></param>
	public TypeConverterValidator(Type type)
		: base(type) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="type">Cannot be null.</param>
	/// <param name="typeConverter">Can be null to don't allow any conversion.</param>
	public TypeConverterValidator(Type type, System.ComponentModel.TypeConverter typeConverter)
		: base(type) {
		TypeConverter = typeConverter;
	}

	/// <summary>
	/// TypeConverter used for this type editor, cannot be null.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public System.ComponentModel.TypeConverter TypeConverter {
		get => _typeConverter;
		set {
			if (_typeConverter != value) {
				_typeConverter = value;
				OnLoadingTypeConverter();
				OnChanged(EventArgs.Empty);
			}
		}
	}

	// Conversion
	/// <summary>
	/// Returns true if string conversion is suported. AllowStringConversion must be true and the current Validator must support string conversion.
	/// </summary>
	public override bool IsStringConversionSupported() {
		if (typeof(string).IsAssignableFrom(ValueType))
			return AllowStringConversion;

		if (_typeConverter != null)
			return AllowStringConversion && _typeConverter.CanConvertFrom(typeof(string)) && _typeConverter.CanConvertTo(typeof(string));
		return base.AllowStringConversion;
	}

	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	/// <param name="args"></param>
	protected override void OnConvertingObjectToValue(ConvertingObjectEventArgs args) {
		base.OnConvertingObjectToValue(args);

		if (args.ConvertingStatus == ConvertingStatus.Error)
			throw new ApplicationException("Invalid conversion");
		if (args.ConvertingStatus == ConvertingStatus.Completed)
			return;

		if (args.Value == null) {
		} else if (args.Value is string) {
			//è importante fare prima il caso stringa per gestire correttamente il null
			var temporary = (string)args.Value;
			if (IsNullString(temporary))
				args.Value = null;
			else if (args.DestinationType.IsAssignableFrom(args.Value.GetType())) {
				//se la stringa non è nulla e il tipo di destinazione è sempre una string allora non faccio nessuna conversione
			} else if (IsStringConversionSupported())
				args.Value = _typeConverter.ConvertFromString(null, CultureInfo, temporary);
			else
				throw new ApplicationException("String conversion not supported for this type of Validator.");
		} else if (args.DestinationType.IsAssignableFrom(args.Value.GetType())) {
		} else if (_typeConverter != null) {
			// For some reason string converter does not allow converting from
			// double to string. So here is just override with simple if statemenet
			if (_typeConverter is StringConverter)
				args.Value = TypeChanger.ChangeType<string>(args.Value);
			else
				// otherwise just do normal conversion
				args.Value = _typeConverter.ConvertFrom(null, CultureInfo, args.Value);
		}
	}

	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	/// <param name="args"></param>
	protected override void OnConvertingValueToObject(ConvertingObjectEventArgs args) {
		base.OnConvertingValueToObject(args);

		if (args.ConvertingStatus == ConvertingStatus.Error)
			throw new ApplicationException("Invalid conversion");
		if (args.ConvertingStatus == ConvertingStatus.Completed)
			return;

		if (args.Value == null) {
		} else if (args.DestinationType.IsAssignableFrom(args.Value.GetType())) {
		} else if (args.DestinationType == typeof(string) && IsStringConversionSupported() == false)
			throw new ApplicationException("String conversion not supported for this type of Validator.");
		else if (_typeConverter != null)
			args.Value = _typeConverter.ConvertTo(null, CultureInfo, args.Value, args.DestinationType);
	}

	protected override void OnLoadingValueType() {
		base.OnLoadingValueType();

		if (ValueType != null)
			TypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(ValueType);
		else
			TypeConverter = null;
	}

	private void OnLoadingTypeConverter() {
		StandardValues = null;
		StandardValuesExclusive = false;

		//Populate properties using TypeConverter
		if (_typeConverter != null) {
			StandardValues = _typeConverter.GetStandardValues();
			if (StandardValues != null && StandardValues.Count > 0)
				StandardValuesExclusive = _typeConverter.GetStandardValuesExclusive();
			else
				StandardValuesExclusive = false;
		}
	}
}
