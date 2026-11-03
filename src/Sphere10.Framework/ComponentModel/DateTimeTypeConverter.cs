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

	// Constructors
	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter() {
	}

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter(string toStringFormat) {
		Format = toStringFormat;
	}

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter(string toStringFormat, string[] parseFormats) {
		ParseFormats = parseFormats;
		Format = toStringFormat;
	}

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public DateTimeTypeConverter(string toStringFormat, string[] parseFormats, DateTimeStyles dateTimeStyles) {
		ParseFormats = parseFormats;
		Format = toStringFormat;
		DateTimeStyles = dateTimeStyles;
	}

	/// <summary>Gets or sets the fallback converter used for operations outside the custom string conversion.</summary>
	public TypeConverter BaseTypeConverter { get; set; } = TypeDescriptor.GetConverter(typeof(DateTime));

	/// <summary>
	/// DateTimeStyle for Parse operations. DefaultValue: AllowInnerWhite|AllowLeadingWhite|AllowTrailingWhite|AllowWhiteSpaces
	/// </summary>
	public DateTimeStyles DateTimeStyles { get; set; } = DateTimeStyles.AllowInnerWhite
		| DateTimeStyles.AllowLeadingWhite
		| DateTimeStyles.AllowTrailingWhite
		| DateTimeStyles.AllowWhiteSpaces;

	/// <summary>
	/// Format of the Date. Example: G, g, d, D. Default value : G
	/// </summary>
	public string Format { get; set; } = "G";

	/// <summary>
	/// Formats to check when parse the string. If null call with no format the parse method. Default value: null
	/// </summary>
	public string[] ParseFormats { get; set; } = null;

	/// <summary>Converts configured string values and delegates other inputs to the fallback converter.</summary>
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
		if (value != null && value.GetType() == typeof(string)) {
			if (ParseFormats != null)
				return DateTime.ParseExact((string)value, ParseFormats, GetCulture(culture).DateTimeFormat, DateTimeStyles);
			return DateTime.Parse((string)value, GetCulture(culture).DateTimeFormat, DateTimeStyles);
		}
		return BaseTypeConverter.ConvertFrom(context, culture, value);
	}

	/// <summary>Formats configured values and delegates other destinations to the fallback converter.</summary>
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
		if (destinationType == typeof(string) && value != null)
			return ((DateTime)value).ToString(Format, GetCulture(culture).DateTimeFormat);
		return BaseTypeConverter.ConvertTo(context, culture, value, destinationType);
	}

	/// <summary>Checks strings through conversion and delegates other values to the fallback converter.</summary>
	public override bool IsValid(ITypeDescriptorContext context, object value) {
		if (value != null && value.GetType() == typeof(string)) {
			// Validate strings through the same conversion pipeline used by callers.
			return Tools.Exceptions.Execute(() => ConvertFrom(context, CultureInfo.CurrentCulture, value));
		}
		return BaseTypeConverter.IsValid(context, value);
	}

	// Implementation TypeConverter
	/// <summary>Reports whether string input or the fallback source type is supported.</summary>
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
		if (sourceType == typeof(string))
			return true;
		return BaseTypeConverter.CanConvertFrom(context, sourceType);
	}

	/// <summary>Reports whether string output or the fallback destination type is supported.</summary>
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
		if (destinationType == typeof(string))
			return true;
		return BaseTypeConverter.CanConvertTo(context, destinationType);
	}

	/// <summary>Delegates instance creation from property values to the fallback converter.</summary>
	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues) {
		return BaseTypeConverter.CreateInstance(context, propertyValues);
	}

	/// <summary>Reports whether the fallback converter supports instance creation.</summary>
	public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) {
		return BaseTypeConverter.GetCreateInstanceSupported(context);
	}

	/// <summary>Returns the properties supplied by the fallback converter.</summary>
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes) {
		return BaseTypeConverter.GetProperties(context, value, attributes);
	}

	/// <summary>Reports whether the fallback converter supplies editable properties.</summary>
	public override bool GetPropertiesSupported(ITypeDescriptorContext context) {
		return BaseTypeConverter.GetPropertiesSupported(context);
	}

	/// <summary>Returns the fallback converter's standard values.</summary>
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
		return BaseTypeConverter.GetStandardValues(context);
	}

	/// <summary>Reports whether the fallback converter restricts values to its standard list.</summary>
	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) {
		return BaseTypeConverter.GetStandardValuesExclusive(context);
	}

	/// <summary>Reports whether the fallback converter supplies standard values.</summary>
	public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
		return BaseTypeConverter.GetStandardValuesSupported(context);
	}

	// Resolve the caller's culture before using the current culture fallback.
	private CultureInfo GetCulture(CultureInfo requestedCulture) {
		if (requestedCulture == null)
			return CultureInfo.CurrentCulture;
		return requestedCulture;
	}
}
