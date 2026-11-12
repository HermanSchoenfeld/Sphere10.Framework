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
using System.Drawing.Design;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Hosts the UITypeEditor associated with the configured value type.
/// </summary>
public partial class TextBoxUITypeEditor : ValidatedTextBoxButton, IServiceProvider, System.Windows.Forms.Design.IWindowsFormsEditorService, ITypeDescriptorContext {
	private UITypeEditor _uiTypeEditor;

	// System.Windows.Forms.Design.IWindowsFormsEditorService
	private Sphere10.Framework.Windows.Forms.DropDownForm _dropDown = null;

	public TextBoxUITypeEditor() {
		// This call is required by the Windows Form Designer.
		InitializeComponent();
	}

	/// <summary>
	/// Gets or sets the UITypeEditor to use. If you have specified a validator the TypeDescriptor.GetEditor method is used based on the Validator.ValueType.
	/// </summary>
	[DefaultValue(null), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public UITypeEditor UITypeEditor {
		get => _uiTypeEditor;
		set => _uiTypeEditor = value;
	}

	IContainer ITypeDescriptorContext.Container => base.Container;

	object ITypeDescriptorContext.Instance => Value;

	PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor => null;

	public override void ShowDialog() {
		try {
			OnDialogOpen(EventArgs.Empty);
			if (_uiTypeEditor != null) {
				var style = _uiTypeEditor.GetEditStyle();
				if (style == UITypeEditorEditStyle.DropDown ||
					style == UITypeEditorEditStyle.Modal) {
					object editObject;
					//Try to read the actual value, if the function failed I edit the default value
					if (IsValidValue(out editObject) == false) {
						if (Validator != null)
							editObject = Validator.DefaultValue;
						else
							editObject = null;
					}

					var temporary = _uiTypeEditor.EditValue(this, this, editObject);
					Value = temporary;
				}
			}

			OnDialogClosed(EventArgs.Empty);
		} catch (Exception exception) {
			_ = ExceptionDialog.ShowAsync(this, exception);
		}
	}

	public virtual System.Windows.Forms.DialogResult ShowDialog(System.Windows.Forms.Form dialog) {
		// IWindowsFormsEditorService member — sync contract required by the interface.
		return dialog.ShowDialog(this);
	}

	public virtual async System.Threading.Tasks.Task<System.Windows.Forms.DialogResult> ShowDialogAsync(System.Windows.Forms.Form dialog)
		=> await dialog.ShowDialogAsync(this);

	public virtual void CloseDropDown() {
		if (_dropDown != null)
			_dropDown.CloseDropDown();
	}

	public virtual void DropDownControl(System.Windows.Forms.Control control) {
		using (_dropDown = new Sphere10.Framework.Windows.Forms.DropDownForm(control, this, this.ParentForm)) {
			_dropDown.DropDownFlags = Sphere10.Framework.Windows.Forms.DropDownFlags.CloseOnEscape;

			_dropDown.ShowDropDown();

			_dropDown.Close();
		}
		_dropDown = null;
	}

	// IServiceProvider Members
	System.Object IServiceProvider.GetService(System.Type serviceType) {
		//modal
		if (serviceType == typeof(System.Windows.Forms.Design.IWindowsFormsEditorService))
			return this;

		return null;
	}

	// ITypeDescriptorContext Members
	void ITypeDescriptorContext.OnComponentChanged() {
	}

	bool ITypeDescriptorContext.OnComponentChanging() => true;

	//public bool ShouldSerializeUITypeEditor()
	//{
	//    return _uiTypeEditor != _defaultUITypeEditor;
	//}

	protected override void ApplyValidatorRules() {
		base.ApplyValidatorRules();

		if (_uiTypeEditor == null && Validator != null) {
			var temporary = System.ComponentModel.TypeDescriptor.GetEditor(Validator.ValueType, typeof(UITypeEditor));
			if (temporary is UITypeEditor)
				_uiTypeEditor = (UITypeEditor)temporary;
		}
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}
}
