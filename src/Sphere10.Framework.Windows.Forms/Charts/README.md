# Charts

Designer-editable numeric and telemetry chart controls over captured data, with a shared Log Output workflow for chart observations.

`GraphChart` plots `GraphChartSeries` of `GraphChartPoint` values as lines, areas, columns or bars. `BarChart` is its column-chart preset. `TelemetryChart` plots `TelemetryChartPoint` time series in percent, bytes, bytes per second or Celsius. All three render through [Sphere10.Framework.Windows.Forms.DataVisualization](../../Sphere10.Framework.Windows.Forms.DataVisualization/README.md). None of them samples data. The caller captures values and passes detached arrays with `SetSeries` or `SetSamples` on the owning UI thread.

`ChartLogOutputBehavior` adds the **Log Output** context command and bounded asynchronous logging of chart observations through the `LogOutputSelectorControl` in [Logging](../Logging/README.md). The chart controls create their own behavior. Create one explicitly only for a bare native `Chart`, and dispose it on the UI thread. `WindowsFormsChartUtil`, reached as `Tools.Windows.Forms.Charts`, creates these controls and formats log lines without a control. The project [README](../README.md) covers ranges, gaps, hover lookup and log formatting in detail.

```csharp
var chart = new BarChart { YAxisTitle = "Temperature (°C)", MinimumY = 0 };
chart.SetSeries(new[] {
	new GraphChartSeries("CPU Cores", new[] {
		new GraphChartPoint(0, 42, "Core 0"),
		new GraphChartPoint(1, 44, "Core 1")
	}, Color.Firebrick)
});
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ChartLogOutputBehavior`](ChartLogOutputBehavior.cs) | class | Adds the Log Output command and deduplicated, bounded asynchronous logging of a chart's captured observations |
| [`ChartLogSample`](ChartLogSample.cs) | record | One captured reading with series, identity, position, values and optional capture time, units and position label |
| [`GraphChartPoint`](GraphChartPoint.cs) | struct | Readonly record struct of an X value, an optional Y value where null marks a gap, and an optional category label |
| [`GraphChartSeries`](GraphChartSeries.cs) | class | Named series that copies its points and carries an optional color and style override |
| [`GraphChartStyle`](GraphChartStyle.cs) | enum | Line, area, column or bar presentation for `GraphChart` |
| [`TelemetryChartPoint`](TelemetryChartPoint.cs) | record | UTC capture time and optional value, with optional detached `Context` for hover text and activation |
| [`TelemetryChartStyle`](TelemetryChartStyle.cs) | enum | Line or filled-area telemetry history |
| [`TelemetryChartUnit`](TelemetryChartUnit.cs) | enum | Percent, bytes, bytes per second or Celsius scale for `TelemetryChart` |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`BarChart`](Controls/BarChart.cs) | class | `GraphChart` preset with column style, category labels and margins |
| [`GraphChart`](Controls/GraphChart.cs) | class | Numeric line, area, column and bar chart with axis ranges, titles, formatters, legend, gaps and nearest-point lookup |
| [`TelemetryChart`](Controls/TelemetryChart.cs) | class | Time-series chart with unit scales, header, hover values and `SampleActivated` for captured telemetry |
