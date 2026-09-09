// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Shares the same CRUD reference dropdown between PropertyGrid and inline grid editors.</summary>
public class CrudReferenceEditor : UITypeEditor {
	private readonly CrudReferenceBinding _binding;

	public CrudReferenceEditor(CrudReferenceBinding Binding) {
		Guard.ArgumentNotNull(Binding, nameof(Binding));
		_binding = Binding;
	}

	public override bool IsDropDownResizable => true;

	public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? Context) => UITypeEditorEditStyle.DropDown;

	public override object? EditValue(ITypeDescriptorContext? Context, IServiceProvider Provider, object? Value) {
		if (Context?.PropertyDescriptor?.IsReadOnly == true || Provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService Service)
			return Value;
		using var Picker = new CrudReferencePicker(_binding, Value);
		if ((Provider as Control ?? Service as Control ?? Context?.Instance as Control) is { } Owner)
			Picker.Font = Owner.Font;
		Picker.SelectionAccepted += (_, _) => Service.CloseDropDown();
		Picker.SelectionCancelled += (_, _) => Service.CloseDropDown();
		Service.DropDownControl(Picker);
		return Picker.HasSelection ? Picker.SelectedEntity : Value;
	}
}

internal sealed class CrudReferenceConverter : TypeConverter {
	private readonly CrudReferenceBinding _binding;

	public CrudReferenceConverter(CrudReferenceBinding Binding) => _binding = Binding;

	public override bool CanConvertTo(ITypeDescriptorContext? Context, Type? DestinationType) => DestinationType == typeof(string) || base.CanConvertTo(Context, DestinationType);

	public override object? ConvertTo(ITypeDescriptorContext? Context, CultureInfo? Culture, object? Value, Type DestinationType) =>
		DestinationType == typeof(string) ? _binding.GetDisplayText(Value) : base.ConvertTo(Context, Culture, Value, DestinationType);

	public override bool GetPropertiesSupported(ITypeDescriptorContext? Context) => false;
}
