// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms.DataVisualization;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class WinForms {

	/// <summary>Creates fresh chart surfaces on the caller's UI thread. The caller or containing control owns each result; factories do not start sampling.</summary>
	public static class Charts {
		/// <summary>Creates the existing line/area/bar graph with its ordinary defaults and built-in logging workflow.</summary>
		public static GraphChart CreateGraphChart() => new GraphChart();

		/// <summary>Creates the existing column chart. Select its Bar style for horizontal bars.</summary>
		public static BarChart CreateBarChart() => new BarChart();

		/// <summary>Creates a telemetry display over caller-supplied captured samples; the control does not collect them.</summary>
		public static TelemetryChart CreateTelemetryChart() => new TelemetryChart();

		/// <summary>Creates the absorbed DataVisualization chart for advanced native chart types.</summary>
		public static Chart CreateChart() => new Chart();

		/// <summary>Attaches caller-owned logging behavior to a bare native chart. GraphChart, BarChart and TelemetryChart already expose their own LogOutputBehavior; use that instance for those wrappers.</summary>
		/// <remarks>The chart remains borrowed. Create and dispose the behavior on its UI thread and await its Completion or DisposeAsync before ending that UI lifetime.</remarks>
		public static ChartLogOutputBehavior CreateLogOutputBehavior(Chart chart) => new ChartLogOutputBehavior(chart);

		/// <summary>Formats one captured chart sample through the existing UTC, title and reading formatter without creating a control or output.</summary>
		public static string FormatLogSample(string title, DateTime capturedAtUtc, ChartLogSample[] readings) => ChartLogOutputBehavior.FormatSample(title, capturedAtUtc, readings);

		/// <summary>Formats one log line per device or group ("Group / Metric" series) of a captured sample, each carrying the sample time, without creating a control or output.</summary>
		public static string[] FormatLogLines(string title, DateTime capturedAtUtc, ChartLogSample[] readings)
			=> ChartLogOutputBehavior.FormatSampleLines(title, capturedAtUtc, readings);
	}
}
