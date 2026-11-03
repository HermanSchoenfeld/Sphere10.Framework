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
internal sealed class CrudReferenceConverter : TypeConverter {
	private readonly CrudReferenceBinding _binding;

	// Retain the same reference binding used by the dropdown editor.
	public CrudReferenceConverter(CrudReferenceBinding Binding) {
		_binding = Binding;
	}

	// String presentation uses the binding's display projection.
	public override bool CanConvertTo(ITypeDescriptorContext? Context, Type? DestinationType) {
		return DestinationType == typeof(string) || base.CanConvertTo(Context, DestinationType);
	}

	// Preserve the existing conversion fallback for every other destination type.
	public override object? ConvertTo(ITypeDescriptorContext? Context, CultureInfo? Culture, object? Value, Type DestinationType) {
		return DestinationType == typeof(string) ? _binding.GetDisplayText(Value) : base.ConvertTo(Context, Culture, Value, DestinationType);
	}

	// A reference is selected as one value rather than edited through child properties.
	public override bool GetPropertiesSupported(ITypeDescriptorContext? Context) {
		return false;
	}
}
