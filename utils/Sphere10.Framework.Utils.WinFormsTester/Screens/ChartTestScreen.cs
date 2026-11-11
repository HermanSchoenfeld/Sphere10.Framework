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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Utils.WinFormsTester.Lifecycle;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class ChartTestScreen : ApplicationScreen {
	public const int SampleCapacity = 120;
	public const int InitialSamples = 48;
	private static readonly Color[] _seriesColors = { Color.FromArgb(34, 112, 177), Color.FromArgb(221, 117, 42), Color.FromArgb(36, 151, 126) };
	private readonly List<GraphChartPoint> _first = new();
	private readonly List<GraphChartPoint> _second = new();
	private readonly List<GraphChartPoint> _third = new();
	private int _sample;
	private bool _ready;
	private bool _initialized;
	private bool _finalized;
	private IDisposable _logOutputs;

	public ChartTestScreen() {
		InitializeComponent();
	}

	public string SelectedScenario => _scenario.SelectedItem as string;

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override ChartTestScreenSettings ScreenSettings => (ChartTestScreenSettings)base.ScreenSettings;

	public void SelectScenario(string scenario) {
		Guard.Argument(_scenario.Items.Contains(scenario), nameof(scenario), "Choose a scenario from the demonstration catalog.");
		_scenario.SelectedItem = scenario;
	}

	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		if (_initialized || _finalized || Tools.Runtime.IsDesignMode)
			return;
		_initialized = true;
		_logOutputs = DemoLogOutputLifetime.RegisterCurrent(this, _line.LogOutputBehavior, _area.LogOutputBehavior, _columns.LogOutputBehavior, _bars.LogOutputBehavior);
		_ready = true;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_interval.SelectedIndex = 1;
			_scenario.SelectedIndex = 0;
			if (HasSettings)
				ApplyScreenPreferences();
		}
		UpdateTimer();
	}

	protected override void OnShow() {
		base.OnShow();
		UpdateTimer();
	}

	protected override void OnDestroyScreen() {
		StopCapture();
		base.OnDestroyScreen();
	}

	protected override void OnVisibleChanged(EventArgs args) {
		base.OnVisibleChanged(args);
		UpdateTimer();
	}

	/// <summary>Applies accepted chart preferences without replacing another screen's values.</summary>
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (HasSettings && _ready && !IsDisposed && !Disposing)
			ApplyScreenPreferences();
	}

	private void StopCapture() {
		if (_finalized)
			return;
		_finalized = true;
		_logOutputs?.Dispose();
		_ready = false;
		_timer?.Stop();
		if (_timer != null)
			_timer.Tick -= TimerTick;
	}

	private void TimerTick(object sender, EventArgs args) => AddSample();

	private void AddSampleClicked(object sender, EventArgs args) => AddSample();

	private void ResetClicked(object sender, EventArgs args) => ResetSamples();

	private void LiveChanged(object sender, EventArgs args) => UpdateTimer();

	private void UpdateTimer() {
		if (!_finalized && _timer != null)
			_timer.Enabled = _ready && !IsDisposed && !Tools.Runtime.IsDesignMode && Visible && _live.Checked;
	}

	private void ScenarioChanged(object sender, EventArgs args) {
		if (_ready) {
			ResetSamples();
			SaveScreenPreferences();
		}
	}

	private void GapsChanged(object sender, EventArgs args) {
		if (_ready) {
			ResetSamples();
			SaveScreenPreferences();
		}
	}

	private void ResetSamples() {
		_first.Clear();
		_second.Clear();
		_third.Clear();
		_sample = 0;
		ConfigureScenario();
		ApplyPresentation();
		for (var index = 0; index < InitialSamples; index++)
			AddSample(false);
		RenderSamples();
	}

	private void AddSample(bool render = true) {
		var phase = _sample % 24;
		double first;
		double second;
		double third;
		switch (SelectedScenario) {
			case "Quarterly Revenue":
				var seasonal = new[] { 44, 57, 68, 91 }[_sample % 4];
				first = (seasonal + _sample / 4 % 12 * 3) * 1000;
				second = (seasonal * 0.72 + _sample / 4 % 12 * 2) * 1000;
				third = (seasonal * 0.44 + _sample / 4 % 12 * 1.5) * 1000;
				break;
			case "Signed Cash Flow":
				first = new[] { 12000, 18000, -4000, 0, 16000, 22000, -9000, 14000, 20000, -6000, 25000, 17000 }[_sample % 12];
				second = new[] { -8000, -12000, -5000, -17000, -6000, 4000, -15000, -9000, -4000, -11000, 6000, -3000 }[_sample % 12];
				third = new[] { 5000, 0, 14000, 10000, -4000, -7000, 12000, 0, -6000, 9000, -5000, -8000 }[_sample % 12];
				break;
			case "Missing Observations":
				first = new[] { 0, 4.2, 6.8, 0, 12.1, 18.4, 7.6, 2.5, 0, 9.4, 16.2, 3.1 }[_sample % 12];
				second = new[] { 0, 2.1, 5.2, 0, 8.8, 14.2, 6.1, 1.3, 0, 7.5, 12.6, 1.9 }[_sample % 12];
				third = new[] { 0, 6.4, 8.1, 0, 15.3, 21.2, 10.4, 3.7, 0, 12.9, 19.2, 4.8 }[_sample % 12];
				break;
			default:
				var traffic = new[] { 18, 15, 12, 10, 16, 32, 55, 82, 112, 136, 149, 158, 145, 152, 140, 126, 134, 170, 128, 96, 72, 49, 35, 24 }[phase];
				first = traffic + _sample / 24 % 3 * 4;
				second = Math.Round(traffic * 0.68 + phase % 5 * 3);
				third = _sample % 13 == 0 ? 0 : Math.Round(traffic * 0.24 + (_sample % 7) * 2);
				break;
		}
		// Missing readings are retained as null; a measured zero remains a plotted observation.
		_first.Add(new GraphChartPoint(_sample, _gaps.Checked && _sample % 17 is 8 or 9 ? null : first));
		_second.Add(new GraphChartPoint(_sample, _gaps.Checked && _sample % 19 == 12 ? null : second));
		_third.Add(new GraphChartPoint(_sample, third));
		_sample++;
		if (_first.Count > SampleCapacity) {
			_first.RemoveAt(0);
			_second.RemoveAt(0);
			_third.RemoveAt(0);
		}
		if (render)
			RenderSamples();
	}

	private void RenderSamples() {
		var names = SelectedScenario switch {
			"Quarterly Revenue" => new[] { "Subscriptions", "Professional Services", "Licensing" },
			"Signed Cash Flow" => new[] { "Operating", "Investing", "Financing" },
			"Missing Observations" => new[] { "North Station", "Central Station", "South Station" },
			_ => new[] { "Web Requests", "API Calls", "Background Jobs" }
		};
		var data = new[] { _first.ToArray(), _second.ToArray(), _third.ToArray() };
		_line.SetSeries(data.Select((points, index) => new GraphChartSeries(names[index], points, _seriesColors[index])).ToArray());
		_area.SetSeries(data.Select((points, index) => new GraphChartSeries(names[index], points, Color.FromArgb(105, _seriesColors[index]))).ToArray());
		var categories = data.Select((points, index) => new GraphChartSeries(names[index], points.TakeLast(6)
			.Select((point, category) => new GraphChartPoint(category, point.Y, PeriodLabel((int)point.X))).ToArray(), _seriesColors[index])).ToArray();
		_columns.SetSeries(categories);
		_bars.SetSeries(categories);
		ApplyPresentation();
		var missing = data.Sum(points => points.Count(point => !point.Y.HasValue));
		var zeros = data.Sum(points => points.Count(point => point.Y == 0));
		_status.Text = $"{SelectedScenario}  •  {_sample:N0} Captured  •  {_first.Count:N0} / {SampleCapacity} Retained Per Series  •  {missing} Missing  •  {zeros} Real Zeros";
	}

	private void ConfigureScenario() {
		_description.Text = SelectedScenario switch {
			"Quarterly Revenue" => "Three Revenue Streams Across Fiscal Quarters • Currency Formatting • Grouped Comparisons Of The Latest Six Quarters",
			"Signed Cash Flow" => "Operating, Investing And Financing • Positive And Negative Amounts Share A Zero Baseline • Six-Month Comparison",
			"Missing Observations" => "Daily Rainfall At Three Stations • Outages Remain Gaps • Dry Days Are Real Zero Measurements",
			_ => "Web, API And Worker Traffic Through A Working Day • Three Shared Series • Compare Trends And The Latest Six Minutes"
		};
		_line.XLabelFormatter = _area.XLabelFormatter = value => PeriodLabel(Math.Max(0, (int)Math.Round(value)));
	}

	private string PeriodLabel(int index) {
		return SelectedScenario switch {
			"Quarterly Revenue" => $"Q{index % 4 + 1} '{21 + index / 4:00}",
			"Signed Cash Flow" => new DateTime(2024, 1, 1).AddMonths(Math.Max(0, index) % 120).ToString("MMM yy"),
			"Missing Observations" => "Day " + (index + 1),
			_ => TimeSpan.FromMinutes(9 * 60 + index).ToString(@"hh\:mm")
		};
	}

	private void PresentationChanged(object sender, EventArgs args) {
		if (_ready) {
			ApplyPresentation();
			SaveScreenPreferences();
		}
	}

	private void IntervalChanged(object sender, EventArgs args) {
		if (_ready && _interval.SelectedIndex >= 0)
			_timer.Interval = new[] { 250, 500, 1000, 2000 }[_interval.SelectedIndex];
		SaveScreenPreferences();
	}

	private void ClearClicked(object sender, EventArgs args) {
		_live.Checked = false;
		_first.Clear();
		_second.Clear();
		_third.Clear();
		_sample = 0;
		foreach (var chart in new GraphChart[] { _line, _area, _columns, _bars })
			chart.Clear();
		_status.Text = "Capture Cleared  •  All Four Charts Have No Data  •  Add A Sample Or Reset Capture To Begin";
	}

	// One ordinary value object owns this screen's presentation, not its generated samples.
	private void ApplyScreenPreferences() {
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_scenario.SelectedItem = values.Scenario;
			_interval.SelectedIndex = values.SampleIntervalMilliseconds switch {
				250 => 0,
				1000 => 2,
				2000 => 3,
				_ => 1
			};
			_gaps.Checked = values.ShowGaps;
			_legends.Checked = values.ShowLegends;
			_labels.Checked = values.ShowLabels;
			_tooltips.Checked = values.ShowTooltips;
			_fixedRange.Checked = values.FixedRanges;
			ApplyPresentation();
		}
	}

	private void ApplyPresentation() {
		var currency = SelectedScenario is "Quarterly Revenue" or "Signed Cash Flow";
		var units = currency ? "Amount (USD)" : SelectedScenario == "Missing Observations" ? "Rainfall (mm)" : "Requests / Minute";
		var maximum = SelectedScenario switch {
			"Quarterly Revenue" => 160000D,
			"Signed Cash Flow" => 30000D,
			"Missing Observations" => 25D,
			_ => 220D
		};
		foreach (var chart in new GraphChart[] { _line, _area, _columns, _bars }) {
			chart.ShowLegend = _legends.Checked;
			chart.YValueFormat = currency ? "C0" : SelectedScenario == "Missing Observations" ? "0.0" : "N0";
			if (chart.ShowToolTips != _tooltips.Checked)
				chart.ShowToolTips = _tooltips.Checked;
			chart.YAxisTitle = units;
			chart.XAxisTitle = chart == _line || chart == _area ? "Capture Period" : "Latest Six Periods";
			// Clear both previous bounds before moving between signed and positive-only scenarios.
			chart.MinimumY = null;
			chart.MaximumY = null;
			if (_fixedRange.Checked) {
				chart.MinimumY = SelectedScenario == "Signed Cash Flow" ? -20000 : 0;
				chart.MaximumY = maximum;
			}
			foreach (var series in chart.NativeChart.Series) {
				series.IsValueShownAsLabel = _labels.Checked && (chart == _columns || chart == _bars);
				series.LabelFormat = chart.YValueFormat;
			}
		}
	}

	// Only deliberate UI changes mark values for the existing accepted-close persistence.
	private void SaveScreenPreferences() {
		if (!HasSettings || Updating || !_ready)
			return;
		var values = ScreenSettings;
		values.Scenario = SelectedScenario;
		values.SampleIntervalMilliseconds = _timer.Interval;
		values.ShowGaps = _gaps.Checked;
		values.ShowLegends = _legends.Checked;
		values.ShowLabels = _labels.Checked;
		values.ShowTooltips = _tooltips.Checked;
		values.FixedRanges = _fixedRange.Checked;
		MarkSettingsChanged();
	}

	#region Inner Types

	/// <summary>Remembers the graph-and-bar example's scenario and presentation choices.</summary>
	[UserEditable]
	public class ChartTestScreenSettings : SettingsObject {
		/// <summary>The generated dataset used by all four charts.</summary>
		[DefaultValue("Service Activity")]
		[AllowedValues("Service Activity", "Quarterly Revenue", "Signed Cash Flow", "Missing Observations")]
		public string Scenario { get; set; } = "Service Activity";

		/// <summary>The interval between simulated observations.</summary>
		[DefaultValue(500)]
		[AllowedValues(250, 500, 1000, 2000), DisplayName("Sample interval (ms)")]
		public int SampleIntervalMilliseconds { get; set; } = 500;

		/// <summary>Whether unavailable observations are retained as gaps.</summary>
		[DefaultValue(true)]
		[DisplayName("Include missing observations")]
		public bool ShowGaps { get; set; } = true;

		/// <summary>Whether charts display series legends.</summary>
		[DefaultValue(true)]
		[DisplayName("Show legends")]
		public bool ShowLegends { get; set; } = true;

		/// <summary>Whether the bar and column charts display value labels.</summary>
		[DefaultValue(false)]
		[DisplayName("Show value labels")]
		public bool ShowLabels {
			get; set;
		}

		/// <summary>Whether the native chart offers point details.</summary>
		[DefaultValue(true)]
		[DisplayName("Show tooltips")]
		public bool ShowTooltips { get; set; } = true;

		/// <summary>Whether each scenario retains its comparable fixed scale.</summary>
		[DefaultValue(true)]
		[DisplayName("Use fixed ranges")]
		public bool FixedRanges { get; set; } = true;
	}
	#endregion
}
