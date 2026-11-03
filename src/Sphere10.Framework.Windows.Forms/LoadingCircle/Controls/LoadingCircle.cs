// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A lightweight, DPI-aware activity indicator with configurable drawing styles and colour.</summary>
public partial class LoadingCircle : Control {
	private readonly Timer _timer;
	private List<Control> _disabledControls;
	private Color _color = Color.DarkGray;
	private int _numberOfSpoke = 10;
	private int _spokeThickness = 4;
	private int _innerCircleRadius = 8;
	private int _outerCircleRadius = 10;
	private int _frame;
	private bool _active;

	/// <summary>Creates an inactive indicator without creating a native window or starting a timer.</summary>
	public LoadingCircle() {
		_timer = new Timer { Interval = 100 };
		_timer.Tick += HandleTimerTick;
		SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
			| ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
		InitializeComponent();
	}

	/// <summary>The foreground colour, including its alpha, used by every animation style.</summary>
	[Category("LoadingCircle")]
	[Description("The foreground colour of the indicator.")]
	[DefaultValue(typeof(Color), "DarkGray")]
	public Color Color {
		get {
			return _color;
		}
		set {
			_color = value;
			Invalidate();
		}
	}

	/// <summary>The animation's visual form; the existing presets continue to configure its geometry.</summary>
	[Category("LoadingCircle")]
	[Description("The visual form of the indicator.")]
	[DefaultValue(LoadingCircleAnimationStyle.Spokes)]
	public LoadingCircleAnimationStyle AnimationStyle {
		get {
			return field;
		}
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Unknown loading animation style.");
			field = value;
			Invalidate();
		}
	}

	/// <summary>The outer radius in logical pixels; drawing scales down when the control is smaller.</summary>
	[Category("LoadingCircle")]
	[Description("The outer radius in logical pixels; drawing fits the available control size.")]
	[DefaultValue(10)]
	public int OuterCircleRadius {
		get {
			return _outerCircleRadius;
		}
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_outerCircleRadius = value;
			Invalidate();
		}
	}

	/// <summary>The inner radius in logical pixels; zero allows spokes to start at the centre.</summary>
	[Category("LoadingCircle")]
	[Description("The inner radius in logical pixels; zero lets spokes start at the centre.")]
	[DefaultValue(8)]
	public int InnerCircleRadius {
		get {
			return _innerCircleRadius;
		}
		set {
			Guard.ArgumentGTE(value, 0, nameof(value));
			_innerCircleRadius = value;
			Invalidate();
		}
	}

	/// <summary>The number of spokes or dots, and the number of steps in one animation cycle.</summary>
	[Category("LoadingCircle")]
	[Description("The number of radial marks and steps per animation cycle.")]
	[DefaultValue(10)]
	public int NumberSpoke {
		get {
			return _numberOfSpoke;
		}
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_numberOfSpoke = value;
			_frame %= value;
			Invalidate();
		}
	}

	/// <summary>Requests animation; hidden, disabled and uncreated native controls pause automatically.</summary>
	[Category("LoadingCircle")]
	[Description("Requests animation while the indicator is visible, enabled and has a native window.")]
	[DefaultValue(false)]
	public bool Active {
		get {
			return _active;
		}
		set {
			if (value)
				ObjectDisposedException.ThrowIf(IsDisposed, this);
			_active = value;
			if (!value)
				_frame = 0;
			UpdateTimer();
			if (!IsDisposed)
				Invalidate();
		}
	}

	/// <summary>The stroke width or dot diameter in logical pixels.</summary>
	[Category("LoadingCircle")]
	[Description("The stroke width or dot diameter in logical pixels.")]
	[DefaultValue(4)]
	public int SpokeThickness {
		get {
			return _spokeThickness;
		}
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_spokeThickness = value;
			Invalidate();
		}
	}

	/// <summary>The interval between frames in milliseconds; larger values animate more slowly.</summary>
	[Category("LoadingCircle")]
	[Description("Milliseconds between frames; larger values animate more slowly.")]
	[DefaultValue(100)]
	public int RotationSpeed {
		get {
			return _timer.Interval;
		}
		set {
			if (value > 0)
				_timer.Interval = value;
		}
	}

	/// <summary>Applies the original geometry presets without restricting the animation style or colour.</summary>
	[Category("LoadingCircle")]
	[Description("Applies a predefined circle geometry without changing its colour or animation style.")]
	[DefaultValue(StylePresets.Custom)]
	public StylePresets StylePreset {
		get {
			return field;
		}
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Unknown loading geometry preset.");
			field = value;
			switch (value) {
				case StylePresets.MacOSX:
					SetCircleAppearance(12, 2, 5, 11);
					break;
				case StylePresets.Firefox:
					SetCircleAppearance(9, 4, 6, 7);
					break;
				case StylePresets.IE7:
					SetCircleAppearance(24, 4, 8, 9);
					break;
				case StylePresets.Custom:
					SetCircleAppearance(10, 4, 8, 10);
					break;
			}
		}
	} = StylePresets.Custom;

	/// <summary>An optional borrowed control shown while StartAnimating is active and hidden when it stops.</summary>
	[Category("LoadingCircle")]
	[Description("An optional control shown by StartAnimating and hidden by StopAnimating.")]
	[DefaultValue(null)]
	public Control HideStopControl {
		get; set;
	}

	/// <summary>Shows the indicator and optionally disables other controls until StopAnimating.</summary>
	public void StartAnimating(Control containerToDisable = null, params Control[] exclusionControls) {
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		if (containerToDisable != null) {
			RestoreDisabledControls();
			var preserved = new HashSet<Control>(exclusionControls ?? Array.Empty<Control>()) { this };
			if (HideStopControl != null)
				preserved.Add(HideStopControl);
			DisableChildren(containerToDisable, preserved);
		}

		Visible = true;
		Active = true;
		if (HideStopControl is { IsDisposed: false })
			HideStopControl.Visible = true;
	}

	/// <summary>Stops and hides the indicator, restoring only controls this instance actually disabled.</summary>
	public void StopAnimating() {
		try {
			Active = false;
			if (!IsDisposed)
				Visible = false;
			if (HideStopControl is { IsDisposed: false })
				HideStopControl.Visible = false;
		} finally {
			RestoreDisabledControls();
		}
	}

	/// <summary>Creates an owned overlay whose scope removes and disposes it after the operation.</summary>
	public static ActionScope EnterAnimationScope(Control containerControl, float relativeSize = 0.5f, StylePresets style = StylePresets.MacOSX, bool disableControls = false, params Control[] exclusionControls) {
		Guard.ArgumentNotNull(containerControl, nameof(containerControl));
		Guard.Argument(float.IsFinite(relativeSize) && relativeSize >= 0.01f && relativeSize <= 1f, nameof(relativeSize), "Must be within [0.01, 1.0].");
		ObjectDisposedException.ThrowIf(containerControl.IsDisposed, containerControl);

		var sideLength = Math.Max(1, (int)(Math.Min(containerControl.ClientSize.Width, containerControl.ClientSize.Height) * relativeSize));
		var loadingCircle = new LoadingCircle();
		try {
			loadingCircle.StylePreset = style;
			loadingCircle.Size = new Size(sideLength, sideLength);
			loadingCircle.SetCircleAppearance(loadingCircle.NumberSpoke, Math.Max(1, sideLength / 15), sideLength / 5, Math.Max(1, sideLength / 3));
			loadingCircle.Location = new Point((containerControl.ClientSize.Width - sideLength) / 2, (containerControl.ClientSize.Height - sideLength) / 2);
			containerControl.Controls.Add(loadingCircle);
			loadingCircle.BringToFront();
			loadingCircle.StartAnimating(disableControls ? containerControl : null, exclusionControls);
		} catch {
			// No scope is returned on entry failure, so release its owned indicator here.
			loadingCircle.Dispose();
			throw;
		}

		return Tools.Scope.ExecuteOnDispose(() => {
			try {
				loadingCircle.StopAnimating();
			} finally {
				loadingCircle.Dispose();
			}
		});
	}

	/// <summary>Animates this borrowed indicator for the lifetime of the returned scope.</summary>
	public ActionScope<LoadingCircle> BeginAnimationScope() {
		return BeginAnimationScope(null);
	}

	/// <summary>Animates this indicator and temporarily disables other controls in the supplied container.</summary>
	public ActionScope<LoadingCircle> BeginAnimationScope(Control containerToDisable, params Control[] exclusionControls) {
		StartAnimating(containerToDisable, exclusionControls);
		return Tools.Scope.ExecuteOnDispose<LoadingCircle>(static circle => circle.StopAnimating(), this);
	}

	/// <summary>Sets all four geometry values together after validating them.</summary>
	public void SetCircleAppearance(int numberSpoke, int spokeThickness, int innerCircleRadius, int outerCircleRadius) {
		Guard.ArgumentGT(numberSpoke, 0, nameof(numberSpoke));
		Guard.ArgumentGT(spokeThickness, 0, nameof(spokeThickness));
		Guard.ArgumentGTE(innerCircleRadius, 0, nameof(innerCircleRadius));
		Guard.ArgumentGT(outerCircleRadius, 0, nameof(outerCircleRadius));
		_numberOfSpoke = numberSpoke;
		_spokeThickness = spokeThickness;
		_innerCircleRadius = innerCircleRadius;
		_outerCircleRadius = outerCircleRadius;
		_frame %= numberSpoke;
		Invalidate();
	}

	/// <summary>Reports both dimensions, including the stroke and current DPI, for native layout and tool strips.</summary>
	public override Size GetPreferredSize(Size proposedSize) {
		var diameter = (int)Math.Min(int.MaxValue, Math.Ceiling((2d * Math.Max(_outerCircleRadius, _innerCircleRadius) + _spokeThickness) * DeviceDpi / 96d));
		return new Size(diameter, diameter);
	}

	/// <summary>Draws directly into the native surface; the standard transparent background paints its parent.</summary>
	protected override void OnPaint(PaintEventArgs e) {
		var availableRadius = Math.Min(ClientSize.Width, ClientSize.Height) / 2f - 0.5f;
		if (availableRadius > 0 && _color.A > 0) {
			var scale = Math.Min(DeviceDpi / 96f, availableRadius / (Math.Max(_innerCircleRadius, _outerCircleRadius) + _spokeThickness / 2f));
			var center = new PointF(ClientSize.Width / 2f, ClientSize.Height / 2f);
			var innerRadius = _innerCircleRadius * scale;
			var outerRadius = _outerCircleRadius * scale;
			var thickness = _spokeThickness * scale;
			var state = e.Graphics.Save();
			try {
				e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				switch (AnimationStyle) {
					case LoadingCircleAnimationStyle.Spokes:
						DrawSpokes(e.Graphics, center, innerRadius, outerRadius, thickness);
						break;
					case LoadingCircleAnimationStyle.Dots:
						DrawDots(e.Graphics, center, (innerRadius + outerRadius) / 2, thickness);
						break;
					case LoadingCircleAnimationStyle.Arc:
						DrawArc(e.Graphics, center, outerRadius, thickness);
						break;
					case LoadingCircleAnimationStyle.Pulse:
						DrawPulse(e.Graphics, center, outerRadius, thickness);
						break;
				}
			} finally {
				e.Graphics.Restore(state);
			}
		}
		base.OnPaint(e);
	}

	/// <summary>Starts the requested animation only after a native handle exists.</summary>
	protected override void OnHandleCreated(EventArgs e) {
		base.OnHandleCreated(e);
		UpdateTimer();
	}

	/// <summary>Stops native timer delivery before the handle is destroyed or recreated.</summary>
	protected override void OnHandleDestroyed(EventArgs e) {
		if (!Disposing && !IsDisposed)
			_timer.Stop();
		base.OnHandleDestroyed(e);
	}

	/// <summary>Pauses hidden indicators without changing the caller's requested Active value.</summary>
	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		UpdateTimer();
	}

	/// <summary>Pauses disabled indicators until their effective enabled state returns.</summary>
	protected override void OnEnabledChanged(EventArgs e) {
		base.OnEnabledChanged(e);
		UpdateTimer();
	}

	// Release the timer and restore borrowed controls; hiding the companion still runs after a consumer error.
	private void DisposeAnimation() {
		if (IsDisposed)
			return;
		_active = false;
		_timer.Stop();
		_timer.Tick -= HandleTimerTick;
		_timer.Dispose();
		try {
			RestoreDisabledControls();
		} finally {
			if (HideStopControl is { IsDisposed: false })
				HideStopControl.Visible = false;
		}
	}

	// Native Tick already runs on the control's UI thread; no separate scheduler or dispatcher is needed.
	private void HandleTimerTick(object sender, EventArgs e) {
		_frame = (_frame + 1) % _numberOfSpoke;
		Invalidate();
	}

	// The requested state survives hiding and handle recreation, but neither runs an idle native timer.
	private void UpdateTimer() {
		if (IsDisposed || Disposing)
			return;
		_timer.Enabled = _active && IsHandleCreated && Visible && Enabled && !DesignMode && !Tools.Runtime.IsDesignMode;
	}

	// Preserve disabled branches and the full ancestor path of every excluded control.
	private void DisableChildren(Control parent, HashSet<Control> preserved) {
		foreach (Control child in parent.Controls) {
			if (!child.Enabled || preserved.Contains(child))
				continue;
			if (child is Form or GroupBox or ValidationIndicator || ContainsPreservedControl(child, preserved)) {
				DisableChildren(child, preserved);
				continue;
			}
			(_disabledControls ??= new List<Control>()).Add(child);
			child.Enabled = false;
		}
	}

	// A single list suffices because only enabled controls change; attempt every restoration before rethrowing the first error.
	private void RestoreDisabledControls() {
		var disabledControls = _disabledControls;
		_disabledControls = null;
		if (disabledControls == null)
			return;
		ExceptionDispatchInfo firstError = null;
		foreach (var control in disabledControls) {
			if (control.IsDisposed || control.Disposing)
				continue;
			try {
				control.Enabled = true;
			} catch (Exception error) {
				firstError ??= ExceptionDispatchInfo.Capture(error);
			}
		}
		firstError?.Throw();
	}

	// Skip an ancestor only when disabling it would also disable an explicitly preserved descendant.
	private static bool ContainsPreservedControl(Control parent, HashSet<Control> preserved) {
		foreach (var control in preserved) {
			if (control != null && parent.Contains(control))
				return true;
		}
		return false;
	}

	// A single scoped pen draws the rotating spokes without retaining a palette or angle table.
	private void DrawSpokes(Graphics graphics, PointF center, float innerRadius, float outerRadius, float thickness) {
		using var pen = new Pen(_color, thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
		for (var index = 0; index < _numberOfSpoke; index++) {
			var angle = (_frame - index) * 2d * Math.PI / _numberOfSpoke - Math.PI / 2;
			pen.Color = FadeColor(_active ? 1f - (float)index / _numberOfSpoke : 1f);
			graphics.DrawLine(pen, Coordinate(center, innerRadius, angle), Coordinate(center, outerRadius, angle));
		}
	}

	// Dot size shares the existing thickness property; the colour's original alpha is retained.
	private void DrawDots(Graphics graphics, PointF center, float radius, float diameter) {
		using var brush = new SolidBrush(_color);
		for (var index = 0; index < _numberOfSpoke; index++) {
			var angle = (_frame - index) * 2d * Math.PI / _numberOfSpoke - Math.PI / 2;
			var point = Coordinate(center, radius, angle);
			brush.Color = FadeColor(_active ? 1f - (float)index / _numberOfSpoke : 1f);
			graphics.FillEllipse(brush, point.X - diameter / 2, point.Y - diameter / 2, diameter, diameter);
		}
	}

	// The faint complete track keeps the arc readable against either light or dark parent backgrounds.
	private void DrawArc(Graphics graphics, PointF center, float radius, float thickness) {
		using var pen = new Pen(FadeColor(0.16f), thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
		var bounds = CircleBounds(center, radius);
		graphics.DrawEllipse(pen, bounds);
		pen.Color = _color;
		graphics.DrawArc(pen, bounds, 360f * _frame / _numberOfSpoke - 90, 250);
	}

	// Two expanding rings use the same bounded frame counter and require no independent timer or state.
	private void DrawPulse(Graphics graphics, PointF center, float radius, float thickness) {
		using var pen = new Pen(_color, thickness);
		for (var ring = 0; ring < 2; ring++) {
			var phase = (_frame / (float)_numberOfSpoke + ring / 2f) % 1f;
			pen.Color = FadeColor(1f - phase);
			graphics.DrawEllipse(pen, CircleBounds(center, radius * (0.2f + 0.8f * phase)));
		}
	}

	// Apply trail opacity without replacing an explicitly translucent foreground colour.
	private Color FadeColor(float opacity) {
		return Color.FromArgb((int)Math.Round(_color.A * opacity), _color.R, _color.G, _color.B);
	}

	// Convert the current geometry directly rather than storing values derived from size or frame.
	private static PointF Coordinate(PointF center, float radius, double angle) {
		return new PointF(center.X + radius * (float)Math.Cos(angle), center.Y + radius * (float)Math.Sin(angle));
	}

	// All circular styles share the same client-centred bounds.
	private static RectangleF CircleBounds(PointF center, float radius) {
		return new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
	}

	#region Inner Types

	/// <summary>The original, stable geometry preset values retained for existing designers and callers.</summary>
	public enum StylePresets {
		/// <summary>Twelve narrow spokes.</summary>
		MacOSX,

		/// <summary>Nine broad spokes.</summary>
		Firefox,

		/// <summary>Twenty-four closely spaced spokes.</summary>
		IE7,

		/// <summary>The default ten-spoke geometry, available for manual adjustment.</summary>
		Custom
	}

	#endregion
}
