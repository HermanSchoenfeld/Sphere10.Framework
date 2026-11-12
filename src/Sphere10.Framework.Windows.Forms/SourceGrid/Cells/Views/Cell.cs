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
using System.Windows.Forms;
using Tools;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Class to manage the visual aspect of a cell. This class can be shared beetween multiple cells.
/// </summary>
[Serializable]
public class Cell : ViewBase {
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IText _elementText = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI();
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IImage _elementImage = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image();

	/// <summary>
	/// Represents a default Model
	/// </summary>
	public readonly static Cell Default = new Cell();

	#region Constructors

	/// <summary>
	/// Use default setting and construct a read and write VisualProperties
	/// </summary>
	public Cell() => ElementsDrawMode = Sphere10.Framework.Windows.Forms.Drawing.ElementsDrawMode.Align;

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public Cell(Cell source)
		: base(source) {
		ElementImage = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IImage)source.ElementImage.Clone();
		ElementText = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IText)source.ElementText.Clone();
	}

	#endregion

	/// <summary>
	/// Gets or sets the IText visual element used to draw the cell text.
	/// Default is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IText ElementText {
		get => _elementText;
		set => _elementText = value;
	}

	/// <summary>
	/// Gets or sets the IImage visual element used to draw the cell image.
	/// Default is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.Image
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IImage ElementImage {
		get => _elementImage;
		set => _elementImage = value;
	}

	#region Clone

	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new Cell(this);

	#endregion

	#region Visual elements

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetElements() {
		if (ElementImage != null)
			yield return ElementImage;

		if (ElementText != null)
			yield return ElementText;
	}

	protected override void PrepareView(CellContext context) {
		base.PrepareView(context);

		PrepareVisualElementText(context);

		PrepareVisualElementImage(context);
	}

	/// <summary>
	/// Apply to the VisualElement specified the Image properties of the current View.
	/// Derived class can call this method to apply the settings to custom VisualElement.
	/// </summary>
	protected virtual void PrepareVisualElementText(CellContext context) {
		if (ElementText is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextRenderer) {
			var elementText = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextRenderer)ElementText;

			elementText.TextFormatFlags = TextFormatFlags.Default | TextFormatFlags.NoPrefix;
			if (WordWrap)
				elementText.TextFormatFlags |= TextFormatFlags.WordBreak;
			if (TrimmingMode == TrimmingMode.Char)
				elementText.TextFormatFlags |= TextFormatFlags.EndEllipsis;
			else if (TrimmingMode == TrimmingMode.Word)
				elementText.TextFormatFlags |= TextFormatFlags.WordEllipsis;
			elementText.TextFormatFlags |= Tools.WinForms.ContentAlignmentToTextFormatFlags(TextAlignment);
		} else if (ElementText is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI) {
			var elementTextGDI = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.TextGDI)ElementText;

			if (WordWrap)
				elementTextGDI.StringFormat.FormatFlags = (StringFormatFlags)0;
			else
				elementTextGDI.StringFormat.FormatFlags = StringFormatFlags.NoWrap;
			if (TrimmingMode == TrimmingMode.Char)
				elementTextGDI.StringFormat.Trimming = StringTrimming.EllipsisCharacter;
			else if (TrimmingMode == TrimmingMode.Word)
				elementTextGDI.StringFormat.Trimming = StringTrimming.EllipsisWord;
			else
				elementTextGDI.StringFormat.Trimming = StringTrimming.None;
			elementTextGDI.Alignment = TextAlignment;
		}

		ElementText.Font = GetDrawingFont(context.Grid);
		ElementText.ForeColor = ForeColor;
		//I have already set the TextFormatFlags for the alignment so the Anchor is not necessary. I have removed this code for performance reasons.
		//element.AnchorArea = new Sphere10.Framework.Drawing.AnchorArea(TextAlignment, false);

		ElementText.Value = context.DisplayText;
	}

	/// <summary>
	/// Apply to the VisualElement specified the Image properties of the current View.
	/// Derived class can call this method to apply the settings to custom VisualElement.
	/// </summary>
	protected virtual void PrepareVisualElementImage(CellContext context) {
		ElementImage.AnchorArea = new Sphere10.Framework.Drawing.AnchorArea(ImageAlignment, ImageStretch);

		//Read the image
		System.Drawing.Image img = null;
		var imgModel = (Models.IImage)context.Cell.Model.FindModel(typeof(Models.IImage));
		if (imgModel != null)
			img = imgModel.GetImage(context);
		ElementImage.Value = img;
		//ElementImage.AnchorArea = AnchorArea.HasBottom
	}

	#endregion
}
