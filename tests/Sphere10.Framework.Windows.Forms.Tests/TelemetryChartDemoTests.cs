// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class TelemetryChartDemoTests {
	[TestCase(820, 680)]
	[TestCase(1480, 950)]
	public void DashboardRendersAllUnitsAndRetainsUsablePlotGeometry(int width, int height) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, height);
		using var screen = new TelemetryChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		await Task.Yield();
		var charts = GetCharts(screen);
		Assert.That(charts.Select(chart => chart.Unit), Is.EquivalentTo(new[] {
			TelemetryChartUnit.Percent, TelemetryChartUnit.Bytes, TelemetryChartUnit.BytesPerSecond, TelemetryChartUnit.Celsius
		}));
		Assert.That(charts.All(chart => chart.Points.Length == 80), Is.True);
		Assert.That(charts.Single(chart => chart.Unit == TelemetryChartUnit.BytesPerSecond).Compact, Is.True);
		Assert.That(charts.Any(chart => !chart.Compact && chart.ShowHeader), Is.True);
		Assert.That(charts.Select(chart => chart.PlotStyle), Does.Contain(TelemetryChartStyle.Line).And.Contain(TelemetryChartStyle.Area));
		Assert.That(charts.Single(chart => chart.Unit == TelemetryChartUnit.Celsius).Points.Any(point => point.Value < 0), Is.True);
		Assert.That(charts.Single(chart => chart.Unit == TelemetryChartUnit.Percent).Points.All(point => point.Value == null || point.Value is >= 0 and <= 100), Is.True);
		Assert.That(screen.Controls.Find("telemetryIntroduction", true).Single().Text, Does.Contain("SIMULATED"));
		screen.PerformLayout();
		using var image = new Bitmap(screen.Width, screen.Height);
		screen.DrawToBitmap(image, screen.ClientRectangle);
		foreach (var chart in charts) {
			var plot = chart.Controls.OfType<GraphChart>().Single();
			Assert.That(chart.Parent.ClientRectangle.Contains(chart.Bounds), Is.True, chart.Name + " Must Fit Inside Its Card");
			Assert.That(plot.PlotBounds.Width, Is.GreaterThan(180), chart.Name + " Plot Width");
			Assert.That(plot.PlotBounds.Height, Is.GreaterThan(70), chart.Name + " Plot Height");
			Assert.That(plot.Series.Single().Points.Any(point => point.Y.HasValue), Is.True);
		}
		var cards = charts.Select(chart => chart.Parent).ToArray();
		Assert.That(cards.Select(card => card.Left).Distinct().Count(), Is.EqualTo(2));
		Assert.That(cards.Select(card => card.Top).Distinct().Count(), Is.EqualTo(2));
		var output = Environment.GetEnvironmentVariable("SPHERE10_CHART_VALIDATION_DIRECTORY");
		if (!string.IsNullOrEmpty(output)) {
			Directory.CreateDirectory(output);
			image.Save(Path.Combine(output, $"TelemetryDashboard-{width}x{height}.png"), ImageFormat.Png);
		}
	});

	[Test]
	public void CaptureCommandsPreserveTimestampsBoundHistoryAndShowEmptyState() => RunWithMessageLoop(owner => {
		owner.ClientSize = new Size(1100, 800);
		using var screen = new TelemetryChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		var charts = GetCharts(screen);
		var tools = screen.Controls.Find("telemetryCommands", true).OfType<ToolStrip>().Single();
		var intervals = tools.Items.OfType<ToolStripComboBox>().Single();
		ToolStripButton Button(string name) => tools.Items.OfType<ToolStripButton>().Single(button => button.Name == name);
		Assert.That(intervals.DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
		Assert.That(charts.All(chart => chart.Points.Any(point => point.Value == null)), Is.True);
		var timestamps = charts[0].Points.Select(point => point.CapturedAtUtc).ToArray();
		Button("telemetryGaps").PerformClick();
		Assert.That(charts.All(chart => chart.Points.All(point => point.Value.HasValue)), Is.True);
		Assert.That(charts[0].Points.Select(point => point.CapturedAtUtc), Is.EqualTo(timestamps));
		Button("telemetryGaps").PerformClick();
		Assert.That(charts.All(chart => chart.Points.Any(point => point.Value == null)), Is.True);
		for (var index = 0; index < 115; index++)
			Button("telemetryAddSample").PerformClick();
		Assert.That(charts.All(chart => chart.Points.Length == 180), Is.True);
		Assert.That(charts[0].Points[0].CapturedAtUtc, Is.GreaterThan(timestamps[0]));
		Assert.That(charts[0].Points.Zip(charts[0].Points.Skip(1)).All(pair => pair.Item1.CapturedAtUtc < pair.Item2.CapturedAtUtc), Is.True);
		Button("telemetryResetCapture").PerformClick();
		Assert.That(charts.All(chart => chart.Points.Length == 0), Is.True);
		Assert.That(screen.Controls.Find("telemetryStatus", true).Single().Text, Does.Contain("0 Since Reset"));
		foreach (var chart in charts) {
			var empty = chart.Controls.Find("chartEmptyState", true).Single();
			Assert.That(empty.Visible, Is.True);
			Assert.That(empty.Text, Is.EqualTo("NO DATA"));
		}
		Button("telemetryAddSample").PerformClick();
		Assert.That(charts.All(chart => chart.Points.Length == 1), Is.True);
		Assert.That(charts.All(chart => chart.Points[0].Value.HasValue), Is.True);
		Button("telemetryClear").PerformClick();
		Assert.That(charts.All(chart => chart.Points.Length == 0), Is.True);
		Assert.That(Button("telemetryLive").Checked, Is.False);
		Assert.That(screen.Controls.Find("telemetryStatus", true).Single().Text, Does.Contain("1 Since Reset"));
		return Task.CompletedTask;
	});

	[Test]
	public void LiveCaptureRespectsSelectedCadenceVisibilityResetAndDisposal() => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(1100, 800);
		using var screen = new TelemetryChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		var charts = GetCharts(screen);
		var tools = screen.Controls.Find("telemetryCommands", true).OfType<ToolStrip>().Single();
		var live = tools.Items.OfType<ToolStripButton>().Single(button => button.Name == "telemetryLive");
		var intervals = tools.Items.OfType<ToolStripComboBox>().Single();
		intervals.SelectedIndex = 0;
		live.PerformClick();
		await WaitUntilAsync(() => charts[0].Points.Length > 80);
		Assert.That(screen.Controls.Find("telemetryStatus", true).Single().Text, Does.Contain("250 ms"));
		screen.Visible = false;
		var hiddenTimes = charts[0].Points.Select(point => point.CapturedAtUtc).ToArray();
		await Task.Delay(650);
		Assert.That(charts[0].Points.Select(point => point.CapturedAtUtc), Is.EqualTo(hiddenTimes));
		Assert.That(live.Checked, Is.True, "Hiding Must Preserve The User's Live Selection");
		screen.Visible = true;
		await WaitUntilAsync(() => charts[0].Points.Length > hiddenTimes.Length);
		tools.Items.OfType<ToolStripButton>().Single(button => button.Name == "telemetryResetCapture").PerformClick();
		Assert.That(charts.All(chart => chart.Points.Length == 0), Is.True);
		Assert.That(live.Checked, Is.True);
		await WaitUntilAsync(() => charts[0].Points.Length > 0);
		screen.Dispose();
		var disposedTimes = charts[0].Points.Select(point => point.CapturedAtUtc).ToArray();
		await Task.Delay(650);
		Assert.That(charts[0].Points.Select(point => point.CapturedAtUtc), Is.EqualTo(disposedTimes));
		Assert.That(screen.IsDisposed, Is.True);
	});

	private static TelemetryChart[] GetCharts(Control screen) => new[] {
		"telemetryCpuChart", "telemetryMemoryChart", "telemetryThroughputChart", "telemetryTemperatureChart"
	}.Select(name => screen.Controls.Find(name, true).OfType<TelemetryChart>().Single()).ToArray();

	private static async Task WaitUntilAsync(Func<bool> predicate) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!predicate() && DateTime.UtcNow < deadline)
			await Task.Delay(30);
		Assert.That(predicate(), Is.True, "The Visible Telemetry Dashboard Did Not Publish A Sample");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		var completed = false;
		watchdog.Tick += (_, _) => { failure = new AssertionException("The Telemetry Dashboard Test Timed Out"); owner.Close(); };
		owner.Shown += async (_, _) => {
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
			watchdog.Stop();
			owner.Close();
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
