// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Designer-editable history chart. The consumer supplies captured samples and owns their retention and sampling cadence.</summary>
public partial class TelemetryChart : UserControl {
	private TelemetryChartPoint[] _points = Array.Empty<TelemetryChartPoint>();
	private PointF[][] _segments = Array.Empty<PointF[]>();
	private PointF[][] _renderedSegments = Array.Empty<PointF[]>();
	private RectangleF _renderedArea;
	private readonly string[] _scaleLabels = new string[5];
	private TelemetryChartUnit _unit;
	private TelemetryChartStyle _plotStyle;
	private Color _lineColor = Color.SteelBlue;
	private Color _fillColor = Color.Empty;
	private Color _gridColor = SystemColors.ControlDark;
	private bool _compact;
	private bool _showHeader = true;
	private bool _showVerticalGrid;
	private int _scaleWidth;
	private int _nowWidth;
	private int _hoveredIndex = -1;
	private double _seconds;
	private double _minimum;
	private double _maximum = 100;

	public TelemetryChart() {
		InitializeComponent();
		PrepareScale();
	}

	[DefaultValue("Metric")]
	public string ChartTitle {
		get => _title.Text;
		set { _title.Text = value; _title.Visible = !_compact && _showHeader && !string.IsNullOrWhiteSpace(value); }
	}

	[DefaultValue(false)]
	public bool Compact {
		get => _compact;
		set {
			_compact = value;
			_title.Visible = !value && _showHeader && !string.IsNullOrWhiteSpace(ChartTitle);
			_value.Visible = !value && _showHeader;
			Padding = value ? Padding.Empty : new Padding(3);
			_plot.Invalidate();
		}
	}

	[DefaultValue(true)]
	public bool ShowHeader {
		get => _showHeader;
		set {
			_showHeader = value;
			_title.Visible = value && !_compact && !string.IsNullOrWhiteSpace(ChartTitle);
			_value.Visible = value && !_compact;
		}
	}

	[DefaultValue(false)]
	public bool Throughput { get => Unit == TelemetryChartUnit.BytesPerSecond; set => Unit = value ? TelemetryChartUnit.BytesPerSecond : TelemetryChartUnit.Percent; }

	[DefaultValue(TelemetryChartUnit.Percent)]
	public TelemetryChartUnit Unit {
		get => _unit;
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Unknown chart unit");
			_unit = value;
			SetSamples(_points);
		}
	}

	[DefaultValue(TelemetryChartStyle.Line)]
	public TelemetryChartStyle PlotStyle {
		get => _plotStyle;
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Unknown chart style");
			_plotStyle = value;
			_plot.Invalidate();
		}
	}

	[DefaultValue(typeof(Color), "SteelBlue")]
	public Color LineColor {
		get => _lineColor;
		set { _lineColor = value; _value.ForeColor = value; _plot.Invalidate(); }
	}

	[DefaultValue(typeof(Color), "Empty")]
	public Color FillColor {
		get => _fillColor;
		set { _fillColor = value; _plot.Invalidate(); }
	}

	[DefaultValue(typeof(Color), "Control")]
	public Color PlotBackColor {
		get => _plot.FromColor;
		set { _plot.FromColor = _plot.ToColor = value; _plot.Invalidate(); }
	}

	[DefaultValue(typeof(Color), "ControlDark")]
	public Color GridColor {
		get => _gridColor;
		set { _gridColor = value; _plot.Invalidate(); }
	}

	[DefaultValue(false)]
	public bool ShowVerticalGrid {
		get => _showVerticalGrid;
		set { _showVerticalGrid = value; _plot.Invalidate(); }
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public TelemetryChartPoint[] Points => (TelemetryChartPoint[])_points.Clone();

	public void SetSamples(TelemetryChartPoint[] points) {
		Guard.ArgumentNotNull(points, nameof(points));
		// Own membership and validate before replacing the displayed snapshot.
		var prepared = new TelemetryChartPoint[points.Length];
		for (var index = 0; index < points.Length; index++) {
			Guard.ArgumentNotNull(points[index], nameof(points));
			var point = points[index];
			prepared[index] = point with { Value = point.Value is { } value && double.IsFinite(value) && (Unit == TelemetryChartUnit.Celsius || value >= 0) ? value : null };
		}
		_points = prepared;
		PrepareScale();
		PrepareSegments();
		_hoveredIndex = -1;
		_sampleToolTip.SetToolTip(_plot, null);
		var latest = prepared.Length == 0 ? null : prepared[^1].Value;
		_value.Text = latest.HasValue ? FormatValue(latest.Value) : "Unavailable";
		_plot.Invalidate();
	}

	/// <summary>Returns the nearest captured reading at a position in this control, including an unavailable reading at a gap.</summary>
	public bool TryGetSampleAt(Point location, out TelemetryChartPoint sample) {
		var plotLocation = new Point(location.X - _plot.Left, location.Y - _plot.Top);
		var area = GetPlotArea();
		if (_points.Length == 0 || !area.Contains(plotLocation)) {
			sample = null;
			return false;
		}
		sample = _points[FindSampleIndex(plotLocation.X, area)];
		return true;
	}

	protected override void OnFontChanged(EventArgs args) {
		base.OnFontChanged(args);
		_scaleWidth = 0;
		_nowWidth = 0;
		_plot?.Invalidate();
	}

	protected override void OnDpiChangedAfterParent(EventArgs args) {
		base.OnDpiChangedAfterParent(args);
		_scaleWidth = 0;
		_nowWidth = 0;
		_plot.Invalidate();
	}

	protected override void Dispose(bool disposing) {
		if (disposing)
			_sampleToolTip?.Dispose();
		base.Dispose(disposing);
	}

	private void PrepareScale() {
		_minimum = 0;
		_maximum = Unit == TelemetryChartUnit.Percent ? 100 : Unit == TelemetryChartUnit.Celsius ? 100 : 1024;
		foreach (var point in _points) {
			if (!point.Value.HasValue)
				continue;
			if (Unit == TelemetryChartUnit.Celsius)
				_minimum = Math.Min(_minimum, Math.Floor(point.Value.Value / 10) * 10);
			if (Unit != TelemetryChartUnit.Percent)
				_maximum = Math.Max(_maximum, point.Value.Value);
		}
		if (Unit == TelemetryChartUnit.Celsius)
			_maximum = Math.Ceiling(_maximum / 10) * 10;
		else if (Unit != TelemetryChartUnit.Percent) {
			var rounded = Math.Pow(2, Math.Ceiling(Math.Log2(_maximum)));
			if (double.IsFinite(rounded))
				_maximum = rounded;
		}
		for (var index = 0; index < _scaleLabels.Length; index++)
			_scaleLabels[index] = FormatValue(_minimum + (_maximum - _minimum) * (4 - index) / 4);
		_scaleWidth = 0;
	}

	private void PrepareSegments() {
		// Normalize once per supplied snapshot. Resizing and repainting never queries or evaluates the source model.
		_seconds = _points.Length < 2 ? 0 : Math.Max(0, (_points[^1].CapturedAtUtc - _points[0].CapturedAtUtc).TotalSeconds);
		var segments = new List<PointF[]>();
		var segment = new List<PointF>();
		foreach (var point in _points) {
			if (!point.Value.HasValue) {
				if (segment.Count > 0) {
					segments.Add(segment.ToArray());
					segment.Clear();
				}
				continue;
			}
			var x = _seconds == 0 ? 1 : Math.Clamp((point.CapturedAtUtc - _points[0].CapturedAtUtc).TotalSeconds / _seconds, 0, 1);
			var y = Math.Clamp((point.Value.Value - _minimum) / (_maximum - _minimum), 0, 1);
			segment.Add(new PointF((float)x, (float)y));
		}
		if (segment.Count > 0)
			segments.Add(segment.ToArray());
		_segments = segments.ToArray();
		_renderedArea = RectangleF.Empty;
	}

	private string FormatValue(double value) => Unit switch {
		TelemetryChartUnit.Bytes => FormatBytes(value),
		TelemetryChartUnit.BytesPerSecond => FormatBytes(value) + "/s",
		TelemetryChartUnit.Celsius => $"{value:N1} °C",
		_ => $"{value:N1}%"
	};

	private static string FormatBytes(double value) {
		// Binary chart scales retain fractions instead of truncating sub-byte rates to zero.
		var suffix = "B";
		foreach (var unit in new[] { "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" }) {
			if (Math.Abs(value) < 1024)
				break;
			value /= 1024;
			suffix = unit;
		}
		return $"{value:N1} {suffix}";
	}

	private RectangleF GetPlotArea() {
		if (Compact)
			return new RectangleF(2, 2, Math.Max(1, _plot.ClientSize.Width - 4), Math.Max(1, _plot.ClientSize.Height - 4));
		if (_scaleWidth == 0) {
			foreach (var label in _scaleLabels)
				_scaleWidth = Math.Max(_scaleWidth, TextRenderer.MeasureText(label, Font).Width + 8);
		}
		var top = Font.Height / 2F + 3;
		return new RectangleF(_scaleWidth, top, Math.Max(1, _plot.ClientSize.Width - _scaleWidth - 9), Math.Max(1, _plot.ClientSize.Height - top - Font.Height - 10));
	}

	private void PrepareGeometry(RectangleF area) {
		if (_renderedArea == area)
			return;
		_renderedArea = area;
		_renderedSegments = new PointF[_segments.Length][];
		for (var segmentIndex = 0; segmentIndex < _segments.Length; segmentIndex++) {
			var segment = _segments[segmentIndex];
			var positions = new PointF[segment.Length];
			for (var index = 0; index < segment.Length; index++)
				positions[index] = new PointF(area.Left + segment[index].X * area.Width, area.Bottom - segment[index].Y * area.Height);
			_renderedSegments[segmentIndex] = positions;
		}
	}

	private int FindSampleIndex(float x, RectangleF area) {
		var seconds = Math.Clamp((x - area.Left) / area.Width, 0, 1) * _seconds;
		var closestIndex = 0;
		var closestDistance = double.MaxValue;
		for (var index = 0; index < _points.Length; index++) {
			var distance = Math.Abs((_points[index].CapturedAtUtc - _points[0].CapturedAtUtc).TotalSeconds - seconds);
			if (distance < closestDistance) {
				closestDistance = distance;
				closestIndex = index;
			}
		}
		return closestIndex;
	}

	private void PlotResize(object sender, EventArgs args) => _plot.Invalidate();

	private void PlotMouseMove(object sender, MouseEventArgs args) {
		var area = GetPlotArea();
		var index = _points.Length > 0 && area.Contains(args.Location) ? FindSampleIndex(args.X, area) : -1;
		if (index == _hoveredIndex)
			return;
		_hoveredIndex = index;
		var sample = index < 0 ? null : _points[index];
		_sampleToolTip.SetToolTip(_plot, sample == null ? null : (sample.Value.HasValue ? FormatValue(sample.Value.Value) : "Unavailable")
			+ Environment.NewLine + sample.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture));
	}

	private void PlotMouseLeave(object sender, EventArgs args) {
		_hoveredIndex = -1;
		_sampleToolTip.SetToolTip(_plot, null);
	}

	private void PlotPaint(object sender, PaintEventArgs args) {
		var graphics = args.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		var area = GetPlotArea();
		PrepareGeometry(area);
		using var gridPen = new Pen(GridColor);
		using var textBrush = new SolidBrush(ForeColor);
		using var linePen = new Pen(LineColor, 2F);
		using var fillBrush = new SolidBrush(FillColor.IsEmpty ? Color.FromArgb(80, LineColor) : FillColor);
		using var rightAligned = new StringFormat { Alignment = StringAlignment.Far };
		for (var line = 0; line <= 4; line++) {
			var y = area.Top + area.Height * line / 4;
			graphics.DrawLine(gridPen, area.Left, y, area.Right, y);
			if (!Compact)
				graphics.DrawString(_scaleLabels[line], Font, textBrush, new RectangleF(0, y - Font.Height / 2F, _scaleWidth - 6, Font.Height + 3), rightAligned);
		}
		if (ShowVerticalGrid)
			for (var line = 0; line <= 12; line++) {
				var x = area.Left + area.Width * line / 12;
				graphics.DrawLine(gridPen, x, area.Top, x, area.Bottom);
			}
		foreach (var positions in _renderedSegments) {
			if (positions.Length == 1) {
				graphics.DrawEllipse(linePen, positions[0].X - 1, positions[0].Y - 1, 2, 2);
				continue;
			}
			if (PlotStyle == TelemetryChartStyle.Area) {
				using var fillPath = new GraphicsPath();
				fillPath.AddLine(positions[0].X, area.Bottom, positions[0].X, positions[0].Y);
				fillPath.AddLines(positions);
				fillPath.AddLine(positions[^1].X, positions[^1].Y, positions[^1].X, area.Bottom);
				fillPath.CloseFigure();
				graphics.FillPath(fillBrush, fillPath);
			}
			graphics.DrawLines(linePen, positions);
		}
		if (!Compact) {
			if (_points.Length < 2)
				graphics.DrawString("Waiting For Successive Readings", Font, textBrush, area.Left + 8, area.Top + 8);
			graphics.DrawString($"{_seconds:N0}s Ago", Font, textBrush, area.Left, area.Bottom + 5);
			if (_nowWidth == 0)
				_nowWidth = TextRenderer.MeasureText("Now", Font).Width;
			graphics.DrawString("Now", Font, textBrush, area.Right - _nowWidth, area.Bottom + 5);
		}
	}
}
