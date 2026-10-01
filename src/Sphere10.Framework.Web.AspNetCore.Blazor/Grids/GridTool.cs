// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Globalization;
using Sphere10.Framework;

// ReSharper disable CheckNamespace
namespace Tools;

/// <summary>Invariant conversions between grid scalar values and HTML editor values.</summary>
public static class BlazorGrid {
	public static bool IsSupportedType(Type type) {
		Guard.ArgumentNotNull(type, nameof(type));
		type = Nullable.GetUnderlyingType(type) ?? type;
		return type.IsEnum || type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(TimeOnly)
			|| type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
			|| Type.GetTypeCode(type) is TypeCode.String or TypeCode.Char or TypeCode.Boolean or TypeCode.DateTime
				or TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or TypeCode.UInt32
				or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
	}

	public static string GetInputType(Type type) {
		Guard.ArgumentNotNull(type, nameof(type));
		type = Nullable.GetUnderlyingType(type) ?? type;
		if (type == typeof(bool))
			return "checkbox";
		if (type == typeof(DateOnly))
			return "date";
		if (type == typeof(TimeOnly))
			return "time";
		if (type == typeof(DateTime))
			return "datetime-local";
		if (!type.IsEnum && Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
			or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or TypeCode.Decimal)
			return "number";
		return "text";
	}

	/// <summary>Formats a display value, or an HTML input value when no custom format is supplied.</summary>
	public static string FormatValue(object value, Type type, string format = null) {
		Guard.ArgumentNotNull(type, nameof(type));
		if (value == null)
			return string.Empty;
		if (format != null && value is IFormattable formatted)
			return formatted.ToString(format, CultureInfo.InvariantCulture);
		return value switch {
			DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			TimeOnly time => time.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
			DateTime dateTime => dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
			DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
			TimeSpan duration => duration.ToString("c", CultureInfo.InvariantCulture),
			IFormattable scalar => scalar.ToString(null, CultureInfo.InvariantCulture),
			_ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
		};
	}

	/// <summary>Parses editor values without depending on the server or browser culture.</summary>
	public static object ParseValue(object value, Type type) {
		Guard.ArgumentNotNull(type, nameof(type));
		var underlyingType = Nullable.GetUnderlyingType(type);
		var scalarType = underlyingType ?? type;
		if (value == null) {
			Guard.Argument(underlyingType != null || !type.IsValueType, nameof(value), "A value is required.");
			return null;
		}
		if (scalarType.IsInstanceOfType(value))
			return value;
		Guard.Argument(IsSupportedType(type), nameof(type), "The value requires a custom editor.");
		var text = Convert.ToString(value, CultureInfo.InvariantCulture);
		if (scalarType == typeof(string))
			return text;
		if (string.IsNullOrWhiteSpace(text)) {
			Guard.Argument(underlyingType != null, nameof(value), "A value is required.");
			return null;
		}
		var converter = TypeDescriptor.GetConverter(scalarType);
		Guard.Argument(converter.CanConvertFrom(typeof(string)), nameof(type), "The value requires a custom editor.");
		return converter.ConvertFrom(null, CultureInfo.InvariantCulture, text);
	}
}
