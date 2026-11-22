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
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Shares the same CRUD reference dropdown between PropertyGrid and inline grid editors.</summary>
internal sealed class VisualGridReferenceEditor : UITypeEditor {
	private readonly VisualGridReferenceBindingBase _binding;

	public VisualGridReferenceEditor(VisualGridReferenceBindingBase binding) {
		Guard.ArgumentNotNull(binding, nameof(binding));
		_binding = binding;
	}

	public override bool IsDropDownResizable => true;

	public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.DropDown;

	public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
		if (context?.PropertyDescriptor?.IsReadOnly == true || provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service)
			return value;
		using (var picker = new VisualGridReferencePicker(_binding, value)) {
			if ((provider as Control ?? service as Control ?? context?.Instance as Control) is { } owner)
				picker.Font = owner.Font;
			picker.SelectionAccepted += _ => service.CloseDropDown();
			picker.SelectionCancelled += _ => service.CloseDropDown();
			service.DropDownControl(picker);
			return picker.HasSelection ? picker.SelectedEntity : value;
		}
	}
}
