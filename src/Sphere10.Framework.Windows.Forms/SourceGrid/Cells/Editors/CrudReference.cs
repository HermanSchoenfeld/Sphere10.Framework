// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
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

	public CrudReference(Type entityType, CrudReferenceBinding binding)
		: base(entityType) {
		Guard.ArgumentNotNull(binding, nameof(binding));
		_binding = binding;
		AllowNull = binding.AllowNull;
		NullString = binding.GetDisplayText(null);
		NullDisplayString = NullString;
		Control.UITypeEditor = new CrudReferenceEditor(binding);
		Control.TextBox.ReadOnly = true;
		Control.Button.Text = "▼";
		Control.Button.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
		Control.DialogClosed += (_, _) => {
			if (IsEditing)
				EditCellContext.EndEdit(false);
		};
	}

	public override bool IsStringConversionSupported() => false;

	internal override void InternalStartEdit(CellContext context) {
		base.InternalStartEdit(context);
		if (IsEditing)
			Control.BeginInvoke(new Action(() => {
				if (IsEditing && !Control.IsDisposed)
					Control.ShowDialog();
			}));
	}

	protected override void OnConvertingValueToDisplayString(Sphere10.Framework.ComponentModel.ConvertingObjectEventArgs args)
		=> args.Value = _binding.GetDisplayText(args.Value);

	protected override void OnSendCharToEditor(char key) {
	}
}
