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
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Captured metric history displayed through the reusable DataVisualization graph control.</summary>
public partial class TelemetryChart : UserControl {
	private TelemetryChartPoint[] _points = Array.Empty<TelemetryChartPoint>();
	private TelemetryChartUnit _unit;
	private TelemetryChartStyle _plotStyle;
	private Color _lineColor = Color.SteelBlue;
	private Color _fillColor = Color.Empty;
	private bool _compact;
	private bool _showHeader = true;
	private int _hoveredIndex = -1;
	private string _logSeriesName = string.Empty;

	public TelemetryChart() {
		InitializeComponent();
		_plot.ShowToolTips = false;
		_plot.YLabelFormatter = FormatValue;
		_plot.XLabelFormatter = value => Math.Abs(value) < 0.0001 ? "Now" : $"{-value:N0}s Ago";
		_plot.LogSampleProvider = () => _points.Select(point => new ChartLogSample(ResolvedLogSeriesName,
			point.CapturedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
			string.Empty, new[] { point.Value?.ToString("R", CultureInfo.InvariantCulture) ?? "Unavailable" }) {
			CapturedAtUtc = point.CapturedAtUtc,
			Units = Unit switch { TelemetryChartUnit.Bytes => "B", TelemetryChartUnit.BytesPerSecond => "B/s", TelemetryChartUnit.Celsius => "°C", _ => "%" }
		}).ToArray();
		PublishSamples();
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
			_plot.ShowAxes = !value;
			_plot.Padding = value ? new Padding(2) : Padding.Empty;
		}
	}

	[DefaultValue(true)]
	public bool ShowHeader {
		get => _showHeader;
		set { _showHeader = value; _title.Visible = value && !_compact && !string.IsNullOrWhiteSpace(ChartTitle); _value.Visible = value && !_compact; }
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
		set { Guard.Argument(Enum.IsDefined(value), nameof(value), "Unknown chart style"); _plotStyle = value; PublishSamples(); }
	}

	[DefaultValue(typeof(Color), "SteelBlue")]
	public Color LineColor { get => _lineColor; set { _lineColor = value; _value.ForeColor = value; PublishSamples(); } }

	[DefaultValue(typeof(Color), "Empty")]
	public Color FillColor { get => _fillColor; set { _fillColor = value; PublishSamples(); } }

	[DefaultValue(typeof(Color), "Control")]
	public Color PlotBackColor { get => _plot.PlotBackColor; set => _plot.PlotBackColor = value; }

	[DefaultValue(typeof(Color), "ControlDark")]
	public Color GridColor { get => _plot.GridColor; set => _plot.GridColor = value; }

	[DefaultValue(false)]
	public bool ShowVerticalGrid { get => _plot.ShowVerticalGrid; set => _plot.ShowVerticalGrid = value; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public TelemetryChartPoint[] Points => (TelemetryChartPoint[])_points.Clone();

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public LogOutputSelectorControl LogOutput => _plot.LogOutput;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ChartLogOutputBehavior LogOutputBehavior => _plot.LogOutputBehavior;

	[Category("Behavior"), DefaultValue("")]
	public string LogTitle { get => _plot.LogTitle; set => _plot.LogTitle = value; }

	/// <summary>Optional descriptive measurement name; empty uses the chart heading or surrounding group caption.</summary>
	[Category("Behavior"), DefaultValue("")]
	public string LogSeriesName { get => _logSeriesName; set => _logSeriesName = value ?? string.Empty; }

	private string ResolvedLogSeriesName => !string.IsNullOrWhiteSpace(_logSeriesName) ? _logSeriesName
		: ChartLogOutputBehavior.IsDescriptive(ChartTitle) ? ChartTitle : ChartLogOutputBehavior.ResolveGroupTitle(this) ?? _plot.ResolvedLogTitle;

	public void SetSamples(TelemetryChartPoint[] points) {
		Guard.ArgumentNotNull(points, nameof(points));
		// Normalize captured observations before replacing the currently displayed snapshot.
		var prepared = new TelemetryChartPoint[points.Length];
		for (var index = 0; index < points.Length; index++) {
			Guard.ArgumentNotNull(points[index], nameof(points));
			var point = points[index];
			prepared[index] = point with { Value = point.Value is { } value && double.IsFinite(value) && (Unit == TelemetryChartUnit.Celsius || value >= 0) ? value : null };
		}
		_points = prepared;
		_hoveredIndex = -1;
		_sampleToolTip.SetToolTip(_plot.NativeChart, null);
		var latest = prepared.Length == 0 ? null : prepared[^1].Value;
		_value.Text = latest.HasValue ? FormatValue(latest.Value) : "Unavailable";
		PublishSamples();
	}

	/// <summary>Returns the nearest captured reading, including unavailable observations at a gap.</summary>
	public bool TryGetSampleAt(Point location, out TelemetryChartPoint sample) {
		var plotLocation = new Point(location.X - _plot.Left, location.Y - _plot.Top);
		if (!_plot.TryGetPointAt(plotLocation, out _, out var point)) {
			sample = null;
			return false;
		}
		var latest = _points[^1].CapturedAtUtc;
		var closest = 0;
		var distance = double.MaxValue;
		for (var index = 0; index < _points.Length; index++) {
			var candidate = Math.Abs((_points[index].CapturedAtUtc - latest).TotalSeconds - point.X);
			if (candidate >= distance)
				continue;
			distance = candidate;
			closest = index;
		}
		sample = _points[closest];
		return true;
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			_points = Array.Empty<TelemetryChartPoint>();
			_sampleToolTip?.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnHandleCreated(EventArgs args) {
		base.OnHandleCreated(args);
		if (Tools.Runtime.IsDesignMode)
			return;
		LogOutputBehavior.AttachSurface(this);
		LogOutputBehavior.AttachSurface(_title);
		LogOutputBehavior.AttachSurface(_value);
	}

	private void PublishSamples() {
		if (_plot == null)
			return;
		// Sampling and retention stay with the caller; only captured numeric data enters the renderer.
		var minimum = 0D;
		var maximum = Unit == TelemetryChartUnit.Percent || Unit == TelemetryChartUnit.Celsius ? 100D : 1024D;
		var latest = _points.Length == 0 ? DateTime.MinValue : _points[^1].CapturedAtUtc;
		var values = new GraphChartPoint[_points.Length];
		for (var index = 0; index < _points.Length; index++) {
			var point = _points[index];
			values[index] = new GraphChartPoint((point.CapturedAtUtc - latest).TotalSeconds, point.Value);
			if (!point.Value.HasValue)
				continue;
			if (Unit == TelemetryChartUnit.Celsius)
				minimum = Math.Min(minimum, Math.Floor(point.Value.Value / 10) * 10);
			if (Unit != TelemetryChartUnit.Percent)
				maximum = Math.Max(maximum, point.Value.Value);
		}
		if (Unit == TelemetryChartUnit.Celsius)
			maximum = Math.Ceiling(maximum / 10) * 10;
		else if (Unit != TelemetryChartUnit.Percent) {
			var rounded = Math.Pow(2, Math.Ceiling(Math.Log2(maximum)));
			if (double.IsFinite(rounded))
				maximum = rounded;
		}
		_plot.MinimumY = null;
		_plot.MaximumY = maximum;
		_plot.MinimumY = minimum;
		_plot.MinimumX = null;
		_plot.MaximumX = 0;
		_plot.MinimumX = values.Length < 2 ? -1 : Math.Min(-0.001, values[0].X);
		_plot.Style = PlotStyle == TelemetryChartStyle.Area ? GraphChartStyle.Area : GraphChartStyle.Line;
		_plot.SetSeries(new[] { new GraphChartSeries("Metric", values, LineColor) });
		foreach (var series in _plot.NativeChart.Series) {
			series.BorderColor = LineColor;
			if (PlotStyle == TelemetryChartStyle.Area)
				series.Color = FillColor.IsEmpty ? Color.FromArgb(80, LineColor) : FillColor;
		}
	}

	private string FormatValue(double value) => Unit switch {
		TelemetryChartUnit.Bytes => FormatBytes(value),
		TelemetryChartUnit.BytesPerSecond => FormatBytes(value) + "/s",
		TelemetryChartUnit.Celsius => $"{value:N1} °C",
		_ => $"{value:N1}%"
	};

	private static string FormatBytes(double value) {
		var suffix = "B";
		foreach (var unit in new[] { "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" }) {
			if (Math.Abs(value) < 1024)
				break;
			value /= 1024;
			suffix = unit;
		}
		return $"{value:N1} {suffix}";
	}

	private void PlotMouseMove(object sender, MouseEventArgs args) {
		if (!TryGetSampleAt(new Point(args.X + _plot.Left, args.Y + _plot.Top), out var sample)) {
			PlotMouseLeave(sender, args);
			return;
		}
		var index = Array.IndexOf(_points, sample);
		if (index == _hoveredIndex)
			return;
		_hoveredIndex = index;
		_sampleToolTip.SetToolTip(_plot.NativeChart, (sample.Value.HasValue ? FormatValue(sample.Value.Value) : "Unavailable")
			+ Environment.NewLine + sample.CapturedAtUtc.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture));
	}

	private void PlotMouseLeave(object sender, EventArgs args) {
		_hoveredIndex = -1;
		_sampleToolTip.SetToolTip(_plot.NativeChart, null);
	}
}
