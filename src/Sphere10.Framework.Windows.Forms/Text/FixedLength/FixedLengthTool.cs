// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text.RegularExpressions;
using Sphere10.Framework.Windows.Forms.Text.FixedLength;

namespace Tools;

public static class FixedLength {
	public static FieldList ExtractFieldListFromType(Type classType) {
		// Discover public properties and their fixed-length field declarations.
		var fields = new FieldList();
		var properties = classType.GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

		foreach (var property in properties) {
			// Read the optional formatting and value constraints for this field.
			FieldAttribute fieldAttribute = null;
			ParseFormatAttribute parseFormat = null;

			var attributes = property.GetCustomAttributes(typeof(FieldAttribute), true);
			if (attributes.Length > 0)
				fieldAttribute = (FieldAttribute)attributes[0];

			attributes = property.GetCustomAttributes(typeof(ParseFormatAttribute), true);
			if (attributes.Length > 0)
				parseFormat = (ParseFormatAttribute)attributes[0];

			var valueMappings = property.GetCustomAttributes(typeof(ValueMappingAttribute), true);
			var standardValues = property.GetCustomAttributes(typeof(StandardValueAttribute), true);

			if (fieldAttribute != null) {
				// Create the validator and field from the resolved attribute values.
				if (parseFormat == null)
					parseFormat = new ParseFormatAttribute();

				var validator = CreateValidator(property.PropertyType, parseFormat);
				var field = new Field(fieldAttribute.FieldIndex, property.Name, fieldAttribute.Length, validator);
				field.TrimBeforeParse = parseFormat.TrimBeforeParse;
				fields.Add(field);

				//ValueMapping - to convert specific values, can be an array of attribute, one for each conversion
				if (valueMappings.Length > 0) {
					var mapping = new Sphere10.Framework.ComponentModel.ValueMapping();
					var values = new object[valueMappings.Length];
					var strings = new object[valueMappings.Length];
					for (var index = 0; index < valueMappings.Length; index++) {
						//I convert the value assigned to the attribute to ensure that the field is valid and to support DateTime (that cannot be directly assigned to an attribute but must be declared as string)
						values[index] = validator.ObjectToValue(((ValueMappingAttribute)valueMappings[index]).FieldValue);
						strings[index] = ((ValueMappingAttribute)valueMappings[index]).StringValue;
					}

					mapping.ThrowErrorIfNotFound = false;
					mapping.ValueList = values;
					mapping.SpecialList = strings;
					mapping.SpecialType = typeof(string);
					mapping.BindValidator(validator);
				}

				//StandardValues
				if (standardValues.Length > 0) {
					var values = new object[standardValues.Length];
					for (var index = 0; index < standardValues.Length; index++)
						values[index] = ((StandardValueAttribute)standardValues[index]).StandardValue;

					validator.StandardValues = values;
					validator.StandardValuesExclusive = true;
				}
			}
		}

		return fields;
	}

	public static Sphere10.Framework.ComponentModel.IValidator CreateValidator(Type type, ParseFormatAttribute parseAttributes) {
		//Check for nullable
		type = GetBaseType(type, out var nullable);
		var converter = GetConverterFromPrimitiveType(type, parseAttributes);

		// Apply the field format and null handling to its validator.
		var validator = new Sphere10.Framework.ComponentModel.TypeConverterValidator(type, converter);
		validator.CultureInfo = parseAttributes.CultureInfo;
		validator.NullString = "";
		validator.NullDisplayString = "";
		validator.AllowNull = nullable;

		return validator;
	}

	/// <summary>Builds a literal separator expression; NUL retains the fixed-length format without separators.</summary>
	public static string ValidateRegExpSeparator(char separator) {
		if (separator == 0)
			return string.Empty;

		// See MSDN: "Character Escapes". Escape the separator so punctuation cannot become a pattern operator.
		return Regex.Escape(separator.ToString());
	}

	private static Type GetBaseType(Type type, out bool isNullable) {
		if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
			isNullable = true;
			return Nullable.GetUnderlyingType(type);
		}

		isNullable = false;
		return type;
	}

	private static System.ComponentModel.TypeConverter GetConverterFromPrimitiveType(Type type, ParseFormatAttribute parseAttributes) {
		if (type == typeof(string))
			return new System.ComponentModel.StringConverter();
		if (type == typeof(int))
			return new System.ComponentModel.Int32Converter();
		if (type == typeof(double))
			return new Sphere10.Framework.ComponentModel.NumberTypeConverter(typeof(double), parseAttributes.NumberFormat);
		if (type == typeof(decimal))
			return new Sphere10.Framework.ComponentModel.NumberTypeConverter(typeof(decimal), parseAttributes.NumberFormat);
		if (type == typeof(DateTime))
			return new Sphere10.Framework.ComponentModel.DateTimeTypeConverter(parseAttributes.DateTimeFormat, new string[] { parseAttributes.DateTimeFormat });
		throw new TypeNotSupportedException(type);
	}
}
