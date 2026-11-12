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
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

[Serializable]
public class RichTextBox : Cell {
	#region Properties

	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IRichText _elementRichText = null;

	/// <summary>
	/// Rotate flip type
	/// </summary>
	private RotateFlipType _rotateFlipType = RotateFlipType.RotateNoneFlipNone;

	/// <summary>
	/// Represents a rich text box
	/// </summary>
	public new readonly static RichTextBox Default = new RichTextBox();

	#region Constructors

	/// <summary>
	/// Use default setting and construct a read and write VisualProperties
	/// </summary>
	public RichTextBox() => ElementRichText = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.RichTextGDI();

	/// <summary>
	/// Copy constructor. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public RichTextBox(RichTextBox source)
		: base(source) {
		ElementRichText = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IRichText)source.ElementRichText.Clone();
	}

	#endregion

	/// <summary>
	/// Gets or sets the IText visual element used to draw the cell rich text.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IRichText ElementRichText {
		get => _elementRichText;
		set => _elementRichText = value;
	}

	public RotateFlipType RotateFlipType {
		get => _rotateFlipType;
		set => _rotateFlipType = value;
	}

	#endregion

	#region Clone

	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new RichTextBox(this);

	#endregion

	#region Preparing

	protected override void PrepareView(CellContext context) {
		// Do not start base prepare view, as it will render the text as normal TextGDI.
		// base.PrepareView(context);

		PrepareVisualElementRichTextBox(context);
	}

	protected virtual void PrepareVisualElementRichTextBox(CellContext context) {
		ElementRichText.Value = context.Cell.Model.ValueModel.GetValue(context) as Sphere10.Framework.Windows.Forms.RichText;
		ElementRichText.ForeColor = ForeColor;
		ElementRichText.TextAlignment = TextAlignment;
		ElementRichText.Font = GetDrawingFont(context.Grid);
		ElementRichText.RotateFlipType = RotateFlipType;
	}

	#endregion

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetElements() {
		if (ElementRichText != null)
			yield return ElementRichText;

		foreach (var v in GetBaseElements())
			yield return v;
	}

	private IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetBaseElements() => base.GetElements();
}
