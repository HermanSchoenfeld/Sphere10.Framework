// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A block selector button that an <see cref="ApplicationBar"/> stacks beneath its menu. It is drawn in the bar's block selector
/// shape and colour style, resolved from its explorer bar theme: a gradient per state (idle, hot, pressed or selected), the header's
/// title font and the header's padding. As a <see cref="Button"/>, it is a push button to keyboard users, assistive technology and
/// UI automation. Outside an <see cref="ApplicationBar"/> it paints as a standard button.
/// </summary>
public class ApplicationBarButton : Button {
	/// <summary>Raised when a click selects this button, which the owning bar turns into a block selection.</summary>
	public event EventHandler ButtonStateChanged;

	private const int LogicalImageTextGap = 6;
	private const int BevelAlpha = 110;
	private bool _pushed;
	private bool _hovering;

	public ApplicationBarButton()
		=> SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

	/// <summary>Whether this button is the selected one in its bar.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Pressed {
		get;
		set {
			field = value;
			Invalidate();
		}
	}

	/// <summary>The bar whose theme this button follows; null outside a bar.</summary>
	internal ApplicationBar Bar { get; set; }

	protected override void OnPaint(PaintEventArgs pevent) {
		var theme = Bar?.Theme;
		if (theme == null) {
			base.OnPaint(pevent);
			return;
		}
		var graphics = pevent.Graphics;
		graphics.Clear(Bar.StripColor);
		if (Width <= 1 || Height <= 1)
			return;
		var appearance = Bar.Appearance;
		var state = Pressed ? ButtonState.Selected : !Enabled ? ButtonState.Normal : _pushed ? ButtonState.Pushed : _hovering ? ButtonState.Hot : ButtonState.Normal;
		var (start, end) = state switch {
			ButtonState.Selected => (appearance.SelectedStart, appearance.SelectedEnd),
			ButtonState.Hot => (appearance.HotStart, appearance.HotEnd),
			ButtonState.Pushed => (appearance.HotEnd, appearance.HotStart),
			_ => (appearance.NormalStart, appearance.NormalEnd)
		};
		var radius = LogicalToDeviceUnits(appearance.CornerRadius);
		var face = new Rectangle(0, 0, Width, Height);
		using var outline = Tools.Drawing.CreateRoundedRectanglePath(radius > 0 ? new Rectangle(0, 0, Width - 1, Height - 1) : face, radius);
		graphics.SmoothingMode = radius > 0 ? SmoothingMode.AntiAlias : SmoothingMode.None;

		// The face: the state's gradient, then an optional sheen over its upper half.
		using (var fill = new LinearGradientBrush(face, ToEnabledColor(start), ToEnabledColor(end), appearance.GradientMode)) {
			if (appearance.GradientOffset > 0f && appearance.GradientOffset < 1f) {
				fill.InterpolationColors = new ColorBlend {
					Colors = [fill.LinearColors[0], fill.LinearColors[0], fill.LinearColors[1]],
					Positions = [0f, appearance.GradientOffset, 1f]
				};
			}
			graphics.FillPath(fill, outline);
		}
		if (appearance.GlossAlpha > 0 && state != ButtonState.Pushed) {
			var upper = new Rectangle(0, 0, Width, Math.Max(1, Height / 2));
			using var sheen = new LinearGradientBrush(new Rectangle(upper.X, upper.Y - 1, upper.Width, upper.Height + 1), Color.FromArgb(appearance.GlossAlpha, Color.White), Color.FromArgb(appearance.GlossAlpha / 4, Color.White), LinearGradientMode.Vertical);
			var clip = graphics.Save();
			graphics.SetClip(outline);
			graphics.FillRectangle(sheen, upper);
			graphics.Restore(clip);
		}

		// A light inner top edge raises a stacked button; a pressed one drops it.
		var line = Math.Max(1, LogicalToDeviceUnits(1));
		if (appearance.Stacked && state != ButtonState.Pushed) {
			using var bevel = new Pen(Color.FromArgb(BevelAlpha, Color.White), line);
			graphics.DrawLine(bevel, 0, line + line / 2, Width, line + line / 2);
		}

		// Stacked buttons share a separator along their top edge; spaced or rounded buttons draw a full frame.
		if (appearance.Border.A > 0) {
			using var border = new Pen(ToEnabledColor(appearance.Border), line);
			if (appearance.Stacked)
				graphics.DrawLine(border, 0, line / 2, Width, line / 2);
			else
				graphics.DrawPath(border, outline);
		}
		graphics.SmoothingMode = SmoothingMode.Default;
		PaintContent(graphics, theme, appearance, state switch {
			ButtonState.Selected => appearance.SelectedText,
			ButtonState.Hot or ButtonState.Pushed => appearance.HotText,
			_ => appearance.NormalText
		});
		if (Focused && ShowFocusCues)
			ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(face, -LogicalToDeviceUnits(3), -LogicalToDeviceUnits(3)));
	}

	// A click selects an unselected button; clicking the selected one changes nothing.
	protected override void OnClick(EventArgs e) {
		if (Pressed)
			return;
		Pressed = true;
		ButtonStateChanged?.Invoke(this, EventArgs.Empty);
		base.OnClick(e);
	}

	protected override void OnMouseDown(MouseEventArgs mevent) {
		if (mevent.Button == MouseButtons.Left) {
			_pushed = true;
			Invalidate();
		}
		base.OnMouseDown(mevent);
	}

	protected override void OnMouseUp(MouseEventArgs mevent) {
		if (_pushed) {
			_pushed = false;
			Invalidate();
		}
		base.OnMouseUp(mevent);
	}

	protected override void OnMouseEnter(EventArgs e) {
		_hovering = true;
		base.OnMouseEnter(e);
		Invalidate();
	}

	protected override void OnMouseLeave(EventArgs e) {
		_hovering = false;
		_pushed = false;
		base.OnMouseLeave(e);
		Invalidate();
	}

	protected override void OnEnabledChanged(EventArgs e) {
		base.OnEnabledChanged(e);
		Invalidate();
	}

	// Lays out the image and caption with the header's padding, mirrored for right-to-left layouts.
	private void PaintContent(Graphics graphics, ExplorerBarInfo theme, BlockSelectorAppearance appearance, Color textColor) {
		var header = theme.Header;
		var padding = Pressed ? header.SpecialPadding : header.NormalPadding;
		var x = LogicalToDeviceUnits(Math.Max(padding.Left, 1));
		var right = LogicalToDeviceUnits(Math.Max(padding.Right, 1));
		if (Image != null) {
			var size = LogicalToDeviceUnits(appearance.IconSize);
			PaintImage(graphics, Mirror(new Rectangle(x, (Height - size) / 2, size, size)));
			x += size + LogicalToDeviceUnits(LogicalImageTextGap);
		}
		if (string.IsNullOrEmpty(Text))
			return;
		using var format = new StringFormat {
			Alignment = StringAlignment.Near,
			LineAlignment = StringAlignment.Center,
			FormatFlags = StringFormatFlags.NoWrap,
			Trimming = StringTrimming.EllipsisCharacter
		};
		if (RightToLeft == RightToLeft.Yes)
			format.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
		using var brush = new SolidBrush(Enabled ? textColor : SystemColors.GrayText);
		graphics.DrawString(Text, header.TitleFont ?? Font, brush, Mirror(new Rectangle(x, 0, Math.Max(0, Width - x - right), Height)), format);
	}

	private void PaintImage(Graphics graphics, Rectangle bounds) {
		var state = graphics.Save();
		graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
		graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
		if (Enabled) {
			graphics.DrawImage(Image, bounds);
		} else {
			using var attributes = new ImageAttributes();
			attributes.SetColorMatrix(ColorMatrices.CreateSaturation(0f));
			graphics.DrawImage(Image, bounds, 0, 0, Image.Width, Image.Height, GraphicsUnit.Pixel, attributes);
		}
		graphics.Restore(state);
	}

	private Color ToEnabledColor(Color color) {
		if (Enabled)
			return color;
		var brightness = (int)Math.Round(color.GetBrightness() * 255);
		return Color.FromArgb(color.A, brightness, brightness, brightness);
	}

	private Rectangle Mirror(Rectangle bounds) => RightToLeft == RightToLeft.Yes ? new Rectangle(Width - bounds.Right, bounds.Y, bounds.Width, bounds.Height) : bounds;

	#region Inner Types

	private enum ButtonState {
		Normal,
		Hot,
		Pushed,
		Selected
	}

	#endregion
}
