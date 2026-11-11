// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>An on/off switch drawn as a thumb sliding in a rounded track.</summary>
/// <remarks>
/// Everything except the drawing is <see cref="CheckBox"/> behaviour: <see cref="CheckBox.Checked"/>, <see cref="CheckBox.CheckedChanged"/>,
/// <see cref="CheckBox.AutoCheck"/>, Space toggling, data binding, accessibility and state observation. The track sits at the leading edge and a
/// non-empty <see cref="Control.Text"/> is drawn beside it as the caption; <see cref="OnText"/> and <see cref="OffText"/> are drawn inside the
/// track on the side away from the thumb. Metrics are logical pixels scaled to the control's DPI. A switch that is not auto-sized and is taller
/// than its preferred track grows the track to fill its height, keeping the track's proportions or the width its texts need, as far as the width
/// beside the caption allows, so hosts such as a banner can size it.
/// </remarks>
public class ToggleSwitch : CheckBox {
	private readonly record struct SwitchLayout(Rectangle Track, Rectangle Caption);

	private const int LogicalDpi = 96;
	private const int LogicalTrackWidth = 44;
	private const int LogicalTrackHeight = 22;
	private const int LogicalThumbInset = 2;
	private const int LogicalTrackTextPadding = 6;
	private const int LogicalCaptionSpacing = 6;
	private const int AnimationDurationMilliseconds = 120;
	private const int AnimationFrameMilliseconds = 15;
	private const TextFormatFlags TrackTextFormat = TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;
	private Timer _animationTimer;
	private long _animationStartTimestamp;
	private float _animationStartPosition;
	private float _thumbPosition;
	private int _deviceDpi;

	/// <summary>Creates an unchecked switch measured at the current DPI.</summary>
	public ToggleSwitch() {
		SetStyle(
			ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor,
			true
		);
		_deviceDpi = DeviceDpi;
		_thumbPosition = TargetThumbPosition;
	}

	/// <summary>Text drawn inside the track while the switch is on, on the side away from the thumb.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	[Localizable(true)]
	[Description("Text drawn inside the track while the switch is on, on the side away from the thumb.")]
	public string OnText {
		get => field;
		set {
			value ??= string.Empty;
			if (field == value)
				return;
			field = value;
			RefreshTrackMetrics();
		}
	} = string.Empty;

	/// <summary>Text drawn inside the track while the switch is off, on the side away from the thumb.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	[Localizable(true)]
	[Description("Text drawn inside the track while the switch is off, on the side away from the thumb.")]
	public string OffText {
		get => field;
		set {
			value ??= string.Empty;
			if (field == value)
				return;
			field = value;
			RefreshTrackMetrics();
		}
	} = string.Empty;

	/// <summary>The track color while the switch is on.</summary>
	[Category("Appearance")]
	[Description("The track color while the switch is on.")]
	public Color OnBackColor {
		get => field;
		set {
			if (field == value)
				return;
			field = value;
			Invalidate();
		}
	} = SystemColors.Highlight;

	/// <summary>The track color while the switch is off.</summary>
	[Category("Appearance")]
	[Description("The track color while the switch is off.")]
	public Color OffBackColor {
		get => field;
		set {
			if (field == value)
				return;
			field = value;
			Invalidate();
		}
	} = SystemColors.ControlDark;

	/// <summary>The color of the circular thumb.</summary>
	[Category("Appearance")]
	[Description("The color of the circular thumb.")]
	public Color ThumbColor {
		get => field;
		set {
			if (field == value)
				return;
			field = value;
			Invalidate();
		}
	} = SystemColors.Window;

	/// <summary>The color of <see cref="OnText"/> and <see cref="OffText"/> inside the track.</summary>
	[Category("Appearance")]
	[Description("The color of the on and off texts inside the track.")]
	public Color TrackForeColor {
		get => field;
		set {
			if (field == value)
				return;
			field = value;
			Invalidate();
		}
	} = Color.White;

	/// <summary>Whether the thumb slides to its new position when the state changes while the switch is on screen.</summary>
	[Category("Behavior")]
	[DefaultValue(true)]
	[Description("Whether the thumb slides to its new position when the state changes while the switch is on screen.")]
	public bool AnimateToggle {
		get => field;
		set {
			field = value;
			if (!value)
				FinishAnimation();
		}
	} = true;

	/// <summary>Always <see cref="System.Windows.Forms.Appearance.Normal"/>; the switch draws its own track and thumb.</summary>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new Appearance Appearance {
		get => base.Appearance;
		set {
		}
	}

	/// <summary>Always <see cref="System.Windows.Forms.FlatStyle.Standard"/>; other styles would hand the drawing back to the native check box.</summary>
	[Browsable(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public new FlatStyle FlatStyle {
		get => base.FlatStyle;
		set {
		}
	}

	/// <summary>The default track size in logical pixels, which form auto-scaling scales like other controls' default sizes.</summary>
	protected override Size DefaultSize => new(LogicalTrackWidth, LogicalTrackHeight);

	// Thumb positions run from 0 (off) to 1 (on); an indeterminate switch rests in the middle.
	private float TargetThumbPosition => CheckState switch {
		CheckState.Checked => 1f,
		CheckState.Indeterminate => 0.5f,
		_ => 0f
	};

	// The thumb slides only while the switch is on screen; otherwise it moves at once.
	private bool CanAnimate => AnimateToggle && IsHandleCreated && Visible && TopLevelControl != null && !DesignMode;

	// The caption follows the check box's mnemonic and reading-order settings.
	private TextFormatFlags CaptionFormat => TextFormatFlags.SingleLine | TextFormatFlags.NoPadding
		| (!UseMnemonic ? TextFormatFlags.NoPrefix : ShowKeyboardCues ? TextFormatFlags.Default : TextFormatFlags.HidePrefix)
		| (RightToLeft == RightToLeft.Yes ? TextFormatFlags.RightToLeft : TextFormatFlags.Default);

	/// <summary>Returns the track, widened for its texts, followed by the caption, plus the padding.</summary>
	public override Size GetPreferredSize(Size proposedSize) {
		var track = GetTrackSize();
		var preferred = track;
		if (!string.IsNullOrEmpty(Text)) {
			var caption = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), CaptionFormat);
			preferred = new Size(track.Width + ScaleLogical(LogicalCaptionSpacing) + caption.Width, Math.Max(track.Height, caption.Height));
		}
		preferred += Padding.Size;

		// The size constraints bound the preferred size as they bound the control.
		if (MaximumSize.Width > 0)
			preferred.Width = Math.Min(preferred.Width, MaximumSize.Width);
		if (MaximumSize.Height > 0)
			preferred.Height = Math.Min(preferred.Height, MaximumSize.Height);
		return new Size(Math.Max(preferred.Width, MinimumSize.Width), Math.Max(preferred.Height, MinimumSize.Height));
	}

	/// <summary>Paints the background, the track with its text and thumb, the caption and the focus cue, then raises <see cref="Control.Paint"/>.</summary>
	protected override void OnPaint(PaintEventArgs e) {
		// ButtonBase is opaque, so the background (a transparent back color shows the parent) is painted here, before the switch.
		OnPaintBackground(e);
		var layout = GetSwitchLayout();
		PaintTrack(e.Graphics, layout.Track);
		if (!layout.Caption.IsEmpty)
			TextRenderer.DrawText(e.Graphics, Text, Font, layout.Caption, Enabled ? ForeColor : SystemColors.GrayText, CaptionFormat | TextFormatFlags.EndEllipsis);
		if (Focused && ShowFocusCues)
			ControlPaint.DrawFocusRectangle(e.Graphics, layout.Caption.IsEmpty ? layout.Track : Rectangle.Inflate(layout.Caption, 1, 1));

		// Raise Paint for observers without letting ButtonBase draw the native check glyph over the switch.
		SetStyle(ControlStyles.UserPaint, false);
		using (Tools.Scope.ExecuteOnDispose(static toggle => toggle.SetStyle(ControlStyles.UserPaint, true), this))
			base.OnPaint(e);
	}

	/// <summary>Moves the thumb towards the new state, sliding it while the switch is on screen, then raises <see cref="CheckBox.CheckStateChanged"/>.</summary>
	protected override void OnCheckStateChanged(EventArgs e) {
		if (CanAnimate)
			StartAnimation();
		else
			FinishAnimation();
		base.OnCheckStateChanged(e);
	}

	/// <summary>Completes a running slide when the switch leaves the screen.</summary>
	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		if (_animationTimer is { Enabled: true } && !CanAnimate)
			FinishAnimation();
	}

	/// <summary>Rescales the logical metrics when the switch moves to a display with another DPI.</summary>
	protected override void RescaleConstantsForDpi(int deviceDpiOld, int deviceDpiNew) {
		base.RescaleConstantsForDpi(deviceDpiOld, deviceDpiNew);
		_deviceDpi = deviceDpiNew;
		RefreshTrackMetrics();
	}

	/// <summary>Disposes the owned animation timer.</summary>
	protected override void Dispose(bool disposing) {
		if (disposing && _animationTimer != null) {
			_animationTimer.Tick -= AnimationTimerTick;
			_animationTimer.Dispose();
			_animationTimer = null;
		}
		base.Dispose(disposing);
	}

	// The track sits at the leading edge and the caption follows it, both centred vertically in the padded client area.
	private SwitchLayout GetSwitchLayout() {
		var content = new Rectangle(Padding.Left, Padding.Top, Math.Max(0, ClientSize.Width - Padding.Horizontal), Math.Max(0, ClientSize.Height - Padding.Vertical));
		var hasCaption = !string.IsNullOrEmpty(Text);
		var spacing = ScaleLogical(LogicalCaptionSpacing);
		var measured = hasCaption ? TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), CaptionFormat) : Size.Empty;
		var trackSize = GetFittedTrackSize(content.Size, hasCaption ? spacing + measured.Width : 0);
		var rightToLeft = RightToLeft == RightToLeft.Yes;
		var trackLeft = rightToLeft ? content.Right - trackSize.Width : content.Left;
		var track = new Rectangle(trackLeft, content.Top + (content.Height - trackSize.Height) / 2, trackSize.Width, trackSize.Height);
		if (!hasCaption)
			return new SwitchLayout(track, Rectangle.Empty);

		// The caption keeps its measured height and is cut with an ellipsis when the control is narrower than its preferred size.
		var available = rightToLeft
			? Rectangle.FromLTRB(content.Left, content.Top, track.Left - spacing, content.Bottom)
			: Rectangle.FromLTRB(track.Right + spacing, content.Top, content.Right, content.Bottom);
		var width = Math.Max(0, Math.Min(measured.Width, available.Width));
		var caption = new Rectangle(rightToLeft ? available.Right - width : available.Left, content.Top + (content.Height - measured.Height) / 2, width, measured.Height);
		return new SwitchLayout(track, width > 0 ? caption : Rectangle.Empty);
	}

	// The preferred track is the default track at the control's DPI, widened until the longer track text fits beside the thumb.
	private Size GetTrackSize() {
		var height = ScaleLogical(LogicalTrackHeight);
		return new Size(Math.Max(ScaleLogical(LogicalTrackWidth), GetTextFittedTrackWidth(height)), height);
	}

	// A switch that is not auto-sized and is taller than its preferred track grows the track to the content height, keeping the track's
	// proportions or the width its texts need, but only as tall as still fits beside the caption; it never shrinks below the preferred track.
	private Size GetFittedTrackSize(Size content, int captionExtent) {
		var preferred = GetTrackSize();
		if (AutoSize || content.Height <= preferred.Height)
			return preferred;

		// Both width terms grow linearly with the height, so the tallest track that fits follows from the available width.
		var availableWidth = content.Width - captionExtent;
		var height = Math.Min(content.Height, availableWidth * LogicalTrackHeight / LogicalTrackWidth);
		var textWidth = GetTrackTextWidth();
		if (textWidth > 0)
			height = Math.Min(height, availableWidth + ScaleLogical(LogicalThumbInset) - 2 * ScaleLogical(LogicalTrackTextPadding) - textWidth);
		if (height <= preferred.Height)
			return preferred;
		var width = Math.Max(height * LogicalTrackWidth / LogicalTrackHeight, GetTextFittedTrackWidth(height));
		return new Size(Math.Max(preferred.Width, width), height);
	}

	// A track of this height fits the longer track text between the thumb and the far end of the track, or needs no width for it.
	private int GetTextFittedTrackWidth(int height) {
		var textWidth = GetTrackTextWidth();
		var inset = ScaleLogical(LogicalThumbInset);
		return textWidth > 0 ? inset + (height - 2 * inset) + 2 * ScaleLogical(LogicalTrackTextPadding) + textWidth : 0;
	}

	private int GetTrackTextWidth() => Math.Max(MeasureTrackText(OnText).Width, MeasureTrackText(OffText).Width);

	private Size MeasureTrackText(string text)
		=> string.IsNullOrEmpty(text) ? Size.Empty : TextRenderer.MeasureText(text, Font, new Size(int.MaxValue, int.MaxValue), TrackTextFormat);

	// Paints the pill track, the state text on the side away from the thumb and the circular thumb.
	private void PaintTrack(Graphics graphics, Rectangle track) {
		var state = graphics.Save();
		using var restore = Tools.Scope.ExecuteOnDispose(() => graphics.Restore(state));
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

		// The track color follows the thumb, so a sliding switch cross-fades between the off and on colors.
		var enabled = Enabled;
		var position = _thumbPosition;
		var trackColor = Blend(OffBackColor, OnBackColor, position);
		using (var trackPath = CreatePillPath(track))
		using (var trackBrush = new SolidBrush(enabled ? trackColor : Mute(trackColor)))
			graphics.FillPath(trackBrush, trackPath);

		// Right-to-left switches mirror the thumb's travel, so the on state rests at the leading edge.
		var inset = ScaleLogical(LogicalThumbInset);
		var padding = ScaleLogical(LogicalTrackTextPadding);
		var diameter = Math.Max(1, track.Height - 2 * inset);
		var travel = Math.Max(0, track.Width - 2 * inset - diameter);
		var offset = RightToLeft == RightToLeft.Yes ? 1 - position : position;
		var stateText = position > 0.5f ? OnText : position < 0.5f ? OffText : string.Empty;
		if (!string.IsNullOrEmpty(stateText)) {
			var textBounds = offset > 0.5f
				? Rectangle.FromLTRB(track.Left + padding, track.Top, track.Right - inset - diameter - padding, track.Bottom)
				: Rectangle.FromLTRB(track.Left + inset + diameter + padding, track.Top, track.Right - padding, track.Bottom);
			var textFormat = TrackTextFormat | TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
			TextRenderer.DrawText(graphics, stateText, Font, textBounds, enabled ? TrackForeColor : Mute(TrackForeColor), textFormat);
		}
		using var thumbBrush = new SolidBrush(enabled ? ThumbColor : Mute(ThumbColor));
		graphics.FillEllipse(thumbBrush, track.Left + inset + travel * offset, track.Top + inset, diameter, diameter);
	}

	private void StartAnimation() {
		_animationStartPosition = _thumbPosition;
		_animationStartTimestamp = Stopwatch.GetTimestamp();
		if (_animationTimer == null) {
			_animationTimer = new Timer { Interval = AnimationFrameMilliseconds };
			_animationTimer.Tick += AnimationTimerTick;
		}
		_animationTimer.Start();
	}

	private void FinishAnimation() {
		_animationTimer?.Stop();
		_thumbPosition = TargetThumbPosition;
		Invalidate();
	}

	// Each frame eases the thumb out towards the current state; a switch that leaves the screen finishes at once.
	private void AnimationTimerTick(object sender, EventArgs e) {
		var progress = Math.Min(1.0, Stopwatch.GetElapsedTime(_animationStartTimestamp).TotalMilliseconds / AnimationDurationMilliseconds);
		if (progress >= 1 || !CanAnimate) {
			FinishAnimation();
			return;
		}
		var eased = 1 - Math.Pow(1 - progress, 3);
		_thumbPosition = (float)(_animationStartPosition + (TargetThumbPosition - _animationStartPosition) * eased);
		Invalidate();
	}

	// Track texts and DPI change the preferred size; an auto-sized switch asks its parent for a new layout.
	private void RefreshTrackMetrics() {
		if (AutoSize)
			Parent?.PerformLayout(this, nameof(PreferredSize));
		Invalidate();
	}

	private int ScaleLogical(int logical) => (int)Math.Round(logical * (double)_deviceDpi / LogicalDpi);

	// A pill is two half circles joined by straight edges.
	private static GraphicsPath CreatePillPath(RectangleF bounds) {
		var path = new GraphicsPath();
		var diameter = Math.Min(bounds.Width, bounds.Height);
		if (diameter <= 0)
			return path;
		path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 90, 180);
		path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 180);
		path.CloseFigure();
		return path;
	}

	// The end colors are returned exactly, so a resting switch draws its configured colors.
	private static Color Blend(Color from, Color to, float amount) {
		if (amount <= 0)
			return from;
		if (amount >= 1)
			return to;
		return Color.FromArgb(
			(int)Math.Round(from.A + (to.A - from.A) * amount),
			(int)Math.Round(from.R + (to.R - from.R) * amount),
			(int)Math.Round(from.G + (to.G - from.G) * amount),
			(int)Math.Round(from.B + (to.B - from.B) * amount)
		);
	}

	// A disabled switch keeps its layout but loses its saturation and contrast.
	private static Color Mute(Color color) {
		var luminance = (int)Math.Round(color.R * 0.299 + color.G * 0.587 + color.B * 0.114);
		return Blend(Color.FromArgb(color.A, luminance, luminance, luminance), SystemColors.Control, 0.45f);
	}

	// Designer serialization: only colors differing from their defaults are persisted.
	private bool ShouldSerializeOnBackColor() => OnBackColor != SystemColors.Highlight;

	private void ResetOnBackColor() => OnBackColor = SystemColors.Highlight;

	private bool ShouldSerializeOffBackColor() => OffBackColor != SystemColors.ControlDark;

	private void ResetOffBackColor() => OffBackColor = SystemColors.ControlDark;

	private bool ShouldSerializeThumbColor() => ThumbColor != SystemColors.Window;

	private void ResetThumbColor() => ThumbColor = SystemColors.Window;

	private bool ShouldSerializeTrackForeColor() => TrackForeColor != Color.White;

	private void ResetTrackForeColor() => TrackForeColor = Color.White;
}
