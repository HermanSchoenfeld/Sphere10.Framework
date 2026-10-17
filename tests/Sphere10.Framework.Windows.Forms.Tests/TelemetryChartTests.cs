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
using NUnit.Framework;


using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class TelemetryChartTests {
	[Test]
	public void OwnsSnapshotMembershipAndRejectedInputDoesNotReplaceTheDisplayedHistory() {
		using var chart = new TelemetryChart();
		var time = DateTime.UtcNow;
		var input = new[] { new TelemetryChartPoint(time, 20), new TelemetryChartPoint(time.AddSeconds(1), 40) };
		chart.SetSamples(input);
		input[0] = null;
		var copy = chart.Points;
		copy[1] = null;
		Assert.That(chart.Points.Select(point => point.Value), Is.EqualTo(new double?[] { 20, 40 }));
		Assert.That(() => chart.SetSamples(input), Throws.ArgumentNullException);
		Assert.That(chart.Points.Select(point => point.Value), Is.EqualTo(new double?[] { 20, 40 }));
	}

	[Test]
	public void AreaRenderingLeavesMissingIntervalsUnfilledAndResizesTheCapturedHistory() {
		using var chart = new TelemetryChart {
			Compact = true, PlotStyle = TelemetryChartStyle.Area, FillColor = Color.DarkRed, LineColor = Color.Red,
			PlotBackColor = Color.LightBlue, GridColor = Color.LightBlue
		};
		var time = DateTime.UtcNow;
		chart.SetSamples(new[] { new TelemetryChartPoint(time, 40), new TelemetryChartPoint(time.AddSeconds(1), 40),
			new TelemetryChartPoint(time.AddSeconds(2), null), new TelemetryChartPoint(time.AddSeconds(3), 40), new TelemetryChartPoint(time.AddSeconds(4), 40) });
		foreach (var size in new[] { new Size(400, 210), new Size(800, 300) }) {
			chart.Size = size;
			chart.CreateControl();
			chart.PerformLayout();
			var plot = chart.Controls.Find("chartPlot", true).Single();
			using var image = new Bitmap(chart.Width, chart.Height);
			chart.DrawToBitmap(image, new Rectangle(Point.Empty, chart.Size));
			Assert.That(image.GetPixel(plot.Left + plot.Width / 8, plot.Top + plot.Height * 3 / 4).ToArgb(), Is.EqualTo(Color.DarkRed.ToArgb()));
			Assert.That(image.GetPixel(plot.Left + plot.Width / 2, plot.Top + plot.Height * 3 / 4).ToArgb(), Is.EqualTo(Color.LightBlue.ToArgb()),
				"Unavailable samples break the filled series; a missing interval must never imply a measured temperature.");
			Assert.That(chart.Points.Select(point => point.Value), Is.EqualTo(new double?[] { 40, 40, null, 40, 40 }));
		}
	}

	[Test]
	public void HoverUsesCapturedTimeRatherThanArrayIndexAndReportsMissingReadings() {
		using var chart = new TelemetryChart { Compact = true, Unit = TelemetryChartUnit.Celsius };
		var time = DateTime.UtcNow;
		chart.SetSamples(new[] { new TelemetryChartPoint(time, 36), new TelemetryChartPoint(time.AddSeconds(1), null), new TelemetryChartPoint(time.AddSeconds(10), 48) });
		chart.CreateControl();
		chart.PerformLayout();
		var plot = chart.Controls.Find("chartPlot", true).Single();
		Assert.That(chart.TryGetSampleAt(new Point(plot.Left + 2 + (plot.Width - 4) / 10, plot.Top + plot.Height / 2), out var gap), Is.True);
		Assert.That(gap.CapturedAtUtc, Is.EqualTo(time.AddSeconds(1)));
		Assert.That(gap.Value, Is.Null);
		Assert.That(chart.TryGetSampleAt(new Point(plot.Right - 3, plot.Top + plot.Height / 2), out var latest), Is.True);
		Assert.That(latest.Value, Is.EqualTo(48));
		Assert.That(chart.TryGetSampleAt(new Point(-1, -1), out _), Is.False);
	}

	[TestCase(-20)]
	[TestCase(0)]
	[TestCase(120)]
	public void CelsiusRetainsRealTemperaturesAndFormatsDegreesInsteadOfPercent(double value) {
		using var chart = new TelemetryChart { Unit = TelemetryChartUnit.Celsius, LineColor = Color.Firebrick };
		var start = DateTime.UtcNow;
		chart.SetSamples(new[] { new TelemetryChartPoint(start, value), new TelemetryChartPoint(start.AddSeconds(1), value + 1) });
		Assert.That(chart.Points.Select(point => point.Value), Is.EqualTo(new double?[] { value, value + 1 }));
		var label = chart.Controls.Find("chartValue", true).OfType<Label>().Single();
		Assert.That(label.Text, Does.EndWith(" °C").And.Not.Contain("%"));
		using var image = new Bitmap(chart.Width, chart.Height);
		chart.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
		Assert.That(Enumerable.Range(0, image.Width).Any(x => Enumerable.Range(0, image.Height).Any(y => image.GetPixel(x, y).ToArgb() == Color.Firebrick.ToArgb())), Is.True,
			"The valid temperature series must render even below zero or above the normal CPU scale.");
	}

	[Test]
	public void InvalidTemperatureSamplesRemainGapsWithoutInventingZeroValues() {
		using var chart = new TelemetryChart { Unit = TelemetryChartUnit.Celsius };
		var start = DateTime.UtcNow;
		chart.SetSamples(new[] { new TelemetryChartPoint(start, double.NaN), new TelemetryChartPoint(start.AddSeconds(1), double.PositiveInfinity),
			new TelemetryChartPoint(start.AddSeconds(2), null) });
		Assert.That(chart.Points.Select(point => point.Value), Is.All.Null);
	}

	[TestCase(TelemetryChartUnit.Percent)]
	[TestCase(TelemetryChartUnit.Bytes)]
	[TestCase(TelemetryChartUnit.BytesPerSecond)]
	public void ExistingUnsignedMetricsContinueRejectingNegativeReadings(TelemetryChartUnit unit) {
		using var chart = new TelemetryChart { Unit = unit };
		chart.SetSamples(new[] { new TelemetryChartPoint(DateTime.UtcNow, -1) });
		Assert.That(chart.Points.Single().Value, Is.Null);
	}
}
