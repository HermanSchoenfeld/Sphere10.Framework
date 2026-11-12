// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// A Cell with a CheckBox. This Cell is of type bool.
/// </summary>
public class CheckBox : Cell {
	#region Constructor

	/// <summary>
	/// Constrcutor
	/// </summary>
	public CheckBox()
		: this(null, false) {
	}

	/// <summary>
	/// Construct a CellCheckBox class with caption and align checkbox in the MiddleLeft, using BehaviorModels.CheckBox.Default
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="checkValue"></param>
	public CheckBox(string caption, bool? checkValue)
		: base(checkValue) {
		if (caption != null && caption.Length > 0)
			View = Views.CheckBox.MiddleLeftAlign;
		else
			View = Views.CheckBox.Default;

		Model.AddModel(new Models.CheckBox());
		AddController(Controllers.CheckBox.Default);
		AddController(Controllers.MouseInvalidate.Default);

		//I use an editor to validate the value and also because the space key are
		// directly used by the controller, so must not start an edit operation but must still edit the value
		Editor = new Editors.EditorBase(typeof(bool));
		Editor.EditableMode = SourceGrid.EditableMode.None;

		Caption = caption;
	}

	#endregion

	/// <summary>
	/// Checked status (equal to the Value property but returns a bool)
	/// </summary>
	public bool? Checked {
		get => CheckBoxModel.GetCheckBoxStatus(GetContext()).Checked;
		set => CheckBoxModel.SetCheckedValue(GetContext(), value);
	}

	/// <summary>
	/// Caption of the cell
	/// </summary>
	public string Caption {
		get => CheckBoxModel.Caption;
		set => CheckBoxModel.Caption = value;
	}

	#region Properties

	private Models.CheckBox CheckBoxModel => (Models.CheckBox)Model.FindModel(typeof(Models.CheckBox));

	#endregion
}
