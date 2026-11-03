// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Base abstract class to manage the visual aspect of a cell. This class can be shared beetween multiple cells.
/// </summary>
[Serializable]
public abstract class ViewBase : Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ContainerBase, IView {
	// Format
	private bool _imageStretch = false;
	private System.Drawing.ContentAlignment _imageAlignment;
	private Font _font = null;
	private Color _foreColor;
	private bool _wordWrap = false;
	private TrimmingMode _trimmingMode = TrimmingMode.Char;
	private System.Drawing.ContentAlignment _textAlignment;

	/// <summary>
	/// A default RectangleBorder instance with a 1 pixed LightGray border on bottom and right side
	/// </summary>
	public static RectangleBorder DefaultBorder = new RectangleBorder(new BorderLine(Color.LightGray, 1), new BorderLine(Color.LightGray, 1));
	public static Sphere10.Framework.Drawing.Padding DefaultPadding = new Sphere10.Framework.Drawing.Padding(2);
	public static Color DefaultBackColor = Color.FromKnownColor(KnownColor.Window);
	public static Color DefaultForeColor = Color.FromKnownColor(KnownColor.WindowText);
	public static System.Drawing.ContentAlignment DefaultAlignment = System.Drawing.ContentAlignment.MiddleLeft;

	// Constructors
	/// <summary>
	/// Use default setting
	/// </summary>
	public ViewBase() {
		Background = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid();

		Padding = DefaultPadding;
		ForeColor = DefaultForeColor;
		BackColor = DefaultBackColor;
		Border = DefaultBorder;
		TextAlignment = DefaultAlignment;
		ImageAlignment = DefaultAlignment;
	}

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public ViewBase(ViewBase source)
		: base(source) {
		ForeColor = source.ForeColor;

		BackColor = source.BackColor;
		Border = source.Border;
		Padding = source.Padding;

		//Duplicate the reference fields
		Font tmpFont = null;
		if (source.Font != null)
			tmpFont = (Font)source.Font.Clone();

		Font = tmpFont;

		WordWrap = source.WordWrap;
		TextAlignment = source.TextAlignment;
		TrimmingMode = source.TrimmingMode;
		ImageAlignment = source.ImageAlignment;
		ImageStretch = source.ImageStretch;
	}

	/// <summary>
	/// True to stretch the image to all the cell
	/// </summary>
	public bool ImageStretch {
		get => _imageStretch;
		set => _imageStretch = value;
	}

	/// <summary>
	/// Image Alignment
	/// </summary>
	public System.Drawing.ContentAlignment ImageAlignment {
		get => _imageAlignment;
		set => _imageAlignment = value;
	}

	/// <summary>
	/// If null the grid font is used
	/// </summary>
	public Font Font {
		get => _font;
		set => _font = value;
	}

	/// <summary>
	/// ForeColor of the cell
	/// </summary>
	public Color ForeColor {
		get => _foreColor;
		set => _foreColor = value;
	}

	/// <summary>
	/// Word Wrap, default false.
	/// </summary>
	public bool WordWrap {
		get => _wordWrap;
		set => _wordWrap = value;
	}

	/// <summary>
	/// TrimmingMode, default Char.
	/// </summary>
	public TrimmingMode TrimmingMode {
		get => _trimmingMode;
		set => _trimmingMode = value;
	}

	/// <summary>
	/// Text Alignment.
	/// </summary>
	public System.Drawing.ContentAlignment TextAlignment {
		get => _textAlignment;
		set => _textAlignment = value;
	}

	/// <summary>
	/// Returns the color of the Background. If the Background it isn't an instance of BackgroundSolid then returns DefaultBackColor
	/// </summary>
	public Color BackColor {
		get {
			if (Background is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid)
				return ((Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid)Background).BackColor;
			return DefaultBackColor;
		}
		set {
			if (Background is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid)
				((Sphere10.Framework.Windows.Forms.Drawing.VisualElements.BackgroundSolid)Background).BackColor = value;
		}
	}

	/// <summary>
	/// The border of the Cell. Usually it is an instance of the Sphere10.Framework.Drawing.RectangleBorder structure.
	/// </summary>
	public new IBorder Border {
		get => base.Border;
		set => base.Border = value;
	}

	/// <summary>
	/// The padding of the cell. Usually it is 2 pixel on all sides.
	/// </summary>
	public new Sphere10.Framework.Drawing.Padding Padding {
		get => base.Padding;
		set => base.Padding = value;
	}

	/// <summary>
	/// Gets or sets a property that specify how to draw the elements.
	/// </summary>
	public new ElementsDrawMode ElementsDrawMode {
		get => base.ElementsDrawMode;
		set => base.ElementsDrawMode = value;
	}

	/// <summary>
	/// Background of the cell. Usually it is an instance of BackgroundSolid
	/// </summary>
	public new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement Background {
		get => base.Background;
		set => base.Background = value;
	}

	/// <summary>
	/// Get the font of the cell, check if the current font is null and in this case return the grid font
	/// </summary>
	/// <param name="grid"></param>
	/// <returns></returns>
	public virtual Font GetDrawingFont(GridVirtual grid) {
		if (Font == null)
			return grid.Font;
		return Font;
	}

	// DrawCell
	/// <summary>
	/// Draw the cell specified
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="graphics">Paint arguments</param>
	/// <param name="rectangle">Rectangle where draw the current cell</param>
	public void DrawCell(CellContext cellContext, Sphere10.Framework.Drawing.GraphicsCache graphics, RectangleF rectangle) {
		PrepareView(cellContext);

		Draw(graphics, rectangle);
	}

	// Measure (GetRequiredSize)
	/// <summary>
	/// Returns the minimum required size of the current cell, calculating using the current DisplayString, Image and Borders informations.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="maxLayoutArea">SizeF structure that specifies the maximum layout area for the text. If width or height are zero the value is set to a default maximum value.</param>
	/// <returns></returns>
	public Size Measure(CellContext cellContext, Size maxLayoutArea) {
		using (var measure = new Sphere10.Framework.Windows.Forms.Drawing.GraphicsMeasurement(cellContext.Grid)) {
			PrepareView(cellContext);

			var measureSize = Measure(measure, SizeF.Empty, maxLayoutArea);
			return Size.Ceiling(measureSize);
		}
	}

	/// <summary>
	/// Prepare the current View before drawing. Override this method for customize the specials VisualModel that you need to create. Always calls the base PrepareView.
	/// </summary>
	/// <param name="context">Current context. Cell to draw. This property is set before drawing. Only inside the PrepareView you can access this property.</param>
	protected virtual void PrepareView(CellContext context) {
	}
}
