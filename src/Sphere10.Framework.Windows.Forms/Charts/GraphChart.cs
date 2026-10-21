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
using System.Globalization;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A reusable DataVisualization surface for captured numeric line, area and bar series.</summary>
public partial class GraphChart : UserControl {
	private static readonly Color[] DefaultColors = { Color.SteelBlue, Color.Firebrick, Color.SeaGreen, Color.DarkOrange, Color.MediumPurple, Color.Teal };
	private GraphChartSeries[] _series = Array.Empty<GraphChartSeries>();
	private GraphChartStyle _style;
	private double? _minimumX;
	private double? _maximumX;
	private double? _minimumY;
	private double? _maximumY;
	private bool _showAxes = true;
	private bool _showToolTips = true;
	private bool _hasRendered;
	private Func<double, string> _xLabelFormatter;
	private Func<double, string> _yLabelFormatter;

	public GraphChart() {
		InitializeComponent();
	}

	[DefaultValue(GraphChartStyle.Line)]
	public GraphChartStyle Style {
		get => _style;
		set {
			Guard.Argument(Enum.IsDefined(value), nameof(value), "Unknown chart style");
			if (_style == value)
				return;
			_style = value;
			PublishSeries();
		}
	}

	[DefaultValue(null)]
	public double? MinimumX { get => _minimumX; set { ValidateBounds(value, _maximumX); _minimumX = value; UpdateAxes(); } }

	[DefaultValue(null)]
	public double? MaximumX { get => _maximumX; set { ValidateBounds(_minimumX, value); _maximumX = value; UpdateAxes(); } }

	[DefaultValue(null)]
	public double? MinimumY { get => _minimumY; set { ValidateBounds(value, _maximumY); _minimumY = value; UpdateAxes(); } }

	[DefaultValue(null)]
	public double? MaximumY { get => _maximumY; set { ValidateBounds(_minimumY, value); _maximumY = value; UpdateAxes(); } }

	[DefaultValue("")]
	public string XAxisTitle { get => PlotArea.AxisX.Title; set => PlotArea.AxisX.Title = value ?? string.Empty; }

	[DefaultValue("")]
	public string YAxisTitle { get => PlotArea.AxisY.Title; set => PlotArea.AxisY.Title = value ?? string.Empty; }

	[DefaultValue("")]
	public string XValueFormat { get => PlotArea.AxisX.LabelStyle.Format; set => PlotArea.AxisX.LabelStyle.Format = value ?? string.Empty; }

	[DefaultValue("")]
	public string YValueFormat { get => PlotArea.AxisY.LabelStyle.Format; set => PlotArea.AxisY.LabelStyle.Format = value ?? string.Empty; }

	[DefaultValue(false)]
	public bool ShowLegend { get => _chart.Legends[0].Enabled; set => _chart.Legends[0].Enabled = value; }

	[DefaultValue(true)]
	public bool ShowAxes {
		get => _showAxes;
		set {
			_showAxes = value;
			PlotArea.AxisX.LabelStyle.Enabled = PlotArea.AxisY.LabelStyle.Enabled = value;
			PlotArea.AxisX.MajorTickMark.Enabled = PlotArea.AxisY.MajorTickMark.Enabled = value;
			PlotArea.AxisX.LineWidth = PlotArea.AxisY.LineWidth = value ? 1 : 0;
			PlotArea.Position = value ? new ElementPosition() : new ElementPosition(0, 0, 100, 100);
			PlotArea.InnerPlotPosition = value ? new ElementPosition() : new ElementPosition(0, 0, 100, 100);
			_chart.Invalidate();
		}
	}

	[DefaultValue(false)]
	public bool ShowVerticalGrid { get => PlotArea.AxisX.MajorGrid.Enabled; set => PlotArea.AxisX.MajorGrid.Enabled = value; }

	[DefaultValue(typeof(Color), "ControlDark")]
	public Color GridColor {
		get => PlotArea.AxisY.MajorGrid.LineColor;
		set { PlotArea.AxisX.MajorGrid.LineColor = PlotArea.AxisY.MajorGrid.LineColor = value; }
	}

	[DefaultValue(typeof(Color), "Control")]
	public Color PlotBackColor {
		get => PlotArea.BackColor;
		set { PlotArea.BackColor = _chart.BackColor = _emptyState.BackColor = value; }
	}

	[DefaultValue("NO DATA"), Localizable(true)]
	public string NoDataText {
		get => _emptyState.Text;
		set { _emptyState.Text = value ?? string.Empty; UpdateEmptyState(); }
	}

	[DefaultValue(true)]
	public bool ShowToolTips { get => _showToolTips; set { _showToolTips = value; PublishSeries(); } }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<double, string> XLabelFormatter { get => _xLabelFormatter; set { _xLabelFormatter = value; _chart.Invalidate(); } }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Func<double, string> YLabelFormatter { get => _yLabelFormatter; set { _yLabelFormatter = value; _chart.Invalidate(); } }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public GraphChartSeries[] Series => (GraphChartSeries[])_series.Clone();

	/// <summary>The owned chart engine, for advanced chart-area, legend and rendering configuration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Chart NativeChart => _chart;

	/// <summary>The actual plot rectangle, in this control's coordinates, after chart layout.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public RectangleF PlotBounds {
		get {
			var outer = PlotArea.Position;
			var inner = PlotArea.InnerPlotPosition;
			if (!_hasRendered)
				return new RectangleF(_chart.Left, _chart.Top, _chart.Width, _chart.Height);
			return new RectangleF(_chart.Left + (outer.X + inner.X * outer.Width / 100) * _chart.Width / 100,
				_chart.Top + (outer.Y + inner.Y * outer.Height / 100) * _chart.Height / 100,
				outer.Width * inner.Width * _chart.Width / 10000, outer.Height * inner.Height * _chart.Height / 10000);
		}
	}

	private ChartArea PlotArea => _chart.ChartAreas[0];

	public void SetSeries(GraphChartSeries[] series) {
		Guard.ArgumentNotNull(series, nameof(series));
		var names = new HashSet<string>(StringComparer.Ordinal);
		foreach (var item in series) {
			Guard.ArgumentNotNull(item, nameof(series));
			Guard.Argument(names.Add(item.Name), nameof(series), "Series names must be unique");
		}
		_series = (GraphChartSeries[])series.Clone();
		PublishSeries();
	}

	public void Clear() => SetSeries(Array.Empty<GraphChartSeries>());

	public bool TryGetPointAt(Point location, out GraphChartSeries series, out GraphChartPoint point) {
		series = null;
		point = default;
		var bounds = PlotBounds;
		if (!bounds.Contains(location) || bounds.Width <= 0)
			return false;
		var minimum = _minimumX ?? FindXBound(false);
		var maximum = _maximumX ?? FindXBound(true);
		var horizontalBars = Style == GraphChartStyle.Bar || _series.Length > 0 && _series[0].Style == GraphChartStyle.Bar;
		var fraction = horizontalBars ? (bounds.Bottom - location.Y) / bounds.Height : (location.X - bounds.Left) / bounds.Width;
		var x = minimum + fraction * (maximum - minimum);
		if (_hasRendered)
			x = PlotArea.AxisX.PixelPositionToValue(horizontalBars ? location.Y - _chart.Top : location.X - _chart.Left);
		var distance = double.MaxValue;
		foreach (var candidate in _series)
			foreach (var sample in candidate.GetPoints()) {
				var candidateDistance = Math.Abs(sample.X - x);
				if (candidateDistance >= distance)
					continue;
				distance = candidateDistance;
				point = sample;
				series = candidate;
			}
		return series != null;
	}

	protected override void OnFontChanged(EventArgs args) {
		base.OnFontChanged(args);
		if (_chart == null)
			return;
		PlotArea.AxisX.LabelStyle.Font = PlotArea.AxisY.LabelStyle.Font = Font;
		_chart.Legends[0].Font = Font;
	}

	protected override void OnForeColorChanged(EventArgs args) {
		base.OnForeColorChanged(args);
		if (_chart == null)
			return;
		PlotArea.AxisX.LabelStyle.ForeColor = PlotArea.AxisY.LabelStyle.ForeColor = ForeColor;
		PlotArea.AxisX.TitleForeColor = PlotArea.AxisY.TitleForeColor = _chart.Legends[0].ForeColor = ForeColor;
	}

	private static void ValidateBounds(double? minimum, double? maximum) {
		Guard.Argument(!minimum.HasValue || double.IsFinite(minimum.Value), nameof(minimum), "Axis bounds must be finite");
		Guard.Argument(!maximum.HasValue || double.IsFinite(maximum.Value), nameof(maximum), "Axis bounds must be finite");
		Guard.Argument(!minimum.HasValue || !maximum.HasValue || minimum.Value < maximum.Value, nameof(maximum), "Maximum must exceed minimum");
	}

	private void UpdateAxes() {
		PlotArea.AxisX.Minimum = _minimumX ?? double.NaN;
		PlotArea.AxisX.Maximum = _maximumX ?? double.NaN;
		PlotArea.AxisY.Minimum = _minimumY ?? double.NaN;
		PlotArea.AxisY.Maximum = _maximumY ?? double.NaN;
		_chart.Invalidate();
	}

	private double FindXBound(bool maximum) {
		var result = maximum ? double.NegativeInfinity : double.PositiveInfinity;
		foreach (var series in _series)
			foreach (var point in series.GetPoints())
				result = maximum ? Math.Max(result, point.X) : Math.Min(result, point.X);
		return double.IsFinite(result) ? result : maximum ? 1 : 0;
	}

	private void PublishSeries() {
		// Keep the control and native series alive between snapshots; collection batching only defers invalidation.
		_chart.Series.SuspendUpdates();
		using var update = Tools.Scope.ExecuteOnDispose(() => { _chart.Series.ResumeUpdates(); _chart.Invalidate(); });
		var nativeIndex = 0;
		PlotArea.AxisX.IsMarginVisible = Style is GraphChartStyle.Column or GraphChartStyle.Bar;
		for (var logicalIndex = 0; logicalIndex < _series.Length; logicalIndex++) {
			var item = _series[logicalIndex];
			var points = item.GetPoints();
			var style = item.Style ?? Style;
			var bars = style is GraphChartStyle.Column or GraphChartStyle.Bar;
			if (bars)
				PlotArea.AxisX.IsMarginVisible = true;
			var segmentStart = 0;
			var firstSegment = true;
			while (segmentStart < points.Length) {
				while (!bars && segmentStart < points.Length && !points[segmentStart].Y.HasValue)
					segmentStart++;
				if (segmentStart == points.Length)
					break;
				var segmentEnd = segmentStart + 1;
				while (segmentEnd < points.Length && (bars || points[segmentEnd].Y.HasValue))
					segmentEnd++;
				var native = GetNativeSeries(nativeIndex++);
				native.ChartType = style switch { GraphChartStyle.Area => SeriesChartType.Area, GraphChartStyle.Column => SeriesChartType.Column,
					GraphChartStyle.Bar => SeriesChartType.Bar, _ => SeriesChartType.Line };
				native.Color = item.Color.IsEmpty ? DefaultColors[logicalIndex % DefaultColors.Length] : item.Color;
				native.BorderColor = native.Color;
				native.BorderWidth = 2;
				native.MarkerStyle = segmentEnd - segmentStart == 1 && !bars ? MarkerStyle.Circle : MarkerStyle.None;
				native.MarkerSize = 4;
				native.LegendText = item.Name;
				native.IsVisibleInLegend = firstSegment;
				native.Points.SuspendUpdates();
				using (Tools.Scope.ExecuteOnDispose(native.Points.ResumeUpdates)) {
					native.Points.Clear();
					for (var index = segmentStart; index < segmentEnd; index++) {
						var point = points[index];
						var dataPoint = new DataPoint(point.X, point.Y ?? 0) { AxisLabel = point.Label ?? string.Empty, IsEmpty = !point.Y.HasValue };
						if (ShowToolTips && point.Y.HasValue)
							dataPoint.ToolTip = $"{item.Name}: {point.Y.Value.ToString(YValueFormat, CultureInfo.CurrentCulture)}"
								+ Environment.NewLine + (point.Label ?? point.X.ToString(XValueFormat, CultureInfo.CurrentCulture));
						native.Points.Add(dataPoint);
					}
				}
				firstSegment = false;
				segmentStart = segmentEnd;
			}
		}
		while (_chart.Series.Count > nativeIndex)
			_chart.Series.RemoveAt(_chart.Series.Count - 1);
		UpdateAxes();
		UpdateEmptyState();
	}

	private void UpdateEmptyState() {
		// Missing observations are not measurements. A finite zero remains valid captured data.
		var hasData = false;
		foreach (var series in _series)
			foreach (var point in series.GetPoints())
				if (point.Y.HasValue) {
					hasData = true;
					break;
				}
		var showEmptyState = !hasData && !string.IsNullOrWhiteSpace(NoDataText);
		_chart.Visible = !showEmptyState;
		_emptyState.Visible = showEmptyState;
		if (!hasData) {
			_hasRendered = false;
			_emptyState.BringToFront();
		}
	}

	private Series GetNativeSeries(int index) {
		if (index < _chart.Series.Count)
			return _chart.Series[index];
		var series = new Series("Series" + index) { ChartArea = "Plot", Legend = "Series", XValueType = ChartValueType.Double, YValueType = ChartValueType.Double };
		_chart.Series.Add(series);
		return series;
	}

	private void ChartCustomize(object sender, EventArgs args) {
		FormatAxis(PlotArea.AxisX, _xLabelFormatter);
		FormatAxis(PlotArea.AxisY, _yLabelFormatter);
	}

	private void ChartPostPaint(object sender, ChartPaintEventArgs args) => _hasRendered = true;

	private static void FormatAxis(Axis axis, Func<double, string> formatter) {
		if (formatter == null)
			return;
		foreach (var label in axis.CustomLabels)
			label.Text = formatter((label.FromPosition + label.ToPosition) / 2);
	}

	private void ChartMouseMove(object sender, MouseEventArgs args) => OnMouseMove(new MouseEventArgs(args.Button, args.Clicks, args.X + _chart.Left, args.Y + _chart.Top, args.Delta));

	private void ChartMouseLeave(object sender, EventArgs args) => OnMouseLeave(args);
}
