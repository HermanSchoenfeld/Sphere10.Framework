// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Summary description for VisualModelCheckBox.
/// </summary>
[Serializable]
public class CheckBox : Cell {
	private System.Drawing.ContentAlignment _checkBoxAlignment = System.Drawing.ContentAlignment.MiddleCenter;
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ICheckBox _elementCheckBox = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.CheckBoxThemed();

	/// <summary>
	/// Represents a default CheckBox with the CheckBox image align to the Middle Center of the cell. You must use this VisualModel with a Cell of type ICellCheckBox.
	/// </summary>
	public new readonly static CheckBox Default = new CheckBox();

	/// <summary>
	/// Represents a CheckBox with the CheckBox image align to the Middle Left of the cell
	/// </summary>
	public readonly static CheckBox MiddleLeftAlign;

	// Constructors
	static CheckBox() {
		MiddleLeftAlign = new CheckBox();
		MiddleLeftAlign.CheckBoxAlignment = System.Drawing.ContentAlignment.MiddleLeft;
		MiddleLeftAlign.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
	}

	/// <summary>
	/// Use default setting and construct a read and write VisualProperties
	/// </summary>
	public CheckBox() {
	}

	/// <summary>
	/// Copy constructor. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public CheckBox(CheckBox source)
		: base(source) {
		_checkBoxAlignment = source._checkBoxAlignment;
		ElementCheckBox = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ICheckBox)ElementCheckBox.Clone();
	}

	/// <summary>
	/// Image Alignment
	/// </summary>
	public System.Drawing.ContentAlignment CheckBoxAlignment {
		get => _checkBoxAlignment;
		set => _checkBoxAlignment = value;
	}

	/// <summary>
	/// Gets or sets the visual element used to draw the checkbox. Default is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.CheckBoxThemed.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ICheckBox ElementCheckBox {
		get => _elementCheckBox;
		set => _elementCheckBox = value;
	}

	// Clone
	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new CheckBox(this);
	}

	protected override void PrepareView(CellContext context) {
		base.PrepareView(context);

		PrepareVisualElementCheckBox(context);
	}

	protected virtual void PrepareVisualElementCheckBox(CellContext context) {
		ElementCheckBox.AnchorArea = new Sphere10.Framework.Drawing.AnchorArea(CheckBoxAlignment, false);

		var checkBoxModel = (Models.ICheckBox)context.Cell.Model.FindModel(typeof(Models.ICheckBox));
		var checkBoxStatus = checkBoxModel.GetCheckBoxStatus(context);

		if (context.CellRange.Contains(context.Grid.MouseCellPosition)) {
			if (checkBoxStatus.CheckEnable)
				ElementCheckBox.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Hot;
			else
				ElementCheckBox.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Disabled;
		} else {
			if (checkBoxStatus.CheckEnable)
				ElementCheckBox.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Normal;
			else
				ElementCheckBox.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Disabled;
		}

		ElementCheckBox.CheckBoxState = checkBoxStatus.CheckState;

		ElementText.Value = checkBoxStatus.Caption;
	}

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetElements() {
		if (ElementCheckBox != null)
			yield return ElementCheckBox;

		foreach (var v in GetBaseElements())
			yield return v;
	}

	private IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetBaseElements() {
		return base.GetElements();
	}
}
