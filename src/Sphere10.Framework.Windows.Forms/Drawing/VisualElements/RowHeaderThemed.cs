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
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public class RowHeaderThemed : RowHeaderBase {
	/// <summary>
	/// Standard header used when the XP style are disabled.
	/// </summary>
	private RowHeader _standardRowHeader = new RowHeader();
	private ColumnHeaderThemed _colHeaderThemed = new ColumnHeaderThemed();
	private bool _rotateColHeaderIfNotDefined = true;

	/// <summary>
	/// Default constructor
	/// </summary>
	public RowHeaderThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public RowHeaderThemed(RowHeaderThemed other)
		: base(other) {
	}

	public override ControlDrawStyle Style {
		get {
			return base.Style;
		}

		set {
			base.Style = value;
			_standardRowHeader.Style = value;
			_colHeaderThemed.Style = value;
		}
	}

	/// <summary>
	/// Gets or sets a property to indicate to rotate the standard column header if the row header is not defined in the current theme.
	/// This is expecially usefull because for many themes the row header is not defined. Default is true.
	/// </summary>
	public virtual bool RotateColHeaderIfNotDefined {
		get {
			return _rotateColHeaderIfNotDefined;
		}

		set {
			_rotateColHeaderIfNotDefined = value;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new RowHeaderThemed(this);
	}

	public override RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		backGroundArea = base.GetBackgroundContentRectangle(measure, backGroundArea);
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			return GetRenderer(GetBackgroundElement()).GetBackgroundContentRectangle(measure.Graphics, Rectangle.Round(backGroundArea));
		return _standardRowHeader.GetBackgroundContentRectangle(measure, backGroundArea);
	}

	public override SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			var content = new Rectangle(new Point(0, 0), Size.Ceiling(contentSize));
			contentSize = GetRenderer(GetBackgroundElement()).GetBackgroundExtent(measure.Graphics, content).Size;
		} else
			contentSize = _standardRowHeader.GetBackgroundExtent(measure, contentSize);
		return base.GetBackgroundExtent(measure, contentSize);
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		base.OnDraw(graphics, area);
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			GetRenderer(GetBackgroundElement()).DrawBackground(graphics.Graphics, Rectangle.Round(area));
		else {
			if (RotateColHeaderIfNotDefined) {
				//Swap Height and Width to rotate the bitmap (note I can't use the Graphics transformation matrix because the theme are drawed without using GDI+ I think and the trasformation matrix has no effect)
				var bufferBitmap = new Bitmap((int)area.Height, (int)area.Width);
				var bufferGraphics = Graphics.FromImage(bufferBitmap);
				try {
					var bufferRectangle = new Rectangle(0, 0, (int)area.Height, (int)area.Width);
					using (var cache = new GraphicsCache(bufferGraphics)) {
						_colHeaderThemed.Draw(cache, bufferRectangle);
					}

					bufferBitmap.RotateFlip(RotateFlipType.Rotate270FlipY);
					graphics.Graphics.DrawImage(bufferBitmap, area);
				} finally {
					bufferGraphics.Dispose();
					bufferBitmap.Dispose();
				}
			} else
				_standardRowHeader.Draw(graphics, area);
		}
	}

	protected VisualStyleElement GetBackgroundElement() {
		if (Style == ControlDrawStyle.Hot)
			return VisualStyleElement.Header.ItemLeft.Hot;
		if (Style == ControlDrawStyle.Pressed)
			return VisualStyleElement.Header.ItemLeft.Pressed;
		return VisualStyleElement.Header.ItemLeft.Normal;
	}

	/// <summary>
	/// Gets the System.Windows.Forms.VisualStyles.VisualStyleRenderer to draw the specified element.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	protected System.Windows.Forms.VisualStyles.VisualStyleRenderer GetRenderer(VisualStyleElement element) {
		return new System.Windows.Forms.VisualStyles.VisualStyleRenderer(element);
	}
}
