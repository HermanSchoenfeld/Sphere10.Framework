// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits screen-owned column order, width and stretch through the native collection editor.</summary>
public sealed class VisualGridColumnsEditor : ArrayEditor {

	/// <summary>Uses the existing fixed column schema rather than creating new column definitions.</summary>
	public VisualGridColumnsEditor()
		: base(typeof(ApplicationGridColumnPreferences[])) {
	}

	/// <summary>Returns accepted column values while leaving the supplied settings untouched on Cancel.</summary>
	public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
		if (value is not ApplicationGridColumnPreferences[] columns)
			return value;

		// The native editor changes its item instances while the dialog is open.
		var copy = VisualGridSettings.CopyColumns(columns);
		var edited = base.EditValue(context, provider, copy);

		// ArrayEditor replaces the array when accepted; Cancel returns its supplied array.
		return ReferenceEquals(edited, copy) ? value : edited;
	}

	/// <summary>Reuses the native ordering and property controls for the screen's fixed column schema.</summary>
	protected override CollectionForm CreateCollectionForm() {
		var form = base.CreateCollectionForm();
		form.Text = "Columns";
		form.Controls.Find("addButton", true).Single().Visible = false;
		form.Controls.Find("removeButton", true).Single().Visible = false;
		return form;
	}

	/// <summary>Column membership belongs to the screen's schema.</summary>
	protected override bool CanRemoveInstance(object value) => false;

	/// <summary>The native editor's Insert shortcut cannot create columns outside the screen's schema.</summary>
	protected override object CreateInstance(Type itemType) => null;
}
