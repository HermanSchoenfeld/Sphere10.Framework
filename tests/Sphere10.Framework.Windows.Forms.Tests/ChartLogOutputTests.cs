// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartLogOutputTests {
	[Test]
	public void FirstLoggingAccessRejectsAWorkerBeforeCreatingNativeOutput() => RunWithMessageLoop(async owner => {
		using var chart = new GraphChart();
		await Assert.ThatAsync(async () => await Task.Run(() => { _ = chart.LogOutputBehavior; }), Throws.TypeOf<InvalidOperationException>());
		owner.Controls.Add(chart);
		Assert.That(chart.LogOutput.IsLogging, Is.False, "A rejected worker access must leave UI-owned logging usable.");
		await chart.LogOutputBehavior.DisposeAsync();
	});

	[Test]
	public void RememberedApplicationOutputWaitsForThePreloadedScreensActualHostAndLogger() => RunWithMessageLoop(async owner => {
		var startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (startedFramework)
			Sphere10Framework.Instance.Build().Start();
		using var framework = Tools.Scope.ExecuteOnDispose(() => { if (startedFramework) Sphere10Framework.Instance.EndFramework(); });
		ISettingsProvider previousProvider = null;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => previousProvider = UserSettings.Provider, out _);
		using var restoreProvider = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = previousProvider);
		var directory = Path.Combine(Path.GetTempPath(), "ChartHostRestore-" + Guid.NewGuid().ToString("N"));
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(directory));
		var preferences = UserSettings.Provider.Get<LogOutputSelectorSettings>("late-chart");
		preferences.HasConfiguration = true;
		preferences.IsLogging = true;
		preferences.Configuration = ApplicationConfiguration();
		preferences.Configuration.RememberAfterClose = true;
		preferences.Save();

		// Let the real child Load run before the owning screen has joined an application host.
		using var screen = new PreloadedChartScreen();
		var behavior = screen.Chart.LogOutputBehavior;
		var selector = screen.Chart.LogOutput;
		selector.SettingsKey = "late-chart";
		owner.Controls.Add(screen);
		Assert.That(screen.Chart.IsHandleCreated, Is.True);
		Assert.That(selector.HasConfiguration, Is.False, "An early child Load must not consume remembered application output before attachment.");
		Assert.That(selector.IsLogging, Is.False);
		owner.Controls.Remove(screen);

		// The native host supplies this existing control and invokes its actual display lifecycle.
		using var applicationForm = new BlockMainForm();
		var host = applicationForm.ScreenHost;
		await using var application = new WinFormsApplication(applicationForm, Array.Empty<IWinFormsApplicationBlock>());
		var logger = new RecordingLogger();
		application.Logger = logger;
		using var block = new WinFormsApplicationBlockBuilder()
			.WithName("Charts")
			.ConfigureMenu(menu => menu
				.WithText("Charts")
				.AddScreenItem<PreloadedChartScreen>("Chart")
			)
			.Build();
		block.Parent = application;
		owner.Controls.Add(host);
		screen.Block = block;
		Assert.That(host.ShowScreen(screen), Is.True);
		Assert.That(selector.ApplicationLogger, Is.SameAs(logger));
		Assert.That(selector.ScreenHost, Is.SameAs(host));
		Assert.That(selector.IsLogging, Is.True, "ScreenDisplayed must retry the deferred preference after native host attachment.");
		screen.Chart.SetSeries(new[] { new GraphChartSeries("S", new[] { new GraphChartPoint(1, 2) }) });
		await behavior.FlushAsync();
		Assert.That(logger.Messages, Has.Count.EqualTo(1));
		Assert.That(await host.CloseScreenAsync(screen), Is.True);
		await behavior.Completion;
		Assert.That(selector.IsDisposed, Is.True);
	});
	[Test]
	public void GraphSnapshotsLogOnlyNewAndRevisedObservationsOnTheWorker() => RunWithMessageLoop(async owner => {
		using var chart = new GraphChart();
		owner.Controls.Add(chart);
		var logger = new RecordingLogger();
		chart.LogOutput.ApplicationLogger = logger;
		chart.LogOutput.StartLogging(ApplicationConfiguration());
		var uiThread = Environment.CurrentManagedThreadId;
		chart.SetSeries(new[] { new GraphChartSeries("Temperature", new[] { new GraphChartPoint(1, 20), new GraphChartPoint(2, 21) }) });
		chart.SetSeries(chart.Series);
		chart.SetSeries(new[] { new GraphChartSeries("Temperature", new[] { new GraphChartPoint(1, 20), new GraphChartPoint(2, 21), new GraphChartPoint(3, 22) }) });
		chart.SetSeries(new[] { new GraphChartSeries("Temperature", new[] { new GraphChartPoint(1, 20), new GraphChartPoint(2, 21), new GraphChartPoint(3, 23) }) });
		await chart.LogOutputBehavior.FlushAsync();
		Assert.That(logger.Messages, Has.Count.EqualTo(4));
		Assert.That(logger.Messages.Last(), Does.Contain("23"));
		Assert.That(logger.Threads, Has.All.Not.EqualTo(uiThread), "Output IO must not run on the rendering thread.");
		var completion = chart.LogOutputBehavior;
		chart.Dispose();
		await completion.Completion;
		Assert.That(completion.LastError, Is.Null);
	});

	[TestCase(true)]
	[TestCase(false)]
	public void RelatedSeriesShareOneTimestampAndRevisionReemitsTheCompleteSample(bool capturedTime) => RunWithMessageLoop(async owner => {
		using var surface = new Panel();
		owner.Controls.Add(surface);
		var time = new DateTime(2026, 10, 22, 1, 2, 3, DateTimeKind.Utc);
		var samples = new[] {
			new ChartLogSample("Temperature", capturedTime ? "temperature/first" : "first", "first", new[] { "51.25" }) { CapturedAtUtc = capturedTime ? time : null, Units = "°C" },
			new ChartLogSample("Power", capturedTime ? "power/first" : "first", "first", new[] { "0" }) { CapturedAtUtc = capturedTime ? time : null, Units = "W" },
			new ChartLogSample("Fan", capturedTime ? "fan/first" : "first", "first", new[] { "Unavailable" }) { CapturedAtUtc = capturedTime ? time : null, Units = "RPM" }
		};
		await using var behavior = new ChartLogOutputBehavior(surface, () => samples, () => "GPU");
		var logger = new RecordingLogger();
		behavior.OutputSelector.ApplicationLogger = logger;
		behavior.OutputSelector.StartLogging(ApplicationConfiguration());
		behavior.Capture();
		await behavior.FlushAsync();
		Assert.That(logger.Messages, Has.Count.EqualTo(1));
		var position = capturedTime ? string.Empty : " (X: first)";
		Assert.That(logger.Messages.Single(), Does.EndWith($"GPU — Temperature: 51.25 °C{position}; Power: 0 W{position}; Fan: Unavailable{position}"));
		if (capturedTime)
			Assert.That(logger.Messages.Single(), Does.StartWith("2026-10-22 01:02:03.000 "));
		samples[1] = samples[1] with { Values = new[] { "12.5" } };
		behavior.Capture();
		behavior.Capture();
		await behavior.FlushAsync();
		Assert.That(logger.Messages, Has.Count.EqualTo(2));
		Assert.That(logger.Messages.Last(), Does.EndWith($"GPU — Temperature: 51.25 °C{position}; Power: 12.5 W{position}; Fan: Unavailable{position}"));
		Assert.That(behavior.RetainedSampleCount, Is.EqualTo(3), "Revision identities remain individual and bounded by the current snapshot.");
	});

	[Test]
	public void TelemetryUsesCapturedTimestampsWhileItsRelativeViewportMoves() => RunWithMessageLoop(async owner => {
		using var chart = new TelemetryChart { ChartTitle = "CPU" };
		owner.Controls.Add(chart);
		var logger = new RecordingLogger();
		chart.LogOutput.ApplicationLogger = logger;
		chart.LogOutput.StartLogging(ApplicationConfiguration());
		var first = new DateTime(2026, 10, 21, 1, 2, 3, DateTimeKind.Utc);
		chart.SetSamples(new[] { new TelemetryChartPoint(first, 10), new TelemetryChartPoint(first.AddSeconds(1), 11) });
		chart.SetSamples(new[] { new TelemetryChartPoint(first, 10), new TelemetryChartPoint(first.AddSeconds(1), 11), new TelemetryChartPoint(first.AddSeconds(2), 12) });
		chart.SetSamples(chart.Points);
		await chart.LogOutputBehavior.FlushAsync();
		Assert.That(logger.Messages, Has.Count.EqualTo(3), "A newer Now position must not republish the retained history.");
		Assert.That(logger.Messages.First(), Is.EqualTo("2026-10-21 01:02:03.000 CPU — CPU: 10 %"));
		await chart.LogOutputBehavior.DisposeAsync();
	});

	[Test]
	public void HiddenChartHeadingUsesItsScreenAndMeasurementContextWithOneUtcCaptureTime() => RunWithMessageLoop(async owner => {
		var previousCulture = CultureInfo.CurrentCulture;
		using var culture = Tools.Scope.ExecuteOnDispose(() => CultureInfo.CurrentCulture = previousCulture);
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
		using var screen = new WinFormsApplicationScreen { Title = "Memory Activity" };
		using var group = new GroupBox { Text = "Memory Usage", Dock = DockStyle.Fill };
		using var chart = new TelemetryChart { ShowHeader = false, Unit = TelemetryChartUnit.Percent, Dock = DockStyle.Fill };
		group.Controls.Add(chart);
		screen.Controls.Add(group);
		owner.Controls.Add(screen);
		var logger = new RecordingLogger();
		chart.LogOutput.ApplicationLogger = logger;
		var configuration = ApplicationConfiguration();
		configuration.TimestampEnabled = true;
		chart.LogOutput.StartLogging(configuration);
		var captured = new DateTime(2026, 10, 22, 1, 2, 3, 456, DateTimeKind.Utc);
		chart.SetSamples(new[] { new TelemetryChartPoint(captured, 42.5) });
		await chart.LogOutputBehavior.FlushAsync();
		Assert.That(logger.Messages.Single(), Is.EqualTo("2026-10-22 01:02:03.456 Memory Activity — Memory Usage: 42.5 %"),
			"The original UTC observation, chart title and units must remain descriptive even with a hidden default Metric heading and enabled output timestamps.");
		Assert.That(chart.LogOutput.Configuration.TimestampEnabled, Is.True, "Chart capture must not rewrite an explicitly accepted timestamp preference.");
		await chart.LogOutputBehavior.DisposeAsync();
	});

	[TestCase(false)]
	[TestCase(true)]
	public void SlidingSnapshotsRetainOnlyCurrentLogIdentitiesAndReleaseHistoryOnClearAndDispose(bool telemetry) => RunWithMessageLoop(async owner => {
		using Control surface = telemetry ? new TelemetryChart() : new GraphChart();
		owner.Controls.Add(surface);
		var graph = telemetry ? surface.Controls.OfType<GraphChart>().Single() : (GraphChart)surface;
		var behavior = graph.LogOutputBehavior;
		graph.LogOutput.ApplicationLogger = new RecordingLogger();
		graph.LogOutput.StartLogging(ApplicationConfiguration());
		var first = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc);
		void Publish(int start, int count) {
			if (telemetry)
				((TelemetryChart)surface).SetSamples(Enumerable.Range(start, count).Select(index => new TelemetryChartPoint(first.AddSeconds(index), index)).ToArray());
			else
				graph.SetSeries(count == 0 ? Array.Empty<GraphChartSeries>()
					: new[] { new GraphChartSeries("Reading", Enumerable.Range(start, count).Select(index => new GraphChartPoint(index, index)).ToArray()) });
		}
		for (var frame = 0; frame < 200; frame++)
			Publish(frame, 4);
		await behavior.FlushAsync();
		Assert.That(behavior.RetainedSampleCount, Is.EqualTo(4), "Deduplication must not accumulate identities from earlier windows.");
		Publish(0, 0);
		Assert.That(behavior.RetainedSampleCount, Is.Zero);
		Publish(1000, 4);
		surface.Dispose();
		await behavior.Completion;
		Assert.That(behavior.RetainedSampleCount, Is.Zero);
		Assert.That(graph.Series, Is.Empty, "Disposed native surfaces release their owned captured series.");
		if (telemetry)
			Assert.That(((TelemetryChart)surface).Points, Is.Empty, "Disposed telemetry surfaces release their captured time/value records.");
	});

	[Test]
	public void SharedRawOutputCanConfigureBothChartKindsWithoutDuplicateHistory() => RunWithMessageLoop(async owner => {
		using var selector = new LogOutputSelectorControl();
		var logger = new RecordingLogger();
		selector.ApplicationLogger = logger;
		using var telemetry = new TelemetryChart();
		using var bars = new BarChart();
		owner.Controls.Add(telemetry);
		owner.Controls.Add(bars);
		foreach (var behavior in new[] { telemetry.LogOutputBehavior, bars.LogOutputBehavior }) {
			behavior.OutputSelector = selector;
			behavior.CaptureEnabled = false;
		}
		selector.StartLogging(ApplicationConfiguration());
		telemetry.SetSamples(new[] { new TelemetryChartPoint(DateTime.UtcNow, 12) });
		bars.SetSeries(new[] { new GraphChartSeries("CPU", new[] { new GraphChartPoint(1, 12) }) });
		selector.Logger.Info("raw sample");
		await telemetry.LogOutputBehavior.FlushAsync();
		await bars.LogOutputBehavior.FlushAsync();
		Assert.That(logger.Messages, Is.EqualTo(new[] { "raw sample" }));
		await telemetry.LogOutputBehavior.DisposeAsync();
		await bars.LogOutputBehavior.DisposeAsync();
		Assert.That(selector.IsLogging, Is.True, "A borrowed selector outlives each chart.");
		Assert.That(selector.IsDisposed, Is.False);
	});

	[Test]
	public void NativeAdapterLogsAllYValuesAndExtendsExistingAndLaterMenus() => RunWithMessageLoop(async owner => {
		using var chart = new Chart();
		using var original = new ContextMenuStrip();
		original.Items.Add("Existing action");
		chart.ContextMenuStrip = original;
		using var behavior = new ChartLogOutputBehavior(chart);
		owner.Controls.Add(chart);
		Assert.That(original.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] { "Existing action", "Log Output" }));
		using var replacement = new ContextMenuStrip();
		replacement.Items.Add("Replacement action");
		chart.ContextMenuStrip = replacement;
		Assert.That(replacement.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] { "Replacement action", "Log Output" }));
		var logger = new RecordingLogger();
		behavior.OutputSelector.ApplicationLogger = logger;
		var series = new Series("Range");
		chart.ChartAreas.Add(new ChartArea("Default"));
		chart.Titles.Add(new Title("Pressure Measurements"));
		chart.ChartAreas[0].AxisY.Title = "Pressure (kPa)";
		chart.ChartAreas[0].AxisX.Title = "Station";
		series.Points.Add(new DataPoint(1, new[] { 10D, 20D }) { AxisLabel = "Inlet" });
		chart.Series.Add(series);
		behavior.OutputSelector.StartLogging(ApplicationConfiguration());
		behavior.Capture();
		series.Points[0].YValues = new[] { 11D, 21D };
		behavior.Capture();
		await behavior.FlushAsync();
		Assert.That(logger.Messages, Has.Count.EqualTo(2));
		Assert.That(logger.Messages.Last(), Does.Match(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} Pressure Measurements — Range: 11 kPa / 21 kPa \(Station: Inlet\)$"));
		await behavior.DisposeAsync();
		Assert.That(chart.ContextMenuStrip, Is.SameAs(replacement));
		Assert.That(replacement.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] { "Replacement action" }));
		Assert.That(original.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] { "Existing action" }));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ChartMenuChangesPreserveCallerCommandsAcrossThePlotAndEmptyState(bool telemetry) => RunWithMessageLoop(async owner => {
		using Control chart = telemetry ? new TelemetryChart() : new GraphChart();
		owner.Controls.Add(chart);
		var plot = telemetry ? chart.Controls.OfType<GraphChart>().Single() : (GraphChart)chart;
		using var custom = new ContextMenuStrip();
		custom.Items.Add("Inspect");
		chart.ContextMenuStrip = custom;
		Assert.That(custom.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] { "Inspect", "Log Output" }));
		Assert.That(plot.NativeChart.ContextMenuStrip, Is.SameAs(custom));
		Assert.That(chart.Controls.Find("chartEmptyState", true).Single().ContextMenuStrip, Is.SameAs(custom));
		chart.ContextMenuStrip = null;
		Assert.That(chart.ContextMenuStrip.Items.Cast<ToolStripItem>().Select(item => item.Text), Is.EqualTo(new[] { "Log Output" }));
		Assert.That(plot.NativeChart.ContextMenuStrip, Is.SameAs(chart.ContextMenuStrip));
		await plot.LogOutputBehavior.DisposeAsync();
	});

	[TestCase(false)]
	[TestCase(true)]
	public void SlowOutputHasABoundedQueueAndDisposalAwaitsAcceptedWritesWithoutBlockingUi(bool explicitlyStop) => RunWithMessageLoop(async owner => {
		using var surface = new Panel();
		owner.Controls.Add(surface);
		var samples = new[] { new ChartLogSample("S", "first", "first", new[] { "1" }) };
		using var released = new ManualResetEventSlim();
		using var release = Tools.Scope.ExecuteOnDispose(released.Set);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var logger = new RecordingLogger(() => { entered.TrySetResult(); released.Wait(); });
		using var behavior = new ChartLogOutputBehavior(surface, () => samples);
		var selector = behavior.OutputSelector;
		selector.ApplicationLogger = logger;
		selector.StartLogging(ApplicationConfiguration());
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		samples = Enumerable.Range(0, 5000).Select(index => new ChartLogSample("S", index.ToString(), index.ToString(), new[] { "2" })).ToArray();
		behavior.Capture();
		Assert.That(behavior.DroppedSamples, Is.GreaterThan(0));
		var stopped = explicitlyStop ? selector.StopLoggingAsync() : Task.CompletedTask;
		behavior.Dispose();
		Assert.That(behavior.Completion.IsCompleted, Is.False, "Disposal must return to the UI while the borrowed sink is occupied.");
		released.Set();
		await stopped.WaitAsync(TimeSpan.FromSeconds(10));
		await behavior.Completion.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.That(logger.Messages.Count, Is.EqualTo(5001 - behavior.DroppedSamples), "Every accepted entry must drain before the owned output closes.");
		Assert.That(selector.IsDisposed, Is.True);
	});

	[TestCase("Graph", false, false)]
	[TestCase("Telemetry", false, false)]
	[TestCase("Native", false, false)]
	[TestCase("Graph", true, false)]
	[TestCase("Graph", false, true)]
	public void ChartScreenOutputNamesTheChartAndPreservesSuppliedSourceNames(string kind, bool borrowed, bool customSource) => RunWithMessageLoop(async owner => {
		using var host = new WinFormsApplicationScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView };
		owner.Controls.Add(host);
		var source = new WinFormsApplicationScreen { Title = "Diagnostics" };
		using Control chart = kind switch {
			"Telemetry" => new TelemetryChart { ShowHeader = false },
			"Native" => new Chart(),
			_ => new GraphChart()
		};
		source.Controls.Add(chart);
		Assert.That(host.ShowScreen(source), Is.True);
		var behavior = chart switch {
			TelemetryChart telemetry => telemetry.LogOutputBehavior,
			GraphChart graph => graph.LogOutputBehavior,
			Chart native => new ChartLogOutputBehavior(native),
			_ => throw new InvalidOperationException()
		};
		var selector = borrowed ? new LogOutputSelectorControl { ScreenTitle = "Provider Capture" } : behavior.OutputSelector;
		if (borrowed) {
			behavior.OutputSelector = selector;
			behavior.CaptureEnabled = false;
		}
		if (customSource)
			selector.ScreenTitle = "Custom Capture";
		// A chart's descriptive title can be supplied after its lazy selector was first accessed.
		if (chart is TelemetryChart telemetryChart)
			telemetryChart.LogTitle = "Memory Activity";
		else if (chart is GraphChart graphChart)
			graphChart.LogTitle = "Memory Activity";
		else
			((Chart)chart).Titles.Add("Memory Activity");
		selector.ScreenHost = host;
		selector.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Screen, TimestampEnabled = false });
		var output = host.OpenScreens.OfType<LogScreen>().Single();
		var expectedSource = borrowed ? "Provider Capture" : customSource ? "Custom Capture" : "Memory Activity";
		Assert.That(output.FindForm().Text, Is.EqualTo("Log Output - " + expectedSource));
		Assert.That(host.DockScreen(output), Is.True);
		Assert.That(host.TabControl.TabPages.Cast<TabPage>().Single(page => ReferenceEquals(page.Tag, output)).Text,
			Is.EqualTo("Log Output - " + expectedSource));
		await behavior.DisposeAsync();
		if (borrowed) {
			Assert.That(selector.IsDisposed, Is.False);
			Assert.That(selector.ScreenTitle, Is.EqualTo("Provider Capture"));
			await selector.DisposeAsync();
		}
	});

	private static LogOutputConfiguration ApplicationConfiguration() => new() {
		Destination = LogOutputDestination.Application,
		TimestampEnabled = false,
		Options = LogOptions.InfoEnabled
	};

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 25000 };
		var completed = false;
		watchdog.Tick += (_, _) => { failure = new AssertionException("Chart logging test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(() => { watchdog.Stop(); owner.Close(); });
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class RecordingLogger : LoggerBase {
		private readonly Action _beforeWrite;

		public RecordingLogger(Action beforeWrite = null) {
			_beforeWrite = beforeWrite;
			Options = LogOptions.InfoEnabled;
		}

		public ConcurrentQueue<string> Messages { get; } = new();
		public ConcurrentQueue<int> Threads { get; } = new();

		protected override void Log(LogLevel level, string message) {
			_beforeWrite?.Invoke();
			Messages.Enqueue(message);
			Threads.Enqueue(Environment.CurrentManagedThreadId);
		}
	}

	public sealed class PreloadedChartScreen : WinFormsApplicationScreen {
		public PreloadedChartScreen() {
			Chart = new GraphChart { Name = "latePlot", Dock = DockStyle.Fill };
			Controls.Add(Chart);
		}

		public GraphChart Chart { get; }
	}

}
