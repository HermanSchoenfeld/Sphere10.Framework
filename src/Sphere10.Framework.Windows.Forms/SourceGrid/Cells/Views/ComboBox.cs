// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

public class ComboBox : Cell {

	/// <summary>
	/// Represents a default CheckBox with the CheckBox image align to the Middle Center of the cell. You must use this VisualModel with a Cell of type ICellCheckBox.
	/// </summary>
	public new readonly static ComboBox Default = new ComboBox();

	// Constructors
	static ComboBox() {
	}

	/// <summary>
	/// Use default setting and construct a read and write VisualProperties
	/// </summary>
	public ComboBox() {
		ElementDropDown.AnchorArea = new Sphere10.Framework.Drawing.AnchorArea(float.NaN, 0, 0, 0, false, false);
	}

	/// <summary>
	/// Copy constructor. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public ComboBox(ComboBox source)
		: base(source) {
		ElementDropDown = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IDropDownButton)source.ElementDropDown.Clone();
	}

	/// <summary>
	/// Gets or sets the visual element used to draw the dropdown button. Default is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.DropDownButtonThemed.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IDropDownButton ElementDropDown { get; set; } = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.DropDownButtonThemed();

	// Clone
	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new ComboBox(this);
	}

	protected override void PrepareView(CellContext context) {
		base.PrepareView(context);

		PrepareVisualElementDropDown(context);
	}

	protected virtual void PrepareVisualElementDropDown(CellContext context) {
		if (context.CellRange.Contains(context.Grid.MouseCellPosition))
			ElementDropDown.Style = Sphere10.Framework.Windows.Forms.Drawing.ButtonStyle.Hot;
		else
			ElementDropDown.Style = Sphere10.Framework.Windows.Forms.Drawing.ButtonStyle.Normal;
	}

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetElements() {
		if (ElementDropDown != null)
			yield return ElementDropDown;

		foreach (var v in GetBaseElements())
			yield return v;
	}

	private IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetBaseElements() {
		return base.GetElements();
	}
}
