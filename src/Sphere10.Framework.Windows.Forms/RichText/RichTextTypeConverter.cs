// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A TypeConverter that support rich text conversion from and to string.
/// </summary>
public class RichTextTypeConverter : System.ComponentModel.TypeConverter {
	// Constructors
	/// <summary>
	/// Constructor
	/// </summary>
	public RichTextTypeConverter() {
	}

	/// <summary>
	/// Convert string to RichText
	/// </summary>
	/// <param name="context"></param>
	/// <param name="culture"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) {
		if (value != null && (value.GetType() == typeof(string) || value.GetType() == typeof(int)))
			return Sphere10.Framework.Windows.Forms.RichTextConversion.StringToRichText(value.ToString());
		if (value != null && value.GetType() == typeof(Sphere10.Framework.Windows.Forms.RichText))
			return value;
		throw new ArgumentException("Not supported type");
	}

	/// <summary>
	/// Convert RichText to string
	/// </summary>
	/// <param name="context"></param>
	/// <param name="culture"></param>
	/// <param name="value"></param>
	/// <param name="destinationType"></param>
	/// <returns></returns>
	public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) {
		if (destinationType == typeof(string) && value != null && value.GetType() == typeof(Sphere10.Framework.Windows.Forms.RichText))
			return Sphere10.Framework.Windows.Forms.RichTextConversion.RichTextToString(value as Sphere10.Framework.Windows.Forms.RichText);
		if (destinationType == typeof(Sphere10.Framework.Windows.Forms.RichText) && IsValid(value))
			return new Sphere10.Framework.Windows.Forms.RichText(value as string);
		if (destinationType == typeof(Sphere10.Framework.Windows.Forms.RichText) && value != null && value.GetType() == typeof(string))
			return Sphere10.Framework.Windows.Forms.RichTextConversion.StringToRichText(value as string);
		if (value != null && destinationType == value.GetType())
			return value;
		throw new ArgumentException("Not supported type");
	}

	/// <summary>
	/// Check if rich text string is valid
	/// </summary>
	/// <param name="context"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public override bool IsValid(System.ComponentModel.ITypeDescriptorContext context, object value) {
		if (value != null && value.GetType() == typeof(string)) {
			// try to convert it
			try {
				ConvertFrom(context, System.Globalization.CultureInfo.CurrentCulture, value);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		return false;
	}

	// Implementation TypeConverter
	/// <summary>
	/// string and RichText can be converted from
	/// </summary>
	/// <param name="context"></param>
	/// <param name="sourceType"></param>
	/// <returns></returns>
	public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) {
		if (sourceType == typeof(string))
			return true;
		if (sourceType == typeof(Sphere10.Framework.Windows.Forms.RichText))
			return true;
		if (sourceType == typeof(int))
			return true;

		return false;
	}

	/// <summary>
	/// string and RichText can be converted to
	/// </summary>
	/// <param name="context"></param>
	/// <param name="destinationType"></param>
	/// <returns></returns>
	public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType) {
		if (destinationType == typeof(Sphere10.Framework.Windows.Forms.RichText))
			return true;
		if (destinationType == typeof(string))
			return true;

		return false;
	}
}
