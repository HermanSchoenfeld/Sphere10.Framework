// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Utils.WinFormsTester;

public partial class ChartGalleryScreen : WinFormsApplicationScreen {
	private ChartGallerySample[] _filtered = Array.Empty<ChartGallerySample>();
	private ChartGalleryTile[] _tiles;
	private ChartGallerySample _selected;
	private int _page;
	private bool _changing;

	public ChartGalleryScreen() {
		InitializeComponent();
		Title = "Data Visualization Gallery";
		if (Tools.Runtime.IsDesignMode)
			return;
		_tiles = new[] { _tile1, _tile2, _tile3, _tile4, _tile5, _tile6 };
		_introduction.Text = $"{ChartGallerySamples.All.Count} Examples • Browse The Gallery, Then Explore And Customize An Example • All Datasets Are Simulated";
		_categories.Items.Add("All Chart Families");
		_categories.Items.AddRange(ChartGallerySamples.All.Select(sample => sample.Category).Distinct().Cast<object>().ToArray());
		_palette.Items.AddRange(new object[] { "Example Colors", "Bright Pastel", "Pastel", "Earth Tones", "Berry", "Sea Green" });
		_palette.SelectedIndex = 0;
		_categories.SelectedIndex = 0;
	}

	public ChartGallerySample SelectedExample => _selected;

	public void SelectExample(string id) {
		var sample = ChartGallerySamples.All.Single(example => example.Id == id);
		if (!_filtered.Contains(sample))
			_categories.SelectedIndex = 0;
		ShowExample(sample, true);
	}

	public async Task SavePngAsync(string path) {
		Guard.ArgumentNotNullOrEmpty(path, nameof(path));
		using var image = new MemoryStream();
		_chart.SaveImage(image, ChartImageFormat.Png);
		await File.WriteAllBytesAsync(path, image.ToArray());
	}

	private void CategoryChanged(object sender, EventArgs args) {
		if (_tiles == null || _changing)
			return;
		_filtered = ChartGallerySamples.All.Where(sample => _categories.SelectedIndex == 0 || sample.Category == _categories.SelectedItem?.ToString()).ToArray();
		if (_categories.SelectedIndex == 0) {
			var featured = new[] { "Funnel", "Radar", "Pie", "Column", "Candlestick", "DualAxis" };
			_filtered = _filtered.OrderBy(sample => Array.IndexOf(featured, sample.Id) is var index && index >= 0 ? index : featured.Length).ToArray();
		}
		_page = 0;
		_changing = true;
		using (Tools.Scope.ExecuteOnDispose(() => _changing = false)) {
			_examples.Items.Clear();
			_examples.Items.AddRange(_filtered.Select(sample => sample.Name).Cast<object>().ToArray());
		}
		RenderGallery();
		if (_filtered.Length > 0)
			ShowExample(_filtered[0], false);
		_views.SelectedTab = _galleryPage;
	}

	private void RenderGallery() {
		var first = _page * _tiles.Length;
		for (var index = 0; index < _tiles.Length; index++)
			_tiles[index].ShowSample(_filtered.Skip(first + index).FirstOrDefault());
		_previous.Enabled = _page > 0;
		_next.Enabled = first + _tiles.Length < _filtered.Length;
		_pageNumber.Text = $"Page {_page + 1} Of {Math.Max(1, (_filtered.Length + _tiles.Length - 1) / _tiles.Length)}";
		_status.Text = $"{_filtered.Length} Examples In This Family • Click A Chart Or Explore Example • Drag In The Explorer To Zoom Numeric Charts";
	}

	private void ShowExample(ChartGallerySample sample, bool explore) {
		_selected = sample;
		_changing = true;
		using (Tools.Scope.ExecuteOnDispose(() => _changing = false)) {
			ChartGalleryRenderer.Render(_chart, sample);
			_examples.SelectedIndex = Array.IndexOf(_filtered, sample);
			_description.Text = sample.Description;
			_legend.Checked = _chart.Legends.Any(legend => legend.Enabled);
			_labels.Checked = _chart.Series.Any(series => series.IsValueShownAsLabel || !string.IsNullOrEmpty(series.Label)
				|| series.Points.Any(point => point.IsValueShownAsLabel || !string.IsNullOrEmpty(point.Label)));
			_threeD.Enabled = _chart.Series.All(series => SupportsThreeDimensions(series.ChartType));
			_threeD.Checked = _threeD.Enabled && _chart.ChartAreas.Any(area => area.Area3DStyle.Enable3D);
			_zoom.Enabled = _chart.Series.All(series => series.ChartType is not (SeriesChartType.Pie or SeriesChartType.Doughnut or SeriesChartType.Radar or SeriesChartType.Polar or SeriesChartType.Funnel or SeriesChartType.Pyramid));
			_zoom.Checked = _zoom.Enabled;
			_palette.SelectedIndex = 0;
		}
		ApplyZoom();
		_pointDetails.Text = "Hover For Values. Click A Data Point To Inspect It. Numeric Charts Support Drag Selection To Zoom.";
		if (explore)
			_views.SelectedTab = _explorePage;
	}

	private static bool SupportsThreeDimensions(SeriesChartType type) => type is SeriesChartType.Line or SeriesChartType.Spline or SeriesChartType.Area
		or SeriesChartType.StackedArea or SeriesChartType.StackedArea100 or SeriesChartType.Column or SeriesChartType.Bar
		or SeriesChartType.StackedColumn or SeriesChartType.StackedColumn100 or SeriesChartType.StackedBar or SeriesChartType.StackedBar100
		or SeriesChartType.Pie or SeriesChartType.Doughnut or SeriesChartType.Funnel or SeriesChartType.Pyramid;

	private void PreviousClicked(object sender, EventArgs args) { if (_page > 0) { _page--; RenderGallery(); } }

	private void NextClicked(object sender, EventArgs args) { if ((_page + 1) * _tiles.Length < _filtered.Length) { _page++; RenderGallery(); } }

	private void ExampleChanged(object sender, EventArgs args) {
		if (!_changing && _examples.SelectedIndex >= 0)
			ShowExample(_filtered[_examples.SelectedIndex], true);
	}

	private void TileExploreRequested(object sender, EventArgs args) {
		if (sender is ChartGalleryTile { Sample: { } sample })
			ShowExample(sample, true);
	}

	private void OptionsChanged(object sender, EventArgs args) {
		if (_changing || _selected == null)
			return;
		foreach (var legend in _chart.Legends)
			legend.Enabled = _legend.Checked;
		foreach (var series in _chart.Series) {
			series.IsValueShownAsLabel = _labels.Checked;
			// These renderers show explicit point labels independently of IsValueShownAsLabel.
			var property = series.ChartType switch {
				SeriesChartType.Pie or SeriesChartType.Doughnut => "PieLabelStyle",
				SeriesChartType.Funnel => "FunnelLabelStyle",
				SeriesChartType.Pyramid => "PyramidLabelStyle",
				_ => null
			};
			if (property != null)
				series[property] = !_labels.Checked ? "Disabled" : property == "PieLabelStyle" ? "Outside" : "OutsideInColumn";
		}
		foreach (var area in _chart.ChartAreas)
			area.Area3DStyle.Enable3D = _threeD.Enabled && _threeD.Checked;
		ApplyZoom();
	}

	private void ApplyZoom() {
		foreach (var area in _chart.ChartAreas) {
			area.CursorX.IsUserEnabled = area.CursorY.IsUserEnabled = _zoom.Enabled && _zoom.Checked;
			area.CursorX.IsUserSelectionEnabled = area.CursorY.IsUserSelectionEnabled = _zoom.Enabled && _zoom.Checked;
			area.AxisX.ScaleView.Zoomable = area.AxisY.ScaleView.Zoomable = _zoom.Enabled && _zoom.Checked;
			area.AxisX.ScrollBar.Enabled = area.AxisY.ScrollBar.Enabled = true;
		}
	}

	private void PaletteChanged(object sender, EventArgs args) {
		if (_changing || _selected == null || _palette.SelectedIndex < 0)
			return;
		if (_palette.SelectedIndex == 0) {
			ShowExample(_selected, true);
			return;
		}
		_chart.Palette = new[] { ChartColorPalette.BrightPastel, ChartColorPalette.Pastel, ChartColorPalette.EarthTones, ChartColorPalette.Berry, ChartColorPalette.SeaGreen }[_palette.SelectedIndex - 1];
		foreach (var series in _chart.Series) {
			series.Color = Color.Empty;
			foreach (var point in series.Points)
				point.Color = Color.Empty;
		}
	}

	private void ResetExampleClicked(object sender, EventArgs args) { if (_selected != null) ShowExample(_selected, true); }

	private void ResetZoomClicked(object sender, EventArgs args) {
		foreach (var area in _chart.ChartAreas) {
			area.AxisX.ScaleView.ZoomReset(0);
			area.AxisY.ScaleView.ZoomReset(0);
		}
	}

	private void ChartMouseClicked(object sender, MouseEventArgs args) {
		var hit = _chart.HitTest(args.X, args.Y);
		if (hit.Series == null || hit.PointIndex < 0 || hit.PointIndex >= hit.Series.Points.Count)
			return;
		var point = hit.Series.Points[hit.PointIndex];
		var x = string.IsNullOrEmpty(point.AxisLabel) ? point.XValue.ToString("G6", CultureInfo.InvariantCulture) : point.AxisLabel;
		_pointDetails.Text = $"{hit.Series.Name} • {x} • {string.Join(" / ", point.YValues.Select(value => value.ToString("G6", CultureInfo.InvariantCulture)))}";
	}

	private async void CSharpClicked(object sender, EventArgs args) => await ExecuteAsync(async () => {
		var code = ChartGalleryRenderer.CreateCSharpExample(_chart);
		await ReviewTextDialog.ShowAsync(this, _selected.Name + " — C# Example", "Copy Or Save A C# Factory That Recreates This Chart, Including Your Current Settings And Data.",
			code, "ChartExample.cs", "C# Files (*.cs)|*.cs|All Files (*.*)|*.*");
	});

	private async void SaveImageClicked(object sender, EventArgs args) => await ExecuteAsync(async () => {
		using var picker = new SaveFileDialog { Filter = "PNG Images (*.png)|*.png", DefaultExt = "png", FileName = _selected.Id + ".png", AddExtension = true, OverwritePrompt = true };
		if (picker.ShowDialog(this) == DialogResult.OK)
			await SavePngAsync(picker.FileName);
	});

	private async Task ExecuteAsync(Func<Task> work) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(work);
		if (error != null && !IsDisposed)
			await ExceptionDialog.ShowAsync(this, "Chart Example", error);
	}
}
