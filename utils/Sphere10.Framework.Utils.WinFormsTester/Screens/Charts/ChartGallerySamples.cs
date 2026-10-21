// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Small, deterministic simulated datasets, constructed only when a gallery example is selected.</summary>
public static class ChartGallerySamples {
	private static readonly Color[] _colors = { Color.FromArgb(47, 112, 193), Color.FromArgb(228, 132, 58), Color.FromArgb(52, 151, 124), Color.FromArgb(152, 105, 189), Color.FromArgb(205, 87, 103) };
	private static readonly IReadOnlyList<ChartGallerySample> _all = Array.AsReadOnly(CreateSamples());

	public static IReadOnlyList<ChartGallerySample> All => _all;

	private static ChartGallerySample[] CreateSamples() => new[] {
		Sample(SeriesChartType.Line, "Trends", "Daily Orders", "Two sales channels over fourteen days, with date labels, markers and one deliberately missing observation.", chart => Orders(chart, SeriesChartType.Line)),
		Sample(SeriesChartType.Spline, "Trends", "Customer Satisfaction", "A smooth comparison of weekly satisfaction scores. Splines interpolate between observations; markers identify the actual samples.", chart => Satisfaction(chart)),
		Sample(SeriesChartType.StepLine, "Trends", "Capacity Changes", "Step lines hold a configured capacity until the next change, rather than implying a gradual increase between changes.", chart => Capacity(chart)),
		Sample(SeriesChartType.FastLine, "Trends", "High Resolution Signal", "240 numeric samples show the fast line renderer, a signed signal and a numeric time axis.", chart => Signal(chart)),
		Sample(SeriesChartType.Area, "Trends", "Daily Energy Demand", "A filled area emphasizes total demand above a zero baseline, while a second outline provides context.", chart => Energy(chart, SeriesChartType.Area)),
		Sample(SeriesChartType.SplineArea, "Trends", "Hourly Site Traffic", "A smooth filled profile of simulated hourly traffic. The translucent fill leaves the grid readable.", chart => Traffic(chart)),
		Sample(SeriesChartType.StackedArea, "Trends", "Workload Capacity", "Three workload areas stack to the total capacity used. Each component keeps its own legend entry.", chart => Workloads(chart, SeriesChartType.StackedArea)),
		Sample(SeriesChartType.StackedArea100, "Trends", "Workload Share", "The same workload observations are normalized to 100 percent to compare composition over time.", chart => Workloads(chart, SeriesChartType.StackedArea100)),
		Sample(SeriesChartType.Column, "Comparisons", "Quarterly Revenue", "Grouped columns compare two years with category labels and a formatted currency axis.", chart => Revenue(chart, SeriesChartType.Column)),
		Sample(SeriesChartType.Bar, "Comparisons", "Regional Net Cash Flow", "Horizontal bars compare signed values. Separate colors distinguish an outflow from positive cash flow.", chart => CashFlow(chart)),
		Sample(SeriesChartType.StackedColumn, "Comparisons", "Orders By Channel", "Monthly channel counts stack into each month's total. Every month has an observation in every series.", chart => Channels(chart, SeriesChartType.StackedColumn)),
		Sample(SeriesChartType.StackedColumn100, "Comparisons", "Channel Mix", "A 100-percent stack exposes changing order mix without letting differences in monthly totals dominate.", chart => Channels(chart, SeriesChartType.StackedColumn100)),
		Sample(SeriesChartType.StackedBar, "Comparisons", "Support Workload", "Horizontal stacked bars compare the priority breakdown for four support teams.", chart => Support(chart, SeriesChartType.StackedBar)),
		Sample(SeriesChartType.StackedBar100, "Comparisons", "Support Priority Mix", "A normalized horizontal comparison emphasizes each team's proportion of urgent, normal and low-priority work.", chart => Support(chart, SeriesChartType.StackedBar100)),
		Sample(SeriesChartType.Pie, "Composition", "Operating Cost Mix", "A part-to-whole chart uses category legends, outside labels and percentage tooltips.", chart => Costs(chart, SeriesChartType.Pie)),
		Sample(SeriesChartType.Doughnut, "Composition", "Operating Cost Ring", "A doughnut presents the same cost mix with an adjustable empty center and color-coded categories.", chart => Costs(chart, SeriesChartType.Doughnut)),
		Sample(SeriesChartType.Funnel, "Composition", "Sales Conversion Funnel", "A descending pipeline follows prospects from first visit to a completed order. Values are simulated counts.", chart => Conversion(chart, SeriesChartType.Funnel)),
		Sample(SeriesChartType.Pyramid, "Composition", "Service Tier Pyramid", "A pyramid compares the size of successive subscription tiers with labels on each segment.", chart => Conversion(chart, SeriesChartType.Pyramid)),
		Sample(SeriesChartType.Point, "Distribution", "Latency Versus Throughput", "Individual samples reveal the relationship and spread between request throughput and response latency.", chart => Scatter(chart, false)),
		Sample(SeriesChartType.FastPoint, "Distribution", "Dense Scatter Samples", "240 independent observations demonstrate the fast point renderer without joining unrelated measurements with a line.", chart => Scatter(chart, true)),
		Sample(SeriesChartType.Bubble, "Distribution", "Portfolio Risk And Impact", "X is delivery effort, Y is expected impact, and bubble area adds the independent budget dimension.", chart => Bubbles(chart)),
		Sample(SeriesChartType.ErrorBar, "Distribution", "Experiment Uncertainty", "Each observation has a measured center and separate lower and upper confidence limits; all three Y values are supplied.", chart => Uncertainty(chart)),
		Sample(SeriesChartType.BoxPlot, "Distribution", "Response Time Distribution", "Each box supplies whiskers, quartiles, mean and median. A seventh value illustrates an outlying observation.", chart => Distributions(chart)),
		Sample(SeriesChartType.Range, "Ranges", "Temperature Envelope", "Lower and upper observations form a daily temperature band, with a separate center line.", chart => TemperatureRange(chart, SeriesChartType.Range)),
		Sample(SeriesChartType.SplineRange, "Ranges", "Smooth Forecast Band", "A smooth range ribbon conveys a forecast interval; the line is the central prediction rather than another limit.", chart => TemperatureRange(chart, SeriesChartType.SplineRange)),
		Sample(SeriesChartType.RangeColumn, "Ranges", "Daily Temperature Limits", "Each column spans a recorded low and high instead of starting from zero.", chart => TemperatureRange(chart, SeriesChartType.RangeColumn)),
		Sample(SeriesChartType.RangeBar, "Ranges", "Project Schedule", "Floating horizontal bars show task start and finish dates. This is an interval chart, not a duration measured from a zero baseline.", chart => Schedule(chart)),
		Sample(SeriesChartType.Stock, "Finance", "OHLC Price Sessions", "Each simulated session supplies high, low, open and close values. Tick marks preserve the four-value structure.", chart => Prices(chart, SeriesChartType.Stock)),
		Sample(SeriesChartType.Candlestick, "Finance", "Candlestick Price Sessions", "Bodies show open-to-close movement; wicks show the session range. Rising and falling sessions use different colors.", chart => Prices(chart, SeriesChartType.Candlestick)),
		Sample(SeriesChartType.Renko, "Finance", "Renko Price Movement", "Fixed-size bricks filter minor price changes. Dates identify source observations; horizontal spacing reflects generated bricks.", chart => Prices(chart, SeriesChartType.Renko)),
		Sample(SeriesChartType.ThreeLineBreak, "Finance", "Three Line Break", "Price reversals are rendered only after crossing the preceding three lines. This is not a uniformly spaced time chart.", chart => Prices(chart, SeriesChartType.ThreeLineBreak)),
		Sample(SeriesChartType.Kagi, "Finance", "Kagi Reversals", "A two-unit reversal threshold illustrates the Kagi renderer's direction and line-thickness changes.", chart => Prices(chart, SeriesChartType.Kagi)),
		Sample(SeriesChartType.PointAndFigure, "Finance", "Point And Figure", "One-unit boxes and a three-box reversal expose price direction using explicitly supplied high and low values.", chart => Prices(chart, SeriesChartType.PointAndFigure)),
		Sample(SeriesChartType.Radar, "Circular", "Service Quality Profile", "Two products are compared across five named dimensions on the same radial scale.", chart => Quality(chart)),
		Sample(SeriesChartType.Polar, "Circular", "Directional Signal", "Angular X values run around the circle, while radial Y values represent signal strength in two sweeps.", chart => Direction(chart)),
		new ChartGallerySample("DualAxis", "Advanced", "Revenue And Growth", "Combines currency columns with a percentage line using a separately labeled secondary Y axis.", DualAxis),
		new ChartGallerySample("AlignedAreas", "Advanced", "Price And Volume Panels", "Two vertically aligned plot areas share the date range while preserving their independent price and volume scales.", AlignedAreas),
		new ChartGallerySample("Logarithmic", "Advanced", "Orders Of Magnitude", "Positive measurements spanning five orders of magnitude use a base-10 logarithmic Y axis with both major and minor grids.", Logarithmic),
		new ChartGallerySample("Thresholds", "Advanced", "Annotated Service Threshold", "A shaded warning band and a data-anchored callout explain a simulated latency incident without adding a fake measurement series.", Thresholds)
	};

	private static ChartGallerySample Sample(SeriesChartType type, string category, string name, string description, Action<Chart> configure) => new(type.ToString(), category, name, description, configure);

	private static ChartArea Axes(Chart chart, string title, string horizontal, string vertical) {
		Guard.ArgumentNotNull(chart, nameof(chart));
		chart.Titles.Add(title);
		var area = chart.ChartAreas["Main"];
		area.AxisX.Title = horizontal;
		area.AxisY.Title = vertical;
		area.AxisX.MajorGrid.Enabled = false;
		return area;
	}

	private static Series Series(Chart chart, string name, SeriesChartType type, int color = 0, int values = 1, string area = "Main") {
		var series = new Series(name) {
			ChartType = type, ChartArea = area, Legend = "Legend", Color = _colors[color % _colors.Length],
			BorderWidth = 3, MarkerSize = 7, YValuesPerPoint = values, ToolTip = "#SERIESNAME: #VALY{N1}", LegendText = name
		};
		chart.Series.Add(series);
		return series;
	}

	private static void Point(Series series, double horizontal, string label, params double[] values) => series.Points.Add(new DataPoint(horizontal, values) { AxisLabel = label });

	private static void Orders(Chart chart, SeriesChartType type) {
		var area = Axes(chart, "Daily Orders By Channel", "Date", "Orders");
		area.AxisX.LabelStyle.Format = "dd MMM";
		foreach (var channel in new[] { 0, 1 }) {
			var series = Series(chart, channel == 0 ? "Online" : "In Store", type, channel);
			series.XValueType = ChartValueType.DateTime;
			series.MarkerStyle = MarkerStyle.Circle;
			for (var day = 0; day < 14; day++) {
				var index = series.Points.AddXY(new DateTime(2026, 1, 5).AddDays(day), 76 + channel * 28 + day * 4 + 15 * Math.Sin(day * 0.7 + channel));
				if (channel == 0 && day == 6) series.Points[index].IsEmpty = true;
			}
		}
	}

	private static void Satisfaction(Chart chart) {
		var area = Axes(chart, "Weekly Customer Satisfaction", "Week", "Score / 100");
		area.AxisY.Minimum = 50;
		area.AxisY.Maximum = 100;
		for (var team = 0; team < 2; team++) {
			var series = Series(chart, team == 0 ? "Product A" : "Product B", SeriesChartType.Spline, team);
			series.MarkerStyle = MarkerStyle.Circle;
			for (var week = 1; week <= 12; week++)
				Point(series, week, string.Empty, 70 + week * 1.5 + 6 * Math.Sin(week * 0.8 + team) + team * 3);
		}
	}

	private static void Capacity(Chart chart) {
		Axes(chart, "Provisioned Capacity", "Hour", "Worker Instances");
		var series = Series(chart, "Provisioned Workers", SeriesChartType.StepLine);
		var values = new[] { 4, 4, 4, 8, 8, 12, 12, 12, 6, 6, 4, 4 };
		for (var hour = 0; hour < values.Length; hour++) Point(series, hour, string.Empty, values[hour]);
	}

	private static void Signal(Chart chart) {
		Axes(chart, "Simulated Signal Detail", "Time (s)", "Amplitude");
		var series = Series(chart, "Signal", SeriesChartType.FastLine);
		for (var index = 0; index < 240; index++) Point(series, index / 20D, string.Empty, 2 * Math.Sin(index / 10D) + 0.45 * Math.Sin(index * 1.7));
	}

	private static void Energy(Chart chart, SeriesChartType type) {
		Axes(chart, "Daily Energy Demand", "Day", "Energy (kWh)");
		var demand = Series(chart, "Measured Demand", type);
		demand.Color = Color.FromArgb(130, _colors[0]);
		var baseline = Series(chart, "Expected Demand", SeriesChartType.Line, 1);
		for (var day = 1; day <= 28; day++) {
			Point(demand, day, string.Empty, 150 + 32 * Math.Sin(day / 2D) + day);
			Point(baseline, day, string.Empty, 155 + day * 0.8);
		}
	}

	private static void Traffic(Chart chart) {
		Axes(chart, "Hourly Site Traffic", "Hour Of Day", "Visits");
		var series = Series(chart, "Site Visits", SeriesChartType.SplineArea, 2);
		series.Color = Color.FromArgb(150, _colors[2]);
		for (var hour = 0; hour < 24; hour++) Point(series, hour, string.Empty, 30 + 180 * Math.Exp(-Math.Pow((hour - 14) / 5D, 2)));
	}

	private static void Workloads(Chart chart, SeriesChartType type) {
		var area = Axes(chart, type == SeriesChartType.StackedArea100 ? "Workload Share" : "Capacity By Workload", "Hour", type == SeriesChartType.StackedArea100 ? "Share (%)" : "CPU Cores");
		if (type == SeriesChartType.StackedArea100) area.AxisY.LabelStyle.Format = "0'%'";
		var names = new[] { "Interactive", "Batch", "Background" };
		for (var workload = 0; workload < names.Length; workload++) {
			var series = Series(chart, names[workload], type, workload);
			for (var hour = 0; hour < 18; hour++) Point(series, hour, string.Empty, 12 + workload * 5 + 8 * (1 + Math.Sin(hour / 3D + workload)));
		}
	}

	private static void Revenue(Chart chart, SeriesChartType type) {
		var area = Axes(chart, "Quarterly Revenue", "Quarter", "Revenue ($000)");
		area.AxisY.LabelStyle.Format = "$0";
		for (var year = 0; year < 2; year++) {
			var series = Series(chart, (2025 + year).ToString(), type, year);
			for (var quarter = 0; quarter < 4; quarter++) Point(series, quarter + 1, "Q" + (quarter + 1), 118 + quarter * 23 + year * (22 + quarter * 7));
		}
	}

	private static void CashFlow(Chart chart) {
		Axes(chart, "Regional Net Cash Flow", "Region", "Net Cash Flow ($000)");
		var series = Series(chart, "Net Cash Flow", SeriesChartType.Bar);
		var labels = new[] { "North", "South", "East", "West", "Central" };
		var values = new[] { 48D, 71, -23, 56, -12 };
		for (var index = 0; index < values.Length; index++) {
			Point(series, index + 1, labels[index], values[index]);
			series.Points[index].Color = values[index] < 0 ? _colors[4] : _colors[0];
		}
	}

	private static void Channels(Chart chart, SeriesChartType type) {
		var normalized = type == SeriesChartType.StackedColumn100;
		var area = Axes(chart, normalized ? "Monthly Channel Mix" : "Orders By Channel", "Month", normalized ? "Share (%)" : "Orders");
		if (normalized) area.AxisY.LabelStyle.Format = "0'%'";
		var names = new[] { "Web", "Retail", "Partners" };
		var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
		for (var channel = 0; channel < names.Length; channel++) {
			var series = Series(chart, names[channel], type, channel);
			for (var month = 0; month < months.Length; month++) Point(series, month + 1, months[month], 35 + channel * 20 + month * (9 - channel * 3));
		}
	}

	private static void Support(Chart chart, SeriesChartType type) {
		var normalized = type == SeriesChartType.StackedBar100;
		var area = Axes(chart, normalized ? "Support Priority Mix" : "Support Workload", "Team", normalized ? "Share (%)" : "Open Tickets");
		if (normalized) area.AxisY.LabelStyle.Format = "0'%'";
		var names = new[] { "Urgent", "Normal", "Low" };
		var teams = new[] { "Desktop", "Platform", "Network", "Applications" };
		for (var priority = 0; priority < names.Length; priority++) {
			var series = Series(chart, names[priority], type, priority);
			for (var team = 0; team < teams.Length; team++) Point(series, team + 1, teams[team], 7 + priority * 13 + team * (priority + 2));
		}
	}

	private static void Costs(Chart chart, SeriesChartType type) {
		var area = Axes(chart, "Operating Cost Mix", string.Empty, string.Empty);
		area.Area3DStyle.Enable3D = true;
		area.Area3DStyle.Inclination = 35;
		var series = Series(chart, "Operating Cost", type);
		series["PieLabelStyle"] = "Outside";
		if (type == SeriesChartType.Doughnut) series["DoughnutRadius"] = "65";
		var names = new[] { "Engineering", "Cloud", "Sales", "Operations", "Administration" };
		var costs = new[] { 115D, 72, 48, 34, 27 };
		for (var index = 0; index < names.Length; index++) {
			Point(series, index + 1, names[index], costs[index]);
			series.Points[index].Color = _colors[index];
			series.Points[index].LegendText = names[index];
			series.Points[index].Label = "#AXISLABEL\n#PERCENT{P0}";
			series.Points[index].ToolTip = "#AXISLABEL: $ #VALY{N0}, #PERCENT{P1}";
		}
	}

	private static void Conversion(Chart chart, SeriesChartType type) {
		var area = Axes(chart, type == SeriesChartType.Funnel ? "Sales Conversion Funnel" : "Subscription Tiers", string.Empty, string.Empty);
		area.Area3DStyle.Enable3D = true;
		var series = Series(chart, type == SeriesChartType.Funnel ? "Prospects" : "Subscribers", type);
		var names = type == SeriesChartType.Funnel ? new[] { "Visitors", "Engaged", "Qualified", "Trial", "Purchased" } : new[] { "Free", "Starter", "Professional", "Business", "Enterprise" };
		var values = new[] { 1000D, 650, 420, 180, 80 };
		for (var index = 0; index < values.Length; index++) {
			Point(series, index + 1, names[index], values[index]);
			series.Points[index].Color = _colors[index];
			series.Points[index].Label = "#AXISLABEL: #VALY{N0}";
		}
	}

	private static void Scatter(Chart chart, bool fast) {
		Axes(chart, fast ? "Dense Throughput And Latency Samples" : "Latency Versus Throughput", "Throughput (requests/s)", "Latency (ms)");
		for (var region = 0; region < (fast ? 1 : 2); region++) {
			var series = Series(chart, fast ? "Samples" : region == 0 ? "Region A" : "Region B", fast ? SeriesChartType.FastPoint : SeriesChartType.Point, region);
			series.MarkerStyle = MarkerStyle.Circle;
			series.MarkerSize = fast ? 4 : 8;
			for (var index = 0; index < (fast ? 240 : 34); index++) {
				var throughput = 30 + index % 80 + 8 * Math.Sin(index * 1.37);
				Point(series, throughput, string.Empty, 12 + throughput * 0.42 + 8 * Math.Sin(index * 2.7 + region) + region * 9);
			}
		}
	}

	private static void Bubbles(Chart chart) {
		Axes(chart, "Project Portfolio", "Delivery Effort (weeks)", "Expected Impact (score)");
		var series = Series(chart, "Projects • Bubble Size = Budget", SeriesChartType.Bubble, 0, 2);
		series["BubbleMaxSize"] = "18";
		series["BubbleMinSize"] = "4";
		var effort = new[] { 3D, 5, 7, 9, 11, 13, 16, 18 };
		var impact = new[] { 24D, 65, 45, 83, 51, 74, 92, 61 };
		for (var index = 0; index < effort.Length; index++) {
			Point(series, effort[index], "P" + (index + 1), impact[index], 20 + index * 13);
			series.Points[index].Color = Color.FromArgb(175, _colors[index % _colors.Length]);
			series.Points[index].ToolTip = "#AXISLABEL • Effort #VALX weeks • Impact #VALY1 • Budget #VALY2";
		}
	}

	private static void Uncertainty(Chart chart) {
		Axes(chart, "Experiment Confidence Intervals", "Experiment", "Measured Response");
		var series = Series(chart, "Mean And 95% Interval", SeriesChartType.ErrorBar, 0, 3);
		series["ErrorBarStyle"] = "Both";
		series["ErrorBarCenterMarkerStyle"] = "Circle";
		for (var index = 0; index < 6; index++) {
			var center = 42 + index * 5 + 3 * Math.Sin(index);
			Point(series, index + 1, "Trial " + (index + 1), center, center - 4 - index, center + 5 + index);
		}
	}

	private static void Distributions(Chart chart) {
		Axes(chart, "Response Time Distribution", "Service", "Response Time (ms)");
		var series = Series(chart, "Distribution", SeriesChartType.BoxPlot, 0, 7);
		series["BoxPlotShowAverage"] = "True";
		series["BoxPlotShowMedian"] = "True";
		series["BoxPlotShowUnusualValues"] = "True";
		var names = new[] { "Search", "Catalogue", "Checkout", "Account" };
		for (var index = 0; index < names.Length; index++) {
			var offset = index * 11;
			Point(series, index + 1, names[index], 12 + offset, 76 + offset, 25 + offset, 55 + offset, 39 + offset, 36 + offset, 95 + offset);
			series.Points[index].Color = Color.FromArgb(150, _colors[index]);
		}
	}

	private static void TemperatureRange(Chart chart, SeriesChartType type) {
		Axes(chart, "Daily Temperature Envelope", "Day", "Temperature (°C)");
		var band = Series(chart, "Low / High", type, 0, 2);
		band.Color = Color.FromArgb(130, _colors[0]);
		var center = type == SeriesChartType.RangeColumn ? null : Series(chart, "Central Estimate", SeriesChartType.Line, 1);
		for (var day = 1; day <= 14; day++) {
			var expected = 20 + 4 * Math.Sin(day / 3D);
			Point(band, day, string.Empty, expected - 5 - day % 3, expected + 4 + day % 2);
			if (center != null) Point(center, day, string.Empty, expected);
		}
	}

	private static void Schedule(Chart chart) {
		var area = Axes(chart, "Project Delivery Schedule", "Workstream", "Date");
		area.AxisY.LabelStyle.Format = "dd MMM";
		area.AxisY.IsStartedFromZero = false;
		var series = Series(chart, "Scheduled Work", SeriesChartType.RangeBar, 0, 2);
		series.YValueType = ChartValueType.DateTime;
		var tasks = new[] { "Design", "Implementation", "Integration", "Validation", "Deployment" };
		for (var index = 0; index < tasks.Length; index++) {
			var start = new DateTime(2026, 2, 2).AddDays(index * 5);
			Point(series, index + 1, tasks[index], start.ToOADate(), start.AddDays(7 + index).ToOADate());
			series.Points[index].Color = _colors[index];
			series.Points[index].ToolTip = tasks[index] + ": " + start.ToString("dd MMM") + " – " + start.AddDays(7 + index).ToString("dd MMM");
		}
	}

	private static void Prices(Chart chart, SeriesChartType type) {
		var area = Axes(chart, "Simulated " + type + " Price Sessions", "Session", "Price ($)");
		area.AxisX.LabelStyle.Format = "dd MMM";
		area.AxisY.IsStartedFromZero = false;
		area.AxisY.LabelStyle.Format = "$0.00";
		var ohlc = type is SeriesChartType.Stock or SeriesChartType.Candlestick;
		var series = Series(chart, "Simulated Price", type, 0, ohlc ? 4 : type == SeriesChartType.PointAndFigure ? 2 : 1);
		series.XValueType = ChartValueType.DateTime;
		series["PriceUpColor"] = "MediumSeaGreen";
		series["PriceDownColor"] = "IndianRed";
		if (type is SeriesChartType.Renko or SeriesChartType.PointAndFigure) series["BoxSize"] = "1";
		if (type == SeriesChartType.PointAndFigure) series["ReversalAmount"] = "3";
		if (type == SeriesChartType.Kagi) series["ReversalAmount"] = "2";
		if (type == SeriesChartType.ThreeLineBreak) series["NumberOfLinesInBreak"] = "3";
		for (var session = 0; session < 42; session++) {
			var opening = 92 + session * 0.35 + 6 * Math.Sin(session / 3D);
			var closing = opening + 2 * Math.Sin(session * 1.1 + 0.4);
			var high = Math.Max(opening, closing) + 1.5;
			var low = Math.Min(opening, closing) - 1.3;
			var date = new DateTime(2026, 3, 2).AddDays(session).ToOADate();
			if (ohlc) Point(series, date, string.Empty, high, low, opening, closing);
			else if (type == SeriesChartType.PointAndFigure) Point(series, date, string.Empty, high, low);
			else Point(series, date, string.Empty, closing);
		}
	}

	private static void Quality(Chart chart) {
		var area = Axes(chart, "Service Quality Profile", string.Empty, "Score / 100");
		area.AxisY.Minimum = 0;
		area.AxisY.Maximum = 100;
		var labels = new[] { "Reliability", "Performance", "Usability", "Coverage", "Support" };
		for (var product = 0; product < 2; product++) {
			var series = Series(chart, "Product " + (product == 0 ? "A" : "B"), SeriesChartType.Radar, product);
			series["RadarDrawingStyle"] = "Area";
			series.Color = Color.FromArgb(95, _colors[product]);
			series.MarkerStyle = MarkerStyle.Circle;
			for (var axis = 0; axis < labels.Length; axis++) Point(series, axis + 1, labels[axis], 65 + 20 * Math.Sin(axis * 1.8 + product));
		}
	}

	private static void Direction(Chart chart) {
		var area = Axes(chart, "Directional Signal Strength", "Bearing (°)", "Strength");
		area.AxisY.Minimum = 0;
		for (var sweep = 0; sweep < 2; sweep++) {
			var series = Series(chart, "Sweep " + (sweep + 1), SeriesChartType.Polar, sweep);
			series["PolarDrawingStyle"] = "Line";
			for (var angle = 0; angle <= 360; angle += 15) Point(series, angle, string.Empty, 40 + 23 * Math.Cos((angle - sweep * 45) * Math.PI / 180) + 9 * Math.Cos(angle * Math.PI / 60));
		}
	}

	private static void DualAxis(Chart chart) {
		var area = Axes(chart, "Revenue And Growth", "Quarter", "Revenue ($000)");
		area.AxisY2.Enabled = AxisEnabled.True;
		area.AxisY2.Title = "Growth (%)";
		area.AxisY2.LabelStyle.Format = "0'%'";
		area.AxisY2.MajorGrid.Enabled = false;
		var revenue = Series(chart, "Revenue", SeriesChartType.Column);
		var growth = Series(chart, "Growth", SeriesChartType.Line, 1);
		growth.YAxisType = AxisType.Secondary;
		growth.MarkerStyle = MarkerStyle.Circle;
		for (var quarter = 0; quarter < 8; quarter++) {
			Point(revenue, quarter + 1, "Q" + (quarter % 4 + 1) + " " + (2025 + quarter / 4), 105 + quarter * 18);
			Point(growth, quarter + 1, string.Empty, 8 + quarter * 1.5 + 4 * Math.Sin(quarter));
		}
	}

	private static void AlignedAreas(Chart chart) {
		var area = Axes(chart, "Aligned Price And Volume", string.Empty, "Price ($)");
		area.Position = new ElementPosition(5, 8, 90, 53);
		area.AxisX.LabelStyle.Enabled = false;
		area.AxisY.IsStartedFromZero = false;
		var volumeArea = new ChartArea("Volume") {
			Position = new ElementPosition(5, 65, 90, 27), AlignWithChartArea = "Main",
			AlignmentOrientation = AreaAlignmentOrientations.Vertical, AlignmentStyle = AreaAlignmentStyles.PlotPosition | AreaAlignmentStyles.AxesView
		};
		volumeArea.AxisX.LabelStyle.Format = "dd MMM";
		volumeArea.AxisX.MajorGrid.Enabled = false;
		volumeArea.AxisY.Title = "Volume";
		chart.ChartAreas.Add(volumeArea);
		var price = Series(chart, "Closing Price", SeriesChartType.Line);
		var volume = Series(chart, "Traded Volume", SeriesChartType.Column, 2, 1, "Volume");
		price.XValueType = volume.XValueType = ChartValueType.DateTime;
		for (var session = 0; session < 28; session++) {
			var date = new DateTime(2026, 4, 1).AddDays(session).ToOADate();
			Point(price, date, string.Empty, 100 + session * 0.6 + 7 * Math.Sin(session / 4D));
			Point(volume, date, string.Empty, 1500 + 700 * Math.Abs(Math.Cos(session * 0.8)) + session * 25);
		}
	}

	private static void Logarithmic(Chart chart) {
		var area = Axes(chart, "Measurements Across Orders Of Magnitude", "Sample", "Concentration (log₁₀)");
		area.AxisY.IsLogarithmic = true;
		area.AxisY.LogarithmBase = 10;
		area.AxisY.IsStartedFromZero = false;
		area.AxisY.LabelStyle.Format = "0.####";
		area.AxisY.MinorGrid.Enabled = true;
		area.AxisY.MinorGrid.LineColor = Color.FromArgb(238, 241, 246);
		var measured = Series(chart, "Measured Concentration", SeriesChartType.Line);
		measured.MarkerStyle = MarkerStyle.Circle;
		for (var sample = 0; sample < 16; sample++) Point(measured, sample + 1, string.Empty, 0.01 * Math.Pow(1.9, sample));
	}

	private static void Thresholds(Chart chart) {
		var area = Axes(chart, "Service Latency And Warning Threshold", "Minute", "Latency (ms)");
		area.AxisY.Maximum = 120;
		area.AxisY.StripLines.Add(new StripLine { IntervalOffset = 80, StripWidth = 40, BackColor = Color.FromArgb(35, _colors[4]), Text = "Warning Range", ForeColor = _colors[4] });
		var latency = Series(chart, "Latency", SeriesChartType.Line);
		for (var minute = 0; minute < 24; minute++) Point(latency, minute, string.Empty, 38 + 8 * Math.Sin(minute * 0.7) + 62 * Math.Exp(-Math.Pow((minute - 15) / 2D, 2)));
		chart.Annotations.Add(new CalloutAnnotation {
			Text = "Simulated Incident", AnchorDataPoint = latency.Points[15], CalloutStyle = CalloutStyle.RoundedRectangle,
			BackColor = Color.FromArgb(255, 249, 232), ForeColor = Color.FromArgb(104, 67, 18), LineColor = _colors[1],
			AnchorAlignment = ContentAlignment.BottomRight, Width = 24, Height = 9
		});
	}
}
