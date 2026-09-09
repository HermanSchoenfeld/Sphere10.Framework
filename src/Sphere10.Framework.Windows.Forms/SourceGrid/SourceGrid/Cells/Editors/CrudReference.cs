// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

public class CrudReference : TextBoxUITypeEditor {
	private readonly CrudReferenceBinding _binding;

	public CrudReference(Type EntityType, CrudReferenceBinding Binding)
		: base(EntityType) {
		Guard.ArgumentNotNull(Binding, nameof(Binding));
		_binding = Binding;
		AllowNull = Binding.AllowNull;
		NullString = Binding.GetDisplayText(null);
		NullDisplayString = NullString;
		Control.UITypeEditor = new CrudReferenceEditor(Binding);
		Control.TextBox.ReadOnly = true;
		Control.Button.Text = "▼";
		Control.Button.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		Control.DialogClosed += (_, _) => {
			if (IsEditing)
				EditCellContext.EndEdit(false);
		};
	}

	public override bool IsStringConversionSupported() => false;

	protected override void OnConvertingValueToDisplayString(ComponentModel.ConvertingObjectEventArgs Args) => Args.Value = _binding.GetDisplayText(Args.Value);

	protected override void OnSendCharToEditor(char Key) {
	}

	internal override void InternalStartEdit(CellContext Context) {
		base.InternalStartEdit(Context);
		if (IsEditing)
			Control.BeginInvoke(new Action(() => {
				if (IsEditing && !Control.IsDisposed)
					Control.ShowDialog();
			}));
	}
}
