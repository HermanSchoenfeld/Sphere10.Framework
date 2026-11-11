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
/// A TypeConverter that support string conversion from and to string with a custom format number.
/// Support Conversion for Float, Double and Decimal, Int
/// </summary>
public class NumberTypeConverter : TypeConverter {
	// Conversion configuration.
	private TypeConverter _baseTypeConverter;
	private Type _baseType;
	private string _format = "G";

	//		private IFormatProvider _formatProvider = null;
	//		public IFormatProvider FormatProvider
	//		{
	//			get{return _formatProvider;}
	//			set{_formatProvider = value;}
	//		}
	//
	private NumberStyles _numberStyles = NumberStyles.Number;

	// Constructors
	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public NumberTypeConverter(Type baseType) => BaseType = baseType;

	/// <summary>Creates a converter with the selected parsing and display configuration.</summary>
	public NumberTypeConverter(Type baseType, string format)
		: this(baseType) {
		Format = format;
	}

	/// <summary>Gets or sets the fallback converter used for operations outside the custom string conversion.</summary>
	public TypeConverter BaseTypeConverter {
		get => _baseTypeConverter;
		set => _baseTypeConverter = value;
	}

	/// <summary>Gets or sets the supported numeric type, including its nullable form where available.</summary>
	public Type BaseType {
		get => _baseType;
		set {
			if (value != typeof(double) &&
				value != typeof(float) &&
				value != typeof(decimal) &&
				value != typeof(int) &&
				value != typeof(double?) &&
				value != typeof(float?) &&
				value != typeof(decimal?) &&
				value != typeof(int?))
				throw new ArgumentException("Type not supported", "BaseType");

			// Keep the selected numeric type and its fallback converter together.
			_baseTypeConverter = TypeDescriptor.GetConverter(value);
			_baseType = value;
		}
	}

	/// <summary>Gets or sets the numeric display format.</summary>
	public string Format {
		get => _format;
		set => _format = value;
	}

	/// <summary>Gets or sets the numeric styles accepted while parsing strings.</summary>
	public NumberStyles NumberStyles {
		get => _numberStyles;
		set => _numberStyles = value;
	}

	/// <summary>Converts configured string values and delegates other inputs to the fallback converter.</summary>
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
		// Parse supported numeric strings before delegating other input types.
		if (value is string text) {
			var nullableType = Nullable.GetUnderlyingType(BaseType);
			if (nullableType != null && text.Length == 0)
				return null;
			var numberType = nullableType ?? BaseType;
			if (numberType == typeof(double))
				return StringToDouble(text, NumberStyles, GetCulture(culture).NumberFormat);
			if (numberType == typeof(decimal))
				return StringToDecimal(text, NumberStyles, GetCulture(culture).NumberFormat);
			if (numberType == typeof(float))
				return StringToFloat(text, NumberStyles, GetCulture(culture).NumberFormat);
			if (numberType == typeof(int))
				return StringToInt(text, NumberStyles, GetCulture(culture).NumberFormat);
			throw new ArgumentException("Not supported type");
		}
		return _baseTypeConverter.ConvertFrom(context, culture, value);
	}

	/// <summary>Formats configured values and delegates other destinations to the fallback converter.</summary>
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
		// Format supported numeric values before delegating other destinations.
		if (destinationType == typeof(string) && value != null) {
			var numberType = Nullable.GetUnderlyingType(BaseType) ?? BaseType;
			if (numberType == typeof(double))
				return DoubleToString((double)value, Format, GetCulture(culture).NumberFormat);
			if (numberType == typeof(decimal))
				return DecimalToString((decimal)value, Format, GetCulture(culture).NumberFormat);
			if (numberType == typeof(float))
				return FloatToString((float)value, Format, GetCulture(culture).NumberFormat);
			if (numberType == typeof(int))
				return IntToString((int)value, Format, GetCulture(culture).NumberFormat);
			return _baseTypeConverter.ConvertTo(context, culture, value, destinationType);
		}
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
	public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues) => _baseTypeConverter.CreateInstance(context, propertyValues);

	/// <summary>Reports whether the fallback converter supports instance creation.</summary>
	public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) => _baseTypeConverter.GetCreateInstanceSupported(context);

	/// <summary>Returns the properties supplied by the fallback converter.</summary>
	public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		=> _baseTypeConverter.GetProperties(context, value, attributes);

	/// <summary>Reports whether the fallback converter supplies editable properties.</summary>
	public override bool GetPropertiesSupported(ITypeDescriptorContext context) => _baseTypeConverter.GetPropertiesSupported(context);

	/// <summary>Returns the fallback converter's standard values.</summary>
	public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => _baseTypeConverter.GetStandardValues(context);

	/// <summary>Reports whether the fallback converter restricts values to its standard list.</summary>
	public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => _baseTypeConverter.GetStandardValuesExclusive(context);

	/// <summary>Reports whether the fallback converter supplies standard values.</summary>
	public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => _baseTypeConverter.GetStandardValuesSupported(context);

	// Conversion String Methods
	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static double StringToDouble(string strVal, NumberStyles style, IFormatProvider provider) => double.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static float StringToFloat(string strVal, NumberStyles style, IFormatProvider provider) => float.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static decimal StringToDecimal(string strVal, NumberStyles style, IFormatProvider provider) => decimal.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static byte StringToByte(string strVal, NumberStyles style, IFormatProvider provider) => byte.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static sbyte StringToSByte(string strVal, NumberStyles style, IFormatProvider provider) => sbyte.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static short StringToShort(string strVal, NumberStyles style, IFormatProvider provider) => short.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static ushort StringToUShort(string strVal, NumberStyles style, IFormatProvider provider) => ushort.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static int StringToInt(string strVal, NumberStyles style, IFormatProvider provider) => int.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static uint StringToUInt(string strVal, NumberStyles style, IFormatProvider provider) => uint.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static long StringToLong(string strVal, NumberStyles style, IFormatProvider provider) => long.Parse(strVal, style, provider);

	/// <summary>Parses the requested numeric value using the supplied styles and format provider.</summary>
	public static ulong StringToULong(string strVal, NumberStyles style, IFormatProvider provider) => ulong.Parse(strVal, style, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string DoubleToString(double val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string FloatToString(float val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string DecimalToString(decimal val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string ByteToString(byte val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string SByteToString(sbyte val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string ShortToString(short val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string UShortToString(ushort val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string IntToString(int val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string UIntToString(uint val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string LongToString(long val, string format, IFormatProvider provider) => val.ToString(format, provider);

	/// <summary>Formats the numeric value using the supplied format and provider.</summary>
	public static string ULongToString(ulong val, string format, IFormatProvider provider) => val.ToString(format, provider);

	// Resolve the caller's culture before using the current culture fallback.
	private CultureInfo GetCulture(CultureInfo requestedCulture) {
		if (requestedCulture == null)
			return CultureInfo.CurrentCulture;
		return requestedCulture;
	}
}
