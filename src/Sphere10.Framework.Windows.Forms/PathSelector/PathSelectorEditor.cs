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

/// <summary>Gives a property grid row the browse button of a <see cref="PathSelectorControl"/>: the row's "..." button opens the dialog of a
/// selector configured from a <see cref="PathSelectorAttribute"/> and holding the row's path, and the chosen path becomes the row's value.</summary>
/// <remarks>A property grid hosts only type editors in its rows, so the row's own text box takes the typed path and this editor supplies the
/// selector's dialog. <see cref="VisualEntityEditor"/> creates one for every property marked with the attribute. Typed paths are not checked
/// for existence here, because the check would run on the UI thread; the owner of the value validates it.</remarks>
public class PathSelectorEditor : UITypeEditor {

	public PathSelectorEditor(PathSelectorAttribute selection) {
		Guard.ArgumentNotNull(selection, nameof(selection));
		Selection = selection;
	}

	/// <summary>The mode, filter, caption and existence rule of the dialog.</summary>
	public PathSelectorAttribute Selection { get; }

	public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

	public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value) {
		if (context?.PropertyDescriptor?.IsReadOnly == true)
			return value;
		// The dialog is owned by the window showing the row: the grid's editor service is its value view, a native window.
		var owner = provider?.GetService(typeof(IWin32Window)) as IWin32Window ?? provider?.GetService(typeof(IWindowsFormsEditorService)) as IWin32Window ?? Form.ActiveForm;
		using var selector = CreateSelector(value as string);
		return Browse(selector, owner) ? selector.Path : value;
	}

	/// <summary>Creates the selector whose dialog chooses the path, configured from <see cref="Selection"/> and holding the current path.</summary>
	protected virtual PathSelectorControl CreateSelector(string path) => new() {
		Mode = Selection.Mode,
		ForcePathExists = Selection.ForcePathExists,
		OverwritePrompt = Selection.OverwritePrompt,
		Filter = Selection.Filter,
		DialogTitle = Selection.DialogTitle,
		Path = path ?? string.Empty
	};

	/// <summary>Shows the selector's dialog over the owner window and returns whether a path was chosen.</summary>
	protected virtual bool Browse(PathSelectorControl selector, IWin32Window owner) => selector.Browse(owner);
}
