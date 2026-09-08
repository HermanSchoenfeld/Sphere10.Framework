// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Drawing.VisualElements;

[Serializable]
public class RichTextGDI : RichText {

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public RichTextGDI()
		: base() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public RichTextGDI(Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.RichText value)
		: base(value) {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public RichTextGDI(RichTextGDI other)
		: base(other) {
	}

	/// <summary>
	/// Init rich text box control
	/// </summary>
	protected virtual void AssertRichTextBoxEditor() {
		if (RichTextBoxEditor == null) {
			RichTextBoxEditor = new SourceGrid.Cells.Editors.RichTextBox();
		}

		RichTextBoxEditor.Control.Clear();

		RichTextBoxEditor.Control.Value = Value;
		if (ForeColor != Color.FromKnownColor(KnownColor.WindowText)) {
			RichTextBoxEditor.Control.SelectAll();
			RichTextBoxEditor.Control.SelectionColor = ForeColor;
		}
		if (TextAlignment != ContentAlignment.MiddleLeft) {
			RichTextBoxEditor.Control.SelectAll();
			RichTextBoxEditor.Control.SelectionAlignment = Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.Utilities.ContentToHorizontalAlignment(TextAlignment);
		}
		if (Font != System.Windows.Forms.Control.DefaultFont) {
			RichTextBoxEditor.Control.SelectAll();
			RichTextBoxEditor.Control.SelectionFont = Font;
		}
	}

	#endregion

	#region Members

	/// <summary>
	/// Will be used to draw picture of rich text. Is static for performance reasons
	/// and therefore needs to be locked.
	/// </summary>
	private static SourceGrid.Cells.Editors.RichTextBox m_RichTextBoxEditor = null;

	public SourceGrid.Cells.Editors.RichTextBox RichTextBoxEditor {
		get { return m_RichTextBoxEditor; }
		set { m_RichTextBoxEditor = value; }
	}

	#endregion

	/// <summary>Calculates or renders the rich-edit control into the supplied bitmap.</summary>
	public int FormatRange(bool measureOnly, SourceGrid.DevAgeControls.DevAgeRichTextBox rtb, ref Bitmap b, int charFrom, int charTo)
		=> Tools.Windows.Win32.FormatRichText(rtb.Handle, b, measureOnly, charFrom, charTo);

	/// <summary>Releases native rich-edit formatting data after printing.</summary>
	public void FormatRangeDone(SourceGrid.DevAgeControls.DevAgeRichTextBox rtb)
		=> Tools.Windows.Win32.ReleaseRichTextFormat(rtb.Handle);

	#region Draw

	/// <summary>
	/// Helper method to get bitmap with size according its area and rotate flip type
	/// </summary>
	/// <param name="area"></param>
	/// <param name="rotateFlipType"></param>
	/// <returns></returns>
	protected Bitmap GetBitmapArea(RectangleF area, RotateFlipType rotateFlipType) {
		int height = (int)area.Height;
		int width = (int)area.Width;

		// when rotation is 90 (resp. 270 which is equivalent)
		// height and width values need to be swapped
		if (rotateFlipType == RotateFlipType.Rotate90FlipNone
		    || rotateFlipType == RotateFlipType.Rotate90FlipX
		    || rotateFlipType == RotateFlipType.Rotate90FlipY
		    || rotateFlipType == RotateFlipType.Rotate90FlipXY) {
			height = width;
			width = (int)area.Height;
		}

		return new Bitmap(width, height);
	}

	/// <summary>
	/// Render RichTextBox as GDI
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		// Do not call base.OnDraw as it would overwrite our drawing
		//base.OnDraw(graphics, area);

		lock (this) {
			// create bitmap
			Bitmap bmp = null;
			if (area.Width > 0 && area.Height > 0) {
				AssertRichTextBoxEditor();
				bmp = GetBitmapArea(area, RotateFlipType);

				// render image
				FormatRange(false,
					RichTextBoxEditor.Control,
					ref bmp,
					0,
					RichTextBoxEditor.Control.Text.Length);
				FormatRangeDone(RichTextBoxEditor.Control);
			} else {
				// create empty picture, in case the area is empty
				bmp = new Bitmap(1, 1);
			}

			DrawImage(graphics, area, bmp);
		}
	}

	/// <summary>
	/// Draw actual picture
	/// </summary>
	protected virtual void DrawImage(GraphicsCache graphics, RectangleF area, Bitmap bmp) {
		bmp.MakeTransparent(Color.White);
		bmp.RotateFlip(RotateFlipType);
		graphics.Graphics.DrawImage(bmp, area);
	}

	#endregion

	#region Measure

	/// <summary>
	/// Measure the current content of the VisualElement.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="maxSize">If empty is not used.</param>
	/// <returns></returns>
	protected override SizeF OnMeasureContent(MeasureHelper measure, System.Drawing.SizeF maxSize) {
		String s = String.Empty;

		if (Value != null && Value.Rtf.Length > 0) {
			s = Sphere10.Framework.Windows.Forms.SourceGrid.DevAgeControls.RichTextConversion.RichTextToString(Value);
		}

		return measure.Graphics.MeasureString(s, Font, maxSize);
	}

	#endregion

	#region Clone

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new RichTextGDI(this);
	}

	#endregion

}

