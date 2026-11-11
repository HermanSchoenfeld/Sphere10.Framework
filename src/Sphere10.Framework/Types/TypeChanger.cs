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

namespace Sphere10.Framework;

/// <summary>Converts values through compatible types, standard conversions and component-model converters.</summary>
public static class TypeChanger {
	public static object SanitizeObject(object obj) {
		switch (Type.GetTypeCode(obj.GetType())) {
			case TypeCode.SByte:
				return (long)(SByte)obj;
			case TypeCode.Byte:
				return (long)(Byte)obj;
			case TypeCode.Int16:
				return (long)(Int16)obj;
			case TypeCode.UInt16:
				return (long)(UInt16)obj;
			case TypeCode.Int32:
				return (long)(Int32)obj;
			case TypeCode.UInt32:
				return (long)(UInt32)obj;
			case TypeCode.Int64:
				return (long)(Int64)obj;
			case TypeCode.UInt64:
				return (long)(UInt64)obj;
			case TypeCode.Single:
				return (double)(Single)obj;
			case TypeCode.Double:
				return (double)(Double)obj;
			case TypeCode.Decimal:
				return obj;
			case TypeCode.Empty:
			case TypeCode.Object:
			case TypeCode.DBNull:
			case TypeCode.Boolean:
			case TypeCode.Char:
			case TypeCode.DateTime:
			case TypeCode.String:
			default:
				return obj;
		}
	}

	/// <summary>Handles nullable targets and compatible values before using standard or component-model conversion.</summary>
	public static object ChangeType(object value, Type targetType) {
		// Handle null values before selecting the concrete conversion target.
		var isNullable = targetType.IsNullable();
		var isReferenceType = !targetType.IsValueType;
		if ((isNullable || isReferenceType) && (value == null || value == DBNull.Value))
			return null;
		if (value == null)
			throw new InvalidOperationException("Unable to convert NULL to a value type");

		// Preserve already compatible values and convert the remaining cases.
		if (targetType.IsInstanceOfType(value))
			return value;
		var conversionType = isNullable ? Nullable.GetUnderlyingType(targetType) : targetType;
		if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(conversionType))
			return Convert.ChangeType(value, conversionType);

		// Reuse component-model conversions for types outside the existing IConvertible path.
		var converter = TypeDescriptor.GetConverter(conversionType);
		if (converter.CanConvertFrom(value.GetType()))
			return converter.ConvertFrom(value);
		converter = TypeDescriptor.GetConverter(value.GetType());
		if (converter.CanConvertTo(conversionType))
			return converter.ConvertTo(value, conversionType);
		return Convert.ChangeType(value, conversionType);
	}

	/// <summary>Converts a value through the same conversion rules and returns the requested type.</summary>
	public static T ChangeType<T>(object value) => (T)ChangeType(value, typeof(T));

}
