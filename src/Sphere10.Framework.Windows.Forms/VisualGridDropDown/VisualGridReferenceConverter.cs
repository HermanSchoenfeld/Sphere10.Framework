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

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Displays bound reference values without exposing their editable component properties.</summary>
internal sealed class VisualGridReferenceConverter : TypeConverter {
	private readonly VisualGridReferenceBindingBase _binding;

	// Retain the same reference binding used by the dropdown editor.
	public VisualGridReferenceConverter(VisualGridReferenceBindingBase binding) => _binding = binding;

	// String presentation uses the binding's display projection.
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		=> destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

	// Preserve the existing conversion fallback for every other destination type.
	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		=> destinationType == typeof(string) ? _binding.GetDisplayText(value) : base.ConvertTo(context, culture, value, destinationType);

	// A reference is selected as one value rather than edited through child properties.
	public override bool GetPropertiesSupported(ITypeDescriptorContext context) => false;
}
