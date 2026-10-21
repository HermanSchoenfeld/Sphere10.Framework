// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class GraphChartTests {
	[TestCase(GraphChartStyle.Line)]
	[TestCase(GraphChartStyle.Area)]
	[TestCase(GraphChartStyle.Column)]
	[TestCase(GraphChartStyle.Bar)]
	public void CenteredEmptyStateTracksInitialMissingZeroAndClearedSnapshots(GraphChartStyle style) {
		using var host = new Form { ClientSize = new Size(640, 360) };
		using var chart = new GraphChart { Style = style, Dock = DockStyle.Fill, PlotBackColor = Color.LightBlue };
		host.Controls.Add(chart);
		host.Show();
		host.Update();
		var label = chart.Controls.Find("chartEmptyState", true).OfType<Label>().Single();
		Assert.That(label.Visible, Is.True, "A newly opened chart must explain why it is empty.");
		Assert.That(label.Text, Is.EqualTo("NO DATA"));
		Assert.That(label.TextAlign, Is.EqualTo(ContentAlignment.MiddleCenter));
		Assert.That(label.Bounds, Is.EqualTo(chart.ClientRectangle));
		Assert.That(chart.Controls.GetChildIndex(label), Is.Zero);
		Assert.That(label.BackColor, Is.EqualTo(Color.LightBlue));
		chart.SetSeries(new[] { new GraphChartSeries("Unavailable", new[] { new GraphChartPoint(0, null), new GraphChartPoint(1, double.NaN),
			new GraphChartPoint(2, double.PositiveInfinity) }) });
		Assert.That(label.Visible, Is.True, "Unavailable and invalid samples must not count as captured values.");
		chart.SetSeries(new[] { new GraphChartSeries("Measured Zero", new[] { new GraphChartPoint(0, 0) }) });
		Assert.That(label.Visible, Is.False, "Zero is a valid measurement and must display the actual chart.");
		chart.Clear();
		host.ClientSize = new Size(840, 480);
		host.PerformLayout();
		host.Update();
		Assert.That(label.Visible, Is.True, "Resetting capture must restore the empty-state message.");
		Assert.That(label.Bounds, Is.EqualTo(chart.ClientRectangle));
		using var bitmap = new Bitmap(chart.Width, chart.Height);
		chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size));
		Assert.That(Enumerable.Range(chart.Width / 2 - 40, 80).Any(x => Enumerable.Range(chart.Height / 2 - 15, 30)
			.Any(y => bitmap.GetPixel(x, y).ToArgb() != Color.LightBlue.ToArgb())), Is.True, "The message must actually render in the chart center.");
	}

	[Test]
	public void EmptyStateCaptionCanBeConfiguredOrSuppressedWithoutAffectingTheData() {
		using var chart = new GraphChart();
		var label = chart.Controls.Find("chartEmptyState", true).OfType<Label>().Single();
		chart.NoDataText = "Waiting For Samples";
		Assert.That(label.Text, Is.EqualTo("Waiting For Samples"));
		Assert.That(label.Visible, Is.True);
		chart.NoDataText = string.Empty;
		Assert.That(label.Visible, Is.False);
		chart.NoDataText = "NO DATA";
		Assert.That(label.Visible, Is.True);
		chart.SetSeries(new[] { new GraphChartSeries("Captured", new[] { new GraphChartPoint(0, 12) }) });
		chart.NoDataText = "Waiting";
		Assert.That(label.Visible, Is.False);
		Assert.That(chart.Series.Single().Points.Single().Y, Is.EqualTo(12));
	}

	[Test]
	public void TelemetryAndDedicatedBarControlsInheritTheEmptyStateForMissingAndResetCaptures() {
		using var telemetry = new TelemetryChart { Unit = TelemetryChartUnit.Celsius, PlotStyle = TelemetryChartStyle.Area };
		using var bars = new BarChart();
		var historyLabel = telemetry.Controls.Find("chartEmptyState", true).OfType<Label>().Single();
		var barLabel = bars.Controls.Find("chartEmptyState", true).OfType<Label>().Single();
		Assert.That(historyLabel.Visible, Is.True);
		Assert.That(barLabel.Visible, Is.True);
		telemetry.SetSamples(new[] { new TelemetryChartPoint(DateTime.UtcNow, null) });
		bars.SetSeries(new[] { new GraphChartSeries("Missing", new[] { new GraphChartPoint(0, null) }) });
		Assert.That(historyLabel.Visible, Is.True);
		Assert.That(barLabel.Visible, Is.True);
		telemetry.SetSamples(new[] { new TelemetryChartPoint(DateTime.UtcNow, 0) });
		bars.SetSeries(new[] { new GraphChartSeries("Zero", new[] { new GraphChartPoint(0, 0) }) });
		Assert.That(historyLabel.Visible, Is.False);
		Assert.That(barLabel.Visible, Is.False);
		telemetry.SetSamples(Array.Empty<TelemetryChartPoint>());
		bars.Clear();
		Assert.That(historyLabel.Visible, Is.True);
		Assert.That(barLabel.Visible, Is.True);
	}

	[Test]
	public void SnapshotOwnershipAndRejectedInputKeepTheDisplayedSeriesIntact() {
		using var chart = new GraphChart();
		var points = new[] { new GraphChartPoint(0, -12), new GraphChartPoint(5, 24) };
		var series = new GraphChartSeries("Sensor", points, Color.Crimson);
		var input = new[] { series };
		chart.SetSeries(input);
		points[0] = new GraphChartPoint(0, 999);
		input[0] = null;
		series.Points[1] = new GraphChartPoint(5, 999);
		chart.Series[0] = null;
		Assert.That(chart.Series.Single().Points.Select(point => point.Y), Is.EqualTo(new double?[] { -12, 24 }));
		Assert.That(() => chart.SetSeries(input), Throws.ArgumentNullException);
		Assert.That(() => chart.SetSeries(new[] { series, series }), Throws.ArgumentException);
		Assert.That(chart.Series.Single(), Is.SameAs(series));
		Assert.That(() => new GraphChartSeries("Invalid", new[] { new GraphChartPoint(double.PositiveInfinity, 1) }), Throws.ArgumentException);
	}

	[TestCase(GraphChartStyle.Line)]
	[TestCase(GraphChartStyle.Area)]
	[TestCase(GraphChartStyle.Column)]
	[TestCase(GraphChartStyle.Bar)]
	public void NumericSeriesRenderNegativeValuesAndResizeUsingTheChartEngine(GraphChartStyle style) {
		using var chart = new GraphChart { Style = style, MinimumY = -20, MaximumY = 140, ShowLegend = true, YValueFormat = "0.0 °C" };
		chart.SetSeries(new[] {
			new GraphChartSeries("CPU", new[] { new GraphChartPoint(0, -10, "Core 0"), new GraphChartPoint(1, 120, "Core 1") }, Color.Firebrick),
			new GraphChartSeries("Board", new[] { new GraphChartPoint(0, 30, "Core 0"), new GraphChartPoint(1, 60, "Core 1") }, Color.SeaGreen)
		});
		foreach (var size in new[] { new Size(420, 240), new Size(840, 480) }) {
			chart.Size = size;
			chart.CreateControl();
			chart.PerformLayout();
			using var bitmap = new Bitmap(size.Width, size.Height);
			Assert.That(() => chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, size)), Throws.Nothing);
			Assert.That(chart.PlotBounds.Width, Is.GreaterThan(size.Width / 2));
			Assert.That(chart.NativeChart.Series, Has.Count.EqualTo(2));
			Assert.That(Enumerable.Range(0, bitmap.Width).Any(x => Enumerable.Range(0, bitmap.Height).Any(y => bitmap.GetPixel(x, y).ToArgb() == Color.Firebrick.ToArgb())), Is.True);
		}
	}

	[Test]
	public void AreaGapsRemainUnpaintedAndNativeSeriesAreReusedAcrossSnapshots() {
		using var chart = new GraphChart { Style = GraphChartStyle.Area, ShowAxes = false, MinimumX = 0, MaximumX = 4, MinimumY = 0, MaximumY = 100,
			PlotBackColor = Color.LightBlue, GridColor = Color.LightBlue };
		chart.SetSeries(new[] { new GraphChartSeries("Temperature", new[] { new GraphChartPoint(0, 40), new GraphChartPoint(1, 40),
			new GraphChartPoint(2, null), new GraphChartPoint(3, 40), new GraphChartPoint(4, 40) }, Color.DarkRed) });
		var native = chart.NativeChart.Series[0];
		chart.CreateControl();
		chart.PerformLayout();
		using var bitmap = new Bitmap(chart.Width, chart.Height);
		chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size));
		var bounds = chart.PlotBounds;
		Assert.That(bitmap.GetPixel((int)(bounds.Left + bounds.Width / 8), (int)(bounds.Top + bounds.Height * .75)).ToArgb(), Is.EqualTo(Color.DarkRed.ToArgb()));
		Assert.That(bitmap.GetPixel((int)(bounds.Left + bounds.Width / 2), (int)(bounds.Top + bounds.Height * .75)).ToArgb(), Is.EqualTo(Color.LightBlue.ToArgb()));
		chart.SetSeries(new[] { new GraphChartSeries("Temperature", new[] { new GraphChartPoint(0, 50), new GraphChartPoint(4, 60) }, Color.DarkRed) });
		Assert.That(chart.NativeChart.Series, Has.Count.EqualTo(1));
		Assert.That(chart.NativeChart.Series[0], Is.SameAs(native));
		chart.Clear();
		Assert.That(chart.NativeChart.Series, Is.Empty);
		Assert.That(chart.TryGetPointAt(Point.Round(new PointF(bounds.Left + 10, bounds.Top + 10)), out _, out _), Is.False);
	}

	[Test]
	public void HorizontalBarLookupUsesTheVerticalCategoryAxisAndRetainsMissingCategories() {
		using var chart = new BarChart { Style = GraphChartStyle.Bar };
		chart.SetSeries(new[] { new GraphChartSeries("Cores", new[] { new GraphChartPoint(0, 34, "Core 0"), new GraphChartPoint(1, null, "Core 1"),
			new GraphChartPoint(2, 42, "Core 2") }, Color.Firebrick) });
		chart.CreateControl();
		chart.PerformLayout();
		using var bitmap = new Bitmap(chart.Width, chart.Height);
		chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size));
		var native = chart.NativeChart;
		Assert.That(native.Series, Has.Count.EqualTo(1));
		Assert.That(native.Series[0].Points[1].IsEmpty, Is.True);
		Assert.That(native.ChartAreas[0].AxisX.IsMarginVisible, Is.True);
		var y = native.ChartAreas[0].AxisX.ValueToPixelPosition(2);
		var bounds = chart.PlotBounds;
		Assert.That(chart.TryGetPointAt(new Point((int)(bounds.Left + bounds.Width / 2), (int)y), out _, out var sample), Is.True);
		Assert.That(sample.Label, Is.EqualTo("Core 2"));
	}

	[Test]
	public void SingleAndUnavailableObservationsRenderWithoutInventingMeasurements() {
		using var chart = new GraphChart { Style = GraphChartStyle.Line };
		chart.SetSeries(new[] { new GraphChartSeries("Sensor", new[] { new GraphChartPoint(0, 23), new GraphChartPoint(1, double.NaN), new GraphChartPoint(2, double.PositiveInfinity) }) });
		Assert.That(chart.Series.Single().Points.Select(point => point.Y), Is.EqualTo(new double?[] { 23, null, null }));
		chart.CreateControl();
		using var bitmap = new Bitmap(chart.Width, chart.Height);
		Assert.That(() => chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size)), Throws.Nothing);
		Assert.That(chart.NativeChart.Series[0].MarkerStyle, Is.EqualTo(MarkerStyle.Circle));
		chart.SetSeries(new[] { new GraphChartSeries("Sensor", new[] { new GraphChartPoint(0, null), new GraphChartPoint(2, null) }) });
		Assert.That(() => chart.DrawToBitmap(bitmap, new Rectangle(Point.Empty, chart.Size)), Throws.Nothing);
		Assert.That(chart.Series.Single().Points.Select(point => point.Y), Is.All.Null);
	}
}
