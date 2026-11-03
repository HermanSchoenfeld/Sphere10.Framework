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

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// Converts dates using configured display and parse formats with the selected culture.
/// </summary>
public class DateTimeTypeConverter : TypeConverter {
	// Conversion configuration.
	private TypeConverter _baseTypeConverter = TypeDescriptor.GetConverter(typeof(DateTime));
	private DateTimeStyles _dateTimeStyles =
		DateTimeStyles.AllowInnerWhite
		| DateTimeStyles.AllowLeadingWhite
		| DateTimeStyles.AllowTrailingWhite
		| DateTimeStyles.AllowWhiteSpaces;
	private string _format = "G";
	private string[] _parseFormats = null;

	// Constructors
	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter() {
	}

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter(string toStringFormat) {
		_format = toStringFormat;
	}

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter(string toStringFormat, string[] parseFormats) {
		_parseFormats = parseFormats;
		_format = toStringFormat;
	}

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter(string toStringFormat, string[] parseFormats, DateTimeStyles dateTimeStyles) {
		_parseFormats = parseFormats;
		_format = toStringFormat;
		_dateTimeStyles = dateTimeStyles;
	}

	/// <summary>Gets or sets the fallback converter used for operations outside the custom string conversion.</summary>
	public TypeConverter BaseTypeConverter {
		get {
			return _baseTypeConverter;
		}
		set {
			_baseTypeConverter = value;
		}
	}

	/// <summary>
	/// DateTimeStyle for Parse operations. DefaultValue: AllowInnerWhite|AllowLeadingWhite|AllowTrailingWhite|AllowWhiteSpaces
	/// </summary>
	public DateTimeStyles DateTimeStyles {
		get {
			return _dateTimeStyles;
		}
		set {
			_dateTimeStyles = value;
		}
	}

	/// <summary>
	/// Format of the Date. Example: G, g, d, D. Default value : G
	/// </summary>
	public string Format {
		get {
			return _format;
		}
		set {
			_format = value;
		}
	}

	/// <summary>
	/// Formats to check when parse the string. If null call with no format the parse method. Default value: null
	/// </summary>
	public string[] ParseFormats {
		get {
			return _parseFormats;
		}
		set {
			_parseFormats = value;
		}
	}

	/// <summary>Converts configured string values and delegates other inputs to the fallback converter.</summary>
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
		if (value != null && value.GetType() == typeof(string)) {
			if (_parseFormats != null)
				return DateTime.ParseExact((string)value, _parseFormats, GetCulture(culture).DateTimeFormat, _dateTimeStyles);
			return DateTime.Parse((string)value, GetCulture(culture).DateTimeFormat, _dateTimeStyles);
		}
		return _baseTypeConverter.ConvertFrom(context, culture, value);
	}

	/// <summary>Formats configured values and delegates other destinations to the fallback converter.</summary>
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
		if (destinationType == typeof(string) && value != null)
			return ((DateTime)value).ToString(_format, GetCulture(culture).DateTimeFormat);
		return _baseTypeConverter.ConvertTo(context, culture, value, destinationType);
	}

	/// <summary>Checks strings through conversion and delegates other values to the fallback converter.</summary>
	public override bool IsValid(ITypeDescriptorContext context, object value) {
		if (value != null && value.GetType() == typeof(string)) {
			// Validate strings through the same conversion pipeline used by callers.
			return Tools.Exceptions.Execute(() => ConvertFrom(context, CultureInfo.CurrentCulture, value));
		}
		return _baseTypeConverter.IsValid(context, value);
	}

	// Implementation TypeConverter
	/// <summary>Reports whether string input or the fallback source type is supported.</summary>
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
		if (sourceType == typeof(string))
			return true;
		return _baseTypeConverter.CanConvertFrom(context, sourceType);
	}

	/// <summary>Reports whether string output or the fallback destination type is supported.</summary>
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
		if (destinationType == typeof(string))
			return true;
		return _baseTypeConverter.CanConvertTo(context, destinationType);
	}

	/// <summary>Delegates instance creation from property values to the fallback converter.</summary>
	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues) {
		return _baseTypeConverter.CreateInstance(context, propertyValues);
	}

	/// <summary>Reports whether the fallback converter supports instance creation.</summary>
	public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) {
		return _baseTypeConverter.GetCreateInstanceSupported(context);
	}

	/// <summary>Returns the properties supplied by the fallback converter.</summary>
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
		return _baseTypeConverter.GetProperties(context, value, attributes);
	}

	/// <summary>Reports whether the fallback converter supplies editable properties.</summary>
	public override bool GetPropertiesSupported(ITypeDescriptorContext context) {
		return _baseTypeConverter.GetPropertiesSupported(context);
	}

	/// <summary>Returns the fallback converter's standard values.</summary>
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
		return _baseTypeConverter.GetStandardValues(context);
	}

	/// <summary>Reports whether the fallback converter restricts values to its standard list.</summary>
	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
		return _baseTypeConverter.GetStandardValuesExclusive(context);
	}

	/// <summary>Reports whether the fallback converter supplies standard values.</summary>
	public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
		return _baseTypeConverter.GetStandardValuesSupported(context);
	}

	// Resolve the caller's culture before using the current culture fallback.
	private CultureInfo GetCulture(CultureInfo requestedCulture) {
		if (requestedCulture == null)
			return CultureInfo.CurrentCulture;
		return requestedCulture;
	}
}
