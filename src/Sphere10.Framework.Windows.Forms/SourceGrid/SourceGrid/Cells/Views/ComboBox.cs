// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

public class ComboBox : Cell {
	/// <summary>
	/// Represents a default CheckBox with the CheckBox image align to the Middle Center of the cell. You must use this VisualModel with a Cell of type ICellCheckBox.
	/// </summary>
	public new readonly static ComboBox Default = new ComboBox();

	#region Constructors

	static ComboBox() {
	}

	/// <summary>
	/// Use default setting and construct a read and write VisualProperties
	/// </summary>
	public ComboBox() {
		ElementDropDown.AnchorArea = new Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.AnchorArea(float.NaN, 0, 0, 0, false, false);
	}

	/// <summary>
	/// Copy constructor. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="p_Source"></param>
	public ComboBox(ComboBox p_Source)
		: base(p_Source) {
		ElementDropDown = (Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IDropDownButton)p_Source.ElementDropDown.Clone();
	}

	#endregion

	protected override void PrepareView(CellContext context) {
		base.PrepareView(context);

		PrepareVisualElementDropDown(context);
	}

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement> GetElements() {
		if (ElementDropDown != null)
			yield return ElementDropDown;

		foreach (Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement v in GetBaseElements())
			yield return v;
	}
	private IEnumerable<Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IVisualElement> GetBaseElements() {
		return base.GetElements();
	}

	private Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IDropDownButton mElementDropDown = new Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.DropDownButtonThemed();

	/// <summary>
	/// Gets or sets the visual element used to draw the checkbox. Default is Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.CheckBoxThemed.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements.IDropDownButton ElementDropDown {
		get { return mElementDropDown; }
		set { mElementDropDown = value; }
	}


	protected virtual void PrepareVisualElementDropDown(CellContext context) {
		if (context.CellRange.Contains(context.Grid.MouseCellPosition)) {
			ElementDropDown.Style = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.ButtonStyle.Hot;
		} else {
			ElementDropDown.Style = Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.ButtonStyle.Normal;
		}
	}

	#region Clone

	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new ComboBox(this);
	}

	#endregion

}

