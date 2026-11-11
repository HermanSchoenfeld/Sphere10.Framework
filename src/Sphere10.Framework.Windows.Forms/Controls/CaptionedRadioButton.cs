// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A radio button whose text is a bold <see cref="Caption"/> followed by a regular <see cref="Description"/>, such as "<b>Per Display</b>: unique effect per display".</summary>
/// <remarks>
/// Everything except the drawing is <see cref="RadioButton"/> behaviour: grouping with sibling radio buttons, <see cref="RadioButton.Checked"/>,
/// <see cref="RadioButton.CheckedChanged"/>, arrow-key navigation, accessibility and state observation. <see cref="Control.Text"/> follows the caption,
/// <see cref="CaptionSeparator"/> and description, so screen readers and mnemonics read the whole sentence. The background, glyph and its hot and
/// pressed states are painted by <see cref="RadioButton"/> itself, which renders the standard glyph at the window's own DPI on every display of a
/// mixed-DPI desktop; this control only draws the text and its focus rectangle. With <see cref="ButtonBase.AutoSize"/> the height is a standard
/// radio button's, so stacked buttons with default margins keep the designer's snap-line spacing.
/// </remarks>
public class CaptionedRadioButton : RadioButton {
	private const int LogicalGlyphSize = 13;
	private const int LogicalGlyphSpacing = 4;
	private const TextFormatFlags TextFormat = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.VerticalCenter;
	private Font _captionFont;
	private bool _paintingBase;

	public CaptionedRadioButton() => SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

	/// <summary>The emphasised leading words, drawn bold.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	[Localizable(true)]
	public string Caption {
		get => field;
		set {
			field = value ?? string.Empty;
			UpdateText();
		}
	} = string.Empty;

	/// <summary>The explanation drawn in the regular font after the caption and separator.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	[Localizable(true)]
	public string Description {
		get => field;
		set {
			field = value ?? string.Empty;
			UpdateText();
		}
	} = string.Empty;

	/// <summary>Drawn in the regular font between the caption and a non-empty description.</summary>
	[Category("Appearance")]
	[DefaultValue(": ")]
	[Localizable(true)]
	public string CaptionSeparator {
		get => field;
		set {
			field = value ?? string.Empty;
			UpdateText();
		}
	} = ": ";

	/// <summary>The combined caption, separator and description; set <see cref="Caption"/> and <see cref="Description"/> instead.</summary>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override string Text {
		get => _paintingBase ? string.Empty : base.Text;
		set => base.Text = value;
	}

	/// <summary>The width fits the bold caption and the description; the height is the standard radio button's for this font.</summary>
	public override Size GetPreferredSize(Size proposedSize) {
		var (caption, rest) = MeasureText();
		var glyph = LogicalToDeviceUnits(LogicalGlyphSize);
		var width = Padding.Horizontal + glyph + LogicalToDeviceUnits(LogicalGlyphSpacing) + caption.Width + rest.Width;
		var height = Math.Max(base.GetPreferredSize(proposedSize).Height, Padding.Vertical + Math.Max(glyph, Math.Max(caption.Height, rest.Height)));
		return new Size(width, height);
	}

	/// <summary>Hides the base focus rectangle, which would surround the empty text the base paints; <see cref="OnPaint"/> draws one around the real text.</summary>
	protected override bool ShowFocusCues => !_paintingBase && base.ShowFocusCues;

	protected override void OnPaint(PaintEventArgs e) {
		// The base paints the background and the standard glyph with no text; the caption and description are drawn after it.
		_paintingBase = true;
		try {
			base.OnPaint(e);
		} finally {
			_paintingBase = false;
		}
		var content = Rectangle.FromLTRB(Padding.Left, Padding.Top, ClientSize.Width - Padding.Right, ClientSize.Height - Padding.Bottom);
		var (caption, rest) = MeasureText();
		var textLeft = content.Left + LogicalToDeviceUnits(LogicalGlyphSize) + LogicalToDeviceUnits(LogicalGlyphSpacing);
		var color = Enabled ? ForeColor : SystemColors.GrayText;
		var captionBounds = new Rectangle(textLeft, content.Top, Math.Min(caption.Width, Math.Max(0, content.Right - textLeft)), content.Height);
		TextRenderer.DrawText(e.Graphics, Caption, CaptionFont, captionBounds, color, TextFormat | TextFormatFlags.EndEllipsis);
		var restBounds = Rectangle.FromLTRB(captionBounds.Right, content.Top, content.Right, content.Bottom);
		if (restBounds.Width > 0)
			TextRenderer.DrawText(e.Graphics, RestText, Font, restBounds, color, TextFormat | TextFormatFlags.EndEllipsis);
		if (Focused && ShowFocusCues) {
			var focus = Rectangle.FromLTRB(textLeft - 1, content.Top, Math.Min(content.Right, textLeft + caption.Width + rest.Width + 1), content.Bottom);
			ControlPaint.DrawFocusRectangle(e.Graphics, focus);
		}
	}

	protected override void OnFontChanged(EventArgs e) {
		ReleaseCaptionFont();
		base.OnFontChanged(e);
		FitToText();
	}

	protected override void OnAutoSizeChanged(EventArgs e) {
		base.OnAutoSizeChanged(e);
		FitToText();
	}

	protected override void Dispose(bool disposing) {
		if (disposing)
			ReleaseCaptionFont();
		base.Dispose(disposing);
	}

	private Font CaptionFont => _captionFont ??= new Font(Font, Font.Style | FontStyle.Bold);

	private string RestText => Description.Length == 0 ? string.Empty : CaptionSeparator + Description;

	private (Size Caption, Size Remainder) MeasureText() {
		var unbounded = new Size(int.MaxValue, int.MaxValue);
		var caption = Caption.Length == 0 ? Size.Empty : TextRenderer.MeasureText(Caption, CaptionFont, unbounded, TextFormat);
		var rest = RestText.Length == 0 ? Size.Empty : TextRenderer.MeasureText(RestText, Font, unbounded, TextFormat);
		return (caption, rest);
	}

	private void UpdateText() {
		base.Text = Caption + RestText;
		ReleaseCaptionFont();
		FitToText();
		Invalidate();
	}

	// A parent's layout sizes an auto-sized control through GetPreferredSize; this also covers a control without a parent yet.
	private void FitToText() {
		if (AutoSize)
			Size = GetPreferredSize(Size.Empty);
	}

	private void ReleaseCaptionFont() {
		_captionFont?.Dispose();
		_captionFont = null;
	}
}
