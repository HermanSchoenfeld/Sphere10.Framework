// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// A base class to support value conversion and validation. This class is used if no conversion is required or as a base class for specialized validator.
/// Naming Legend:
/// Object = an object not yet converted for the current validator,
/// Value = an object already converted and valid for the current validator,
/// String = a string that can be used for conversion to and from Value,
/// DisplayString = a string representation of the Value
/// </summary>
public abstract class ValidatorBase : Component, IValidator {
	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	public event ConvertingObjectEventHandler ConvertingObjectToValue {
		add {
			_convertingObjectToValue += value;
		}
		remove {
			_convertingObjectToValue -= value;
		}
	}

	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	public event ConvertingObjectEventHandler ConvertingValueToObject {
		add {
			_convertingValueToObject += value;
		}
		remove {
			_convertingValueToObject -= value;
		}
	}

	/// <summary>
	/// Fired when converting a value to a display string. Called from method ValueToDisplayString
	/// </summary>
	public event ConvertingObjectEventHandler ConvertingValueToDisplayString {
		add {
			_convertingValueToDisplayString += value;
		}
		remove {
			_convertingValueToDisplayString -= value;
		}
	}

	/// <summary>
	/// Fired when one of the properties of the Validator change.
	/// </summary>
	public event EventHandler Changed {
		add {
			_changed += value;
		}
		remove {
			_changed -= value;
		}
	}

	// Null
	private bool _allowNull;
	private string _nullString;
	private string _nullDisplayString;
	private bool _allowStringConversion = true;

	// Events
	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	private ConvertingObjectEventHandler _convertingObjectToValue;

	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	private ConvertingObjectEventHandler _convertingValueToObject;

	/// <summary>
	/// Fired when converting a value to a display string. Called from method ValueToDisplayString
	/// </summary>
	private ConvertingObjectEventHandler _convertingValueToDisplayString;

	// Maximum/Minimum
	private object _minimumValue = null;
	private object _maximumValue = null;

	// Type
	private Type _valueType;

	// Default Value
	private object _defaultValue;

	// Standard Value Exclusive
	private ICollection _standardValues;
	private bool _standardValuesExclusive;

	// Culture
	private CultureInfo _cultureInfo = null;
	private EventHandler _changed;

	// Constructors
	/// <summary>
	/// Constructor. Initialize the class using a null type.
	/// </summary>
	protected ValidatorBase() => ValueType = null;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="type">The type used to validate the values. If null no validation is made.</param>
	protected ValidatorBase(Type type) => ValueType = type;

	/// <summary>
	/// True to allow null object value or NullString string Value
	/// </summary>
	[DefaultValue(true), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool AllowNull {
		get => _allowNull;
		set {
			if (_allowNull != value) {
				_allowNull = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Null string representation. A string is null when is null or when is equals to this string. Default is empty string.
	/// Used by ValueToString and StringToValue
	/// </summary>
	[DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string NullString {
		get => _nullString;
		set {
			if (_nullString != value) {
				_nullString = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Null string representation. Default is empty string.
	/// Used by ValueToDisplayString
	/// </summary>
	[DefaultValue(""), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string NullDisplayString {
		get => _nullDisplayString;
		set {
			if (_nullDisplayString != value) {
				_nullDisplayString = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Gets or Sets if the string conversion is allowed.
	/// </summary>
	[DefaultValue(true), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool AllowStringConversion {
		get => _allowStringConversion;
		set {
			if (_allowStringConversion != value) {
				_allowStringConversion = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Minimum value allowed. If null no check is performed. The value must derive from IComparable interface to use Minimum or Maximum feature.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object MinimumValue {
		get => _minimumValue;
		set {
			if (_minimumValue != value) {
				_minimumValue = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Maximum value allowed. If null no check is performed. The value must derive from IComparable interface to use Minimum or Maximum feature.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object MaximumValue {
		get => _maximumValue;
		set {
			if (_maximumValue != value) {
				_maximumValue = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Type allowed for the current editor. Cannot be null.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Type ValueType {
		get => _valueType;
		set {
			if (_valueType != value) {
				_valueType = value;
				OnLoadingValueType();
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Set the ValueType property using a string value, usually used by designer code generation.
	/// The designer for this type is configured to use only this property.
	/// </summary>
	[DefaultValue(""), Browsable(true)]
	public string ValueTypeName {
		get {
			if (ValueType == null)
				return string.Empty;
			return ValueType.AssemblyQualifiedName;
		}
		set {
			if (value == null || value.Trim().Length == 0)
				ValueType = null;
			else
				ValueType = Type.GetType(value, true, true);
		}
	}

	/// <summary>
	/// Default value for this editor, usually is the default value for the specified type.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public object DefaultValue {
		get => _defaultValue;
		set {
			if (_defaultValue != value) {
				_defaultValue = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// A list of values that this editor can support. If StandardValuesExclusive is true then the editor can only support one of these values.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ICollection StandardValues {
		get => _standardValues;
		set {
			if (_standardValues != value) {
				_standardValues = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// If StandardValuesExclusive is true then the editor can only support the list specified in StandardValues.
	/// </summary>
	[DefaultValue(false), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool StandardValuesExclusive {
		get => _standardValuesExclusive;
		set {
			if (_standardValuesExclusive != value) {
				_standardValuesExclusive = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Culture for conversion. If null the default user culture is used. Default is null.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CultureInfo CultureInfo {
		get => _cultureInfo;
		set {
			if (_cultureInfo != value) {
				_cultureInfo = value;
				OnChanged(EventArgs.Empty);
			}
		}
	}

	// Conversion
	/// <summary>
	/// Convert an object according to the current ValueType of the validator
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public object ObjectToValue(object input) {
		// Resolve the destination type before invoking the conversion pipeline.
		var destinationType = ValueType;
		if (destinationType == null && input != null)
			destinationType = input.GetType();

		// Run conversion callbacks, then validate the resulting value.
		var converting = new ConvertingObjectEventArgs(input, destinationType);
		OnConvertingObjectToValue(converting);
		if (converting.ConvertingStatus == ConvertingStatus.Error)
			throw new ValueConversionException(ValueTypeName, ObjectToStringForError(converting.Value));

		if (IsValidValue(converting.Value))
			return converting.Value;
		throw new ValueConversionException(ValueTypeName, ObjectToStringForError(converting.Value));
	}

	/// <summary>
	/// Converts a string to an object according to the type of the string editor
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public object StringToValue(string str) => ObjectToValue(str);

	/// <summary>
	/// Convert a value according to the current ValueType to an object with the Type specified. Throw an exception on error.
	/// </summary>
	/// <param name="value"></param>
	/// <param name="returnObjectType"></param>
	/// <returns></returns>
	public object ValueToObject(object value, Type returnObjectType) {
		if (returnObjectType == null)
			throw new ApplicationException("Invalid parameter returnObjectType cannot be null");

		// Run the conversion pipeline for the requested destination type.
		var converting = new ConvertingObjectEventArgs(value, returnObjectType);
		OnConvertingValueToObject(converting);
		if (converting.ConvertingStatus == ConvertingStatus.Error)
			throw new ValueConversionException(returnObjectType.Name, ObjectToStringForError(converting.Value));

		if (converting.Value == null)
			return null;
		if (converting.DestinationType.IsAssignableFrom(converting.Value.GetType()))
			return converting.Value;
		throw new ValueConversionException(returnObjectType.Name, ObjectToStringForError(converting.Value));
	}

	/// <summary>
	/// Converts a value object to a string representation
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public string ValueToString(object value) {
		var temporary = ValueToObject(value, typeof(string));
		if (temporary == null)
			return null;
		return (string)temporary;
	}

	// DisplayString
	/// <summary>
	/// Converts a value valid for this validator valuetype to a string representation. The string cannot be used for conversion.
	/// If the validator support string conversion this method simply call ValueToString otherwise call Value.ToString()
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public virtual string ValueToDisplayString(object value) {
		// Allow display callbacks to supply a label before applying the fallback.
		var converting = new ConvertingObjectEventArgs(value, typeof(string));
		OnConvertingValueToDisplayString(converting);
		if (converting.ConvertingStatus == ConvertingStatus.Error)
			throw new ValueConversionException("display String", ObjectToStringForError(converting.Value));

		if (converting.Value == null)
			return NullDisplayString;
		if (converting.Value is string)
			return (string)converting.Value;
		throw new ValueConversionException("display String", ObjectToStringForError(converting.Value));
	}

	/// <summary>
	/// Returns true if the object is valid for this type of validator, using conversion functions.
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public bool IsValidObject(object input) => IsValidObject(input, out _);

	/// <summary>
	/// Returns true if the object is valid for this type of validator, using conversion functions. Returns as parameter the value converted.
	/// </summary>
	/// <param name="input"></param>
	/// <param name="valueConverted"></param>
	/// <returns></returns>
	public bool IsValidObject(object input, out object valueConverted) {
		// Preserve a completed conversion even when subsequent validation rejects the value.
		object converted = null;
		var valid = Tools.Exceptions.TryOrDefault(() => {
			converted = ObjectToValue(input);
			return IsValidValue(converted);
		}, false);
		valueConverted = converted;
		return valid;
	}

	/// <summary>
	/// Returns true if the string is valid for this type of editor, using string conversion function.
	/// </summary>
	/// <param name="strValue"></param>
	/// <returns></returns>
	public bool IsValidString(string strValue) => IsValidString(strValue, out _);

	/// <summary>
	/// Returns true if the string is valid for this type of editor, using string conversion function. Returns as out parameter the object converted.
	/// </summary>
	/// <param name="strValue"></param>
	/// <param name="valueConverted"></param>
	/// <returns></returns>
	public bool IsValidString(string strValue, out object valueConverted) => IsValidObject(strValue, out valueConverted);

	// Validating
	/// <summary>
	/// Returns true if the value is valid for this type of editor without any conversion.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public bool IsValidValue(object value) {
		return Tools.Exceptions.TryOrDefault(() => {
			if (IsInStandardValues(value))
				return true;

			if (value == null) {
				if (AllowNull)
					return true;
				return false;
			}

			if (_standardValuesExclusive)
				return false;

			// Check the configured bounds before requiring a compatible runtime type.
			if (_maximumValue != null) {
				var maximum = (IComparable)_maximumValue;
				if (maximum.CompareTo(value) < 0)
					return false;
			}

			if (_minimumValue != null) {
				var minimum = (IComparable)_minimumValue;
				if (minimum.CompareTo(value) > 0)
					return false;
			}

			if (ValueType != null)
				return ValueType.IsAssignableFrom(value.GetType());
			return true;
		}, false);
	}

	/// <summary>
	/// Returns true if the value specified is presents in the list StandardValues.
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public virtual bool IsInStandardValues(object value) {
		if (_standardValues == null)
			return false;

		foreach (var standardValue in _standardValues) {
			if ((standardValue == null && value == null) || (standardValue != null && standardValue.Equals(value)))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Returns the standard values at the specified index. If StandardValues support IList use simple the indexer method otherwise loop troght the collection.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public virtual object StandardValueAtIndex(int index) {
		if (_standardValues == null)
			throw new ApplicationException("StandardValues is null");

		// Prefer indexed access when the configured collection supplies it.
		var list = _standardValues as IList;
		if (list != null)
			return list[index];

		// Otherwise enumerate to the requested position.
		var currentIndex = 0;
		foreach (var value in _standardValues) {
			if (currentIndex == index)
				return value;

			currentIndex++;
		}

		throw new ApplicationException("Invalid Index");
	}

	/// <summary>
	/// Returns the index of the specified standard value. -1 if not found. If StandardValues support IList use simple the indexer method otherwise loop troght the collection.
	/// </summary>
	/// <param name="standardValue"></param>
	/// <returns></returns>
	public virtual int StandardValuesIndexOf(object standardValue) {
		if (_standardValues == null)
			throw new ApplicationException("StandardValues is null");

		// Prefer the configured list lookup when it is available.
		var list = _standardValues as IList;
		if (list != null)
			return list.IndexOf(standardValue);

		// Otherwise preserve the collection's enumeration order for the index.
		var currentIndex = 0;
		foreach (var value in _standardValues) {
			if (value == null && standardValue == null)
				return currentIndex;
			if (value != null) {
				if (value.Equals(standardValue))
					return currentIndex;
			}

			currentIndex++;
		}

		return -1;
	}

	/// <summary>
	/// Returns true if the string is null or if is equals to the NullString
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public virtual bool IsNullString(string str) => str == null || str == _nullString;

	/// <summary>
	/// Returns true if string conversion is suported. AllowStringConversion must be true and the current Validator must support string conversion.
	/// </summary>
	public virtual bool IsStringConversionSupported() => (AllowStringConversion && typeof(string) == ValueType) || ValueType == null;

	// Error string
	/// <summary>
	/// Returns a string used for error description for a specified object. Usually used when printing the object for the error message when there is a conversion error.
	/// </summary>
	/// <param name="val"></param>
	public virtual string ObjectToStringForError(object val) => val == null ? "<null>" : Tools.Exceptions.TryOrDefault(val.ToString, "<object>");

	// Component methods
	/// <summary>Delegates component resource cleanup to the base implementation.</summary>
	protected override void Dispose(bool disposing) => base.Dispose(disposing);

	/// <summary>Restores validation defaults for the selected type without changing enum declaration order.</summary>
	protected virtual void OnLoadingValueType() {
		// Reset validation defaults whenever the value type changes.
		if (ValueType != null) {
			if (ValueType.IsValueType) {
				_allowNull = false;
				if (ValueType.IsEnum) {
					// The first declared field is the editor default, rather than the numerically smallest enum value.
					var fields = ValueType.GetFields(BindingFlags.Public | BindingFlags.Static);
					_defaultValue = fields[0].GetValue(null);
				} else
					_defaultValue = Tools.Object.Create(ValueType);
			} else {
				_defaultValue = null;
				_allowNull = true;
			}

			_standardValues = null;
			_standardValuesExclusive = false;
			_maximumValue = null;
			_minimumValue = null;
			_nullString = "";
			_nullDisplayString = "";
		} else {
			_allowNull = true;
			_defaultValue = null;
			_standardValues = null;
			_standardValuesExclusive = false;
			_maximumValue = null;
			_minimumValue = null;
			_nullString = "";
			_nullDisplayString = "";
		}
	}

	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnConvertingObjectToValue(ConvertingObjectEventArgs args) {
		if (_convertingObjectToValue != null)
			_convertingObjectToValue(this, args);

		if (args.ConvertingStatus == ConvertingStatus.Error)
			throw new ValueConversionException(args.DestinationType.Name, ObjectToStringForError(args.Value));
		if (args.ConvertingStatus == ConvertingStatus.Completed)
			return;

		if (args.Value is string) {
			//è importante fare prima il caso stringa per gestire correttamente il null
			var temporary = (string)args.Value;
			if (IsNullString(temporary))
				args.Value = null;
		}
	}

	/// <summary>
	/// Fired when converting a object to the value specified. Called from method ObjectToValue and IsValidObject
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnConvertingValueToObject(ConvertingObjectEventArgs args) {
		if (_convertingValueToObject != null)
			_convertingValueToObject(this, args);

		if (args.ConvertingStatus == ConvertingStatus.Error)
			throw new ValueConversionException(args.DestinationType.Name, ObjectToStringForError(args.Value));
		if (args.ConvertingStatus == ConvertingStatus.Completed)
			return;
	}

	/// <summary>
	/// Fired when converting a value to a display string. Called from method ValueToDisplayString
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnConvertingValueToDisplayString(ConvertingObjectEventArgs args) {
		if (_convertingValueToDisplayString != null)
			_convertingValueToDisplayString(this, args);

		if (args.ConvertingStatus == ConvertingStatus.Error)
			throw new ValueConversionException("display String", ObjectToStringForError(args.Value));
		if (args.ConvertingStatus == ConvertingStatus.Completed)
			return;

		if (args.Value == null)
			args.Value = NullDisplayString;
		else if (IsStringConversionSupported())
			args.Value = ValueToString(args.Value);
		else
			args.Value = args.Value.ToString();
	}

	// Changed event
	/// <summary>
	/// Fired when one of the properties of the Validator change.
	/// Call the Changed event.
	/// </summary>
	/// <param name="args"></param>
	protected virtual void OnChanged(EventArgs args) {
		if (_changed != null)
			_changed(this, args);
	}


}
