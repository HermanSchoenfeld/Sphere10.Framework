// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace Sphere10.Framework;

/// <summary>Formats collection values as a readable, culture-aware sequence for component-model consumers.</summary>
/// <remarks>Enumerates the collection synchronously; the caller must supply a finite collection and retain any required resource scope.</remarks>
public class CollectionTypeConverter : CollectionConverter {
	protected virtual string Separator => "; ";
	protected virtual string EmptyText => "None";

	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
		if (destinationType == typeof(string) && value is IEnumerable items && value is not string) {
			var text = new StringBuilder();
			var any = false;
			foreach (var item in items) {
				if (any) text.Append(Separator);
				text.Append(FormatItem(item, culture ?? CultureInfo.CurrentCulture));
				any = true;
			}
			return any ? text.ToString() : EmptyText;
		}
		return base.ConvertTo(context, culture, value, destinationType);
	}

	protected virtual string FormatItem(object item, CultureInfo culture) => Convert.ToString(item, culture) ?? string.Empty;
}
