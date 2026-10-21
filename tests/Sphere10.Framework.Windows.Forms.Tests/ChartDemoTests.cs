// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartDemoTests {
	[TestCase(900, 700)]
	[TestCase(1600, 1000)]
	public void FourChartDashboardDrawsEveryScenarioAtCompactAndLargeSizes(int width, int height) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, height);
		using var screen = new ChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		Assert.That(screen.Title, Is.EqualTo("Graph And Bar Charts"));
		var charts = Charts(screen);
		Assert.That(charts.Select(chart => chart.Style), Is.EqualTo(new[] { GraphChartStyle.Line, GraphChartStyle.Area, GraphChartStyle.Column, GraphChartStyle.Bar }));
		Assert.That(Combo(screen, "chartScenario").DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
		Assert.That(Combo(screen, "chartInterval").DropDownStyle, Is.EqualTo(ComboBoxStyle.DropDownList));
		foreach (var scenario in Combo(screen, "chartScenario").Items.Cast<string>()) {
			screen.SelectScenario(scenario);
			await Task.Yield();
			screen.PerformLayout();
			using var image = new Bitmap(screen.Width, screen.Height);
			screen.DrawToBitmap(image, screen.ClientRectangle);
			foreach (var chart in charts) {
				Assert.That(chart.Series, Has.Length.EqualTo(3), scenario + " must demonstrate multiple series in every style.");
				Assert.That(chart.Parent.ClientRectangle.Contains(chart.Bounds), Is.True);
				Assert.That(chart.PlotBounds.Width, Is.GreaterThan(140), chart.Name + " must retain a useful plot width.");
				Assert.That(chart.PlotBounds.Height, Is.GreaterThan(75), chart.Name + " must retain a useful plot height.");
				Assert.That(chart.NativeChart.Series.SelectMany(series => series.Points).Any(point => !point.IsEmpty), Is.True);
			}
			var output = Environment.GetEnvironmentVariable("SPHERE10_CHART_VALIDATION_DIRECTORY");
			if (!string.IsNullOrEmpty(output)) {
				Directory.CreateDirectory(output);
				image.Save(Path.Combine(output, $"ChartDemo-{scenario.Replace(' ', '-')}-{width}x{height}.png"), ImageFormat.Png);
			}
		}
	});

	[TestCase("Service Activity", "Requests / Minute", "N0", 0, 220)]
	[TestCase("Quarterly Revenue", "Amount (USD)", "C0", 0, 160000)]
	[TestCase("Signed Cash Flow", "Amount (USD)", "C0", -20000, 30000)]
	[TestCase("Missing Observations", "Rainfall (mm)", "0.0", 0, 25)]
	public void ScenariosShareHistoryAndGroupedComparisonsWithAppropriateUnits(string scenario, string units, string format, double minimum, double maximum)
		=> RunWithMessageLoop(owner => {
			using var screen = new ChartTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			screen.SelectScenario(scenario);
			var charts = Charts(screen);
			Assert.That(screen.SelectedScenario, Is.EqualTo(scenario));
			foreach (var chart in charts) {
				Assert.That(chart.YAxisTitle, Is.EqualTo(units));
				Assert.That(chart.YValueFormat, Is.EqualTo(format));
				Assert.That(chart.MinimumY, Is.EqualTo(minimum));
				Assert.That(chart.MaximumY, Is.EqualTo(maximum));
				Assert.That(chart.ShowLegend, Is.True);
				Assert.That(chart.ShowToolTips, Is.True);
			}
			Assert.That(charts[0].Series.All(series => series.Points.Length == ChartTestScreen.InitialSamples), Is.True);
			Assert.That(charts[1].Series.SelectMany(series => series.Points).Select(point => point.Y),
				Is.EqualTo(charts[0].Series.SelectMany(series => series.Points).Select(point => point.Y)));
			Assert.That(charts[2].Series.All(series => series.Points.Length == 6), Is.True);
			Assert.That(charts[2].Series.SelectMany(series => series.Points).Select(point => point.Label), Has.All.Not.Null.And.Not.Empty);
			Assert.That(charts[3].Series.SelectMany(series => series.Points), Is.EqualTo(charts[2].Series.SelectMany(series => series.Points)));
			if (scenario == "Signed Cash Flow") {
				var values = charts[0].Series.SelectMany(series => series.Points).Where(point => point.Y.HasValue).Select(point => point.Y.Value).ToArray();
				Assert.That(values.Any(value => value < 0), Is.True);
				Assert.That(values.Any(value => value > 0), Is.True);
				Assert.That(values, Does.Contain(0D));
			}
			return Task.CompletedTask;
		});

	[Test]
	public void MissingIntervalsAndMeasuredZerosRemainDifferentInTheNativePlots() => RunWithMessageLoop(owner => {
		using var screen = new ChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		screen.SelectScenario("Missing Observations");
		var line = Charts(screen)[0];
		Assert.That(line.Series.SelectMany(series => series.Points).Any(point => point.Y == null), Is.True);
		Assert.That(line.Series.SelectMany(series => series.Points).Any(point => point.Y == 0), Is.True);
		Assert.That(line.NativeChart.Series.Count, Is.GreaterThan(line.Series.Length), "Null observations must split the rendered lines into separate segments.");
		Assert.That(line.NativeChart.Series.SelectMany(series => series.Points).Any(point => !point.IsEmpty && point.YValues[0] == 0), Is.True);
		Button(screen, "Missing Samples").PerformClick();
		Assert.That(line.Series.SelectMany(series => series.Points).All(point => point.Y.HasValue), Is.True);
		Button(screen, "Missing Samples").PerformClick();
		Assert.That(line.Series.SelectMany(series => series.Points).Any(point => !point.Y.HasValue), Is.True);
		return Task.CompletedTask;
	});

	[Test]
	public void PresentationToolsControlNativeLegendsLabelsTooltipsAndAxisBounds() => RunWithMessageLoop(owner => {
		using var screen = new ChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		screen.SelectScenario("Signed Cash Flow");
		Button(screen, "Legends").PerformClick();
		Button(screen, "Value Labels").PerformClick();
		Button(screen, "Tooltips").PerformClick();
		Button(screen, "Fixed Range").PerformClick();
		foreach (var chart in Charts(screen)) {
			Assert.That(chart.ShowLegend, Is.False);
			Assert.That(chart.NativeChart.Legends[0].Enabled, Is.False);
			Assert.That(chart.ShowToolTips, Is.False);
			Assert.That(chart.NativeChart.Series.SelectMany(series => series.Points).All(point => string.IsNullOrEmpty(point.ToolTip)), Is.True);
			Assert.That(chart.MinimumY, Is.Null);
			Assert.That(chart.MaximumY, Is.Null);
			Assert.That(chart.NativeChart.Series.All(series => series.IsValueShownAsLabel == (chart is BarChart)), Is.True);
		}
		Button(screen, "Fixed Range").PerformClick();
		screen.SelectScenario("Quarterly Revenue");
		Assert.That(Charts(screen).All(chart => chart.MinimumY == 0 && chart.MaximumY == 160000), Is.True);
		return Task.CompletedTask;
	});

	[Test]
	public void CaptureCommandsRetainBoundedHistoryAndClearAllFourEmptyStates() => RunWithMessageLoop(owner => {
		using var screen = new ChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		var charts = Charts(screen);
		var native = charts[0].NativeChart;
		for (var index = 0; index < 130; index++)
			Button(screen, "Add Sample").PerformClick();
		Assert.That(charts[0].Series.All(series => series.Points.Length == ChartTestScreen.SampleCapacity), Is.True);
		Assert.That(charts[0].Series[0].Points[0].X, Is.EqualTo(58));
		Assert.That(charts[0].Series[0].Points[^1].X, Is.EqualTo(177));
		Assert.That(charts[0].NativeChart, Is.SameAs(native));
		Button(screen, "Clear").PerformClick();
		foreach (var chart in charts) {
			Assert.That(chart.Series, Is.Empty);
			Assert.That(chart.Controls.Find("chartEmptyState", true).Single().Visible, Is.True);
		}
		Button(screen, "Add Sample").PerformClick();
		Assert.That(charts[0].Series[0].Points, Has.Length.EqualTo(1));
		Button(screen, "Reset Capture").PerformClick();
		Assert.That(charts[0].Series[0].Points, Has.Length.EqualTo(ChartTestScreen.InitialSamples));
		Assert.That(charts[0].Series[0].Points[0].X, Is.Zero);
		return Task.CompletedTask;
	});

	[Test]
	public void LiveIntervalPausesWhenHiddenAndReleasesItsTimerOnDisposal() => RunWithMessageLoop(async owner => {
		using var screen = new ChartTestScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		Combo(screen, "chartInterval").SelectedIndex = 0;
		var timer = (System.Windows.Forms.Timer)typeof(ChartTestScreen).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(screen);
		Assert.That(timer.Interval, Is.EqualTo(250));
		Button(screen, "Live").PerformClick();
		await WaitForAsync(() => Charts(screen)[0].Series[0].Points.Length > ChartTestScreen.InitialSamples);
		screen.Visible = false;
		var last = Charts(screen)[0].Series[0].Points[^1].X;
		Assert.That(timer.Enabled, Is.False);
		await Task.Delay(550);
		Assert.That(Charts(screen)[0].Series[0].Points[^1].X, Is.EqualTo(last));
		screen.Visible = true;
		await WaitForAsync(() => Charts(screen)[0].Series[0].Points[^1].X > last);
		var disposed = false;
		timer.Disposed += (_, _) => disposed = true;
		screen.Dispose();
		Assert.That(disposed, Is.True);
		Assert.That(screen.IsDisposed, Is.True);
	});

	private static GraphChart[] Charts(Control screen) => new[] { "demoLineChart", "demoAreaChart", "demoColumnChart", "demoBarChart" }
		.Select(name => screen.Controls.Find(name, true).OfType<GraphChart>().Single()).ToArray();

	private static ToolStripComboBox Combo(Control screen, string name) => screen.Controls.Find("chartCommands", true).OfType<ToolStrip>().Single()
		.Items.OfType<ToolStripComboBox>().Single(combo => combo.Name == name);

	private static ToolStripButton Button(Control screen, string text) => new[] { "chartCommands", "chartOptions" }
		.SelectMany(name => screen.Controls.Find(name, true).OfType<ToolStrip>()).SelectMany(strip => strip.Items.OfType<ToolStripButton>()).Single(button => button.Text == text);

	private static async Task WaitForAsync(Func<bool> completed) {
		var watch = Stopwatch.StartNew();
		while (!completed() && watch.Elapsed < TimeSpan.FromSeconds(5))
			await Task.Delay(10);
		Assert.That(completed(), Is.True, "The demo's live timer did not publish its next sample.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ClientSize = new Size(1200, 900), ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		var completed = false;
		watchdog.Tick += (_, _) => { failure = new AssertionException("The chart demo timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(() => { watchdog.Stop(); owner.Close(); });
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => { await test(owner); completed = true; });
			if (error != null)
				failure = error;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
