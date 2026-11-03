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
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

/// <summary>Draws rich text through a native rich-edit surface owned by each render operation.</summary>
[Serializable]
public class RichTextGDI : RichText {
	/// <summary>Creates an empty rich-text visual.</summary>
	public RichTextGDI() {
	}

	/// <summary>Creates a visual for the supplied rich-text value.</summary>
	public RichTextGDI(Sphere10.Framework.Windows.Forms.RichText value)
		: base(value) {
	}

	/// <summary>Copies the visual configuration.</summary>
	public RichTextGDI(RichTextGDI other)
		: base(other) {
	}

	/// <summary>Copies the visual without sharing a render control.</summary>
	public override object Clone() {
		return new RichTextGDI(this);
	}

	/// <summary>Draws rich text without retaining a native editor between visual elements.</summary>
	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		// Do not call base.OnDraw, which would overwrite the native rich-text rendering.
		using var bitmap = area.Width > 0 && area.Height > 0 ? GetBitmapArea(area, RotateFlipType) : new Bitmap(1, 1);
		if (area.Width > 0 && area.Height > 0) {
			using var editor = CreateRenderControl();
			try {
				Tools.Windows.Win32.FormatRichText(editor.Handle, bitmap, false, 0, editor.Text.Length);
			} finally {
				Tools.Windows.Win32.ReleaseRichTextFormat(editor.Handle);
			}
		}

		DrawImage(graphics, area, bitmap);
	}

	/// <summary>Measures the plain-text content with the configured font.</summary>
	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		var text = string.Empty;
		if (Value != null && Value.Rtf.Length > 0)
			text = RichTextConversion.RichTextToString(Value);
		return measure.Graphics.MeasureString(text, Font, maxSize);
	}

	/// <summary>Creates the native rendering surface; the calling draw operation disposes it.</summary>
	protected virtual RichTextBoxEx CreateRenderControl() {
		var editor = new RichTextBoxEx {
			BorderStyle = System.Windows.Forms.BorderStyle.None,
			AutoSize = false
		};
		try {
			editor.Clear();
			// The rendering surface consumes RTF directly without a grid editor's value converter.
			if (!string.IsNullOrEmpty(Value?.Rtf))
				editor.Rtf = Value.Rtf;
			if (ForeColor != Color.FromKnownColor(KnownColor.WindowText)) {
				editor.SelectAll();
				editor.SelectionColor = ForeColor;
			}

			if (TextAlignment != ContentAlignment.MiddleLeft) {
				editor.SelectAll();
				editor.SelectionAlignment = Tools.WinForms.ContentToHorizontalAlignment(TextAlignment);
			}

			if (Font != Control.DefaultFont) {
				editor.SelectAll();
				editor.SelectionFont = Font;
			}

			return editor;
		} catch {
			editor.Dispose();
			throw;
		}
	}

	/// <summary>Allocates a bitmap with swapped dimensions for quarter-turn rotation.</summary>
	protected Bitmap GetBitmapArea(RectangleF area, RotateFlipType rotateFlipType) {
		var height = (int)area.Height;
		var width = (int)area.Width;
		// Quarter turns exchange the source dimensions before the bitmap is rotated.
		if (rotateFlipType == RotateFlipType.Rotate90FlipNone || rotateFlipType == RotateFlipType.Rotate90FlipX || rotateFlipType == RotateFlipType.Rotate90FlipY || rotateFlipType == RotateFlipType.Rotate90FlipXY) {
			height = width;
			width = (int)area.Height;
		}

		return new Bitmap(width, height);
	}

	/// <summary>Applies transparency and rotation before copying the rendered bitmap.</summary>
	protected virtual void DrawImage(GraphicsCache graphics, RectangleF area, Bitmap bitmap) {
		bitmap.MakeTransparent(Color.White);
		bitmap.RotateFlip(RotateFlipType);
		graphics.Graphics.DrawImage(bitmap, area);
	}
}
