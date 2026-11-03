// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Virtual;

/// <summary>
/// A Cell with a CheckBox. This Cell is of type bool. Abstract, you must override GetValue and SetValue.
/// </summary>
public class CheckBox : CellVirtual {
	/// <summary>
	/// Constructor of a CheckBox style cell. You must st a valid Model to use this type of cell with this constructor.
	/// </summary>
	public CheckBox() {
		View = Views.CheckBox.Default;
		AddController(Controllers.CheckBox.Default);
		AddController(Controllers.MouseInvalidate.Default);

		//I use an editor to validate the value and also because the space key are
		// directly used by the controller, so must not start an edit operation but must still edit the value
		Editor = new Editors.EditorBase(typeof(bool));
		Editor.EditableMode = SourceGrid.EditableMode.None;

		Model.AddModel(new Models.CheckBox());
	}
}
