// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class TelemetryChartTestScreen : WinFormsApplicationScreen {
	private const int RetainedSampleLimit = 180;
	private readonly List<TelemetryChartPoint> _cpuSamples = new();
	private readonly List<TelemetryChartPoint> _memorySamples = new();
	private readonly List<TelemetryChartPoint> _throughputSamples = new();
	private readonly List<TelemetryChartPoint> _temperatureSamples = new();
	private long _capturedSamples;
	private DateTime _lastCapturedAtUtc;

	public TelemetryChartTestScreen() {
		InitializeComponent();
		Title = "Live Telemetry";
		if (Tools.Runtime.IsDesignMode)
			return;
		_sampleInterval.SelectedIndex = 1;
		// Only generated observations enter these charts; this demonstration never reads hardware or the network.
		var firstTimestamp = DateTime.UtcNow.AddMilliseconds(-79 * _captureTimer.Interval);
		for (var index = 0; index < 80; index++)
			AddSample(firstTimestamp.AddMilliseconds(index * _captureTimer.Interval), false);
		RenderSamples();
	}

	protected override void OnVisibleChanged(EventArgs args) {
		base.OnVisibleChanged(args);
		UpdateCaptureState();
	}

	protected override void OnHandleCreated(EventArgs args) {
		base.OnHandleCreated(args);
		UpdateCaptureState();
	}

	protected override void OnHandleDestroyed(EventArgs args) {
		_captureTimer?.Stop();
		base.OnHandleDestroyed(args);
	}

	private void AddSample(DateTime capturedAtUtc, bool render = true) {
		if (capturedAtUtc <= _lastCapturedAtUtc)
			capturedAtUtc = _lastCapturedAtUtc.AddTicks(1);
		_lastCapturedAtUtc = capturedAtUtc;
		_cpuSamples.Add(new TelemetryChartPoint(capturedAtUtc, CreateValue(_capturedSamples, TelemetryChartUnit.Percent)));
		_memorySamples.Add(new TelemetryChartPoint(capturedAtUtc, CreateValue(_capturedSamples, TelemetryChartUnit.Bytes)));
		_throughputSamples.Add(new TelemetryChartPoint(capturedAtUtc, CreateValue(_capturedSamples, TelemetryChartUnit.BytesPerSecond)));
		_temperatureSamples.Add(new TelemetryChartPoint(capturedAtUtc, CreateValue(_capturedSamples, TelemetryChartUnit.Celsius)));
		_capturedSamples++;
		if (_cpuSamples.Count > RetainedSampleLimit) {
			_cpuSamples.RemoveAt(0);
			_memorySamples.RemoveAt(0);
			_throughputSamples.RemoveAt(0);
			_temperatureSamples.RemoveAt(0);
		}
		if (render)
			RenderSamples();
	}

	private double? CreateValue(long sample, TelemetryChartUnit unit) {
		if (_gaps.Checked && sample % 29 is 13 or 14)
			return null;
		return unit switch {
			TelemetryChartUnit.Percent => Math.Clamp(42 + 24 * Math.Sin(sample / 9D) + 13 * Math.Sin(sample / 3.4D)
				+ (sample % 47 is >= 29 and <= 33 ? 18 : 0), 4, 97),
			TelemetryChartUnit.Bytes => (5.3 + 1.1 * Math.Sin(sample / 28D) + 0.25 * Math.Sin(sample / 6D)) * 1024 * 1024 * 1024,
			TelemetryChartUnit.BytesPerSecond => (4 + 11 * Math.Pow(Math.Sin(sample / 11D), 2)
				+ (sample % 37 is >= 22 and <= 27 ? 21 : 0)) * 1024 * 1024,
			TelemetryChartUnit.Celsius => 38 + 48 * Math.Sin(sample / 13D),
			_ => throw new ArgumentOutOfRangeException(nameof(unit))
		};
	}

	private void RenderSamples() {
		_cpuChart.SetSamples(_cpuSamples.ToArray());
		_memoryChart.SetSamples(_memorySamples.ToArray());
		_throughputChart.SetSamples(_throughputSamples.ToArray());
		_temperatureChart.SetSamples(_temperatureSamples.ToArray());
		UpdateStatus();
	}

	private void UpdateCaptureState() {
		if (_captureTimer == null || _live == null || IsDisposed || Disposing)
			return;
		_captureTimer.Enabled = !Tools.Runtime.IsDesignMode && IsHandleCreated && Visible && _live.Checked;
		UpdateStatus();
	}

	private void UpdateStatus() {
		if (_status == null || _captureTimer == null)
			return;
		_status.Text = $"Simulated • {_cpuSamples.Count} / {RetainedSampleLimit} Retained • {_capturedSamples} Since Reset • "
			+ (_captureTimer.Enabled ? $"Live Every {_captureTimer.Interval:N0} ms" : "Paused") + " • Hover For Reading And Timestamp";
	}

	private void ClearSamples(bool resetCapture) {
		_cpuSamples.Clear();
		_memorySamples.Clear();
		_throughputSamples.Clear();
		_temperatureSamples.Clear();
		if (resetCapture) {
			_capturedSamples = 0;
			_lastCapturedAtUtc = DateTime.MinValue;
		}
		RenderSamples();
	}

	private void TimerTick(object sender, EventArgs args) => AddSample(DateTime.UtcNow);

	private void AddSampleClicked(object sender, EventArgs args) => AddSample(DateTime.UtcNow);

	private void ResetClicked(object sender, EventArgs args) => ClearSamples(true);

	private void ClearClicked(object sender, EventArgs args) {
		_live.Checked = false;
		ClearSamples(false);
	}

	private void LiveChanged(object sender, EventArgs args) => UpdateCaptureState();

	private void IntervalChanged(object sender, EventArgs args) {
		if (_captureTimer == null)
			return;
		_captureTimer.Interval = _sampleInterval.SelectedIndex switch { 0 => 250, 2 => 1000, 3 => 2000, _ => 500 };
		UpdateStatus();
	}

	private void GapsChanged(object sender, EventArgs args) {
		// Keep capture times and history length while comparing continuous signals with unavailable observations.
		for (var index = 0; index < _cpuSamples.Count; index++) {
			var sample = _capturedSamples - _cpuSamples.Count + index;
			_cpuSamples[index] = _cpuSamples[index] with { Value = CreateValue(sample, TelemetryChartUnit.Percent) };
			_memorySamples[index] = _memorySamples[index] with { Value = CreateValue(sample, TelemetryChartUnit.Bytes) };
			_throughputSamples[index] = _throughputSamples[index] with { Value = CreateValue(sample, TelemetryChartUnit.BytesPerSecond) };
			_temperatureSamples[index] = _temperatureSamples[index] with { Value = CreateValue(sample, TelemetryChartUnit.Celsius) };
		}
		RenderSamples();
	}
}
