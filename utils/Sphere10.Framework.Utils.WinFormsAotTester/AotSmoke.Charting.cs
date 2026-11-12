// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Utils.WinFormsTester;

internal static partial class AotSmoke {
	private static async Task VerifyDataVisualization(BlockMainForm main) {
		var host = main.ScreenHost;
		var block = host.Blocks.Single(item => item.ScreenDefinitions.Any(definition => definition.ScreenType == typeof(ChartGalleryScreen)));
		var definition = block.ScreenDefinitions.Single(item => item.ScreenType == typeof(ChartGalleryScreen));
		Record("ChartingActivate");
		var gallery = (ChartGalleryScreen)await host.OpenScreenAsync(block, definition);
		await Task.Yield();
		var commands = gallery.Controls.Find("galleryCommands", true).OfType<ToolStrip>().Single();
		var categories = commands.Items.OfType<ToolStripComboBox>().Single();
		var next = commands.Items.OfType<ToolStripButton>().Single(item => item.Name == "galleryNext");
		var pages = commands.Items.OfType<ToolStripLabel>().Single(item => item.Name == "galleryPageNumber");
		var views = gallery.Controls.Find("galleryViews", true).OfType<TabControl>().Single();
		var tiles = gallery.Controls.Find("galleryTiles", true).Single().Controls.OfType<ChartGalleryTile>().ToArray();
		Guard.Ensure(ChartGallerySamples.All.Count == 39 && tiles.Length == 6 && categories.Items.Count == 9, "The chart catalog or gallery layout changed; review the representative chart checks.");
		categories.SelectedIndex = 0;
		var thumbnails = new HashSet<string>(StringComparer.Ordinal);
		var pageCount = 0;
		while (true) {
			await Task.Yield();
			gallery.PerformLayout();
			Guard.Ensure(views.SelectedTab.Name == "galleryOverviewPage");
			foreach (var tile in tiles.Where(tile => tile.Sample != null)) {
				var chart = tile.Controls.OfType<Chart>().Single();
				Guard.Ensure(tile.Visible && thumbnails.Add(tile.Sample.Id), "Gallery pagination repeated or hid an example.");
				VerifyChartData(chart, tile.Sample.Id);
				var path = Path.Combine(_directory, "chart-thumbnail-" + tile.Sample.Id + ".png");
				// SaveImage calls the renderer directly, so a caught WinForms paint failure cannot pass as a preview.
				chart.SaveImage(path, ChartImageFormat.Png);
				VerifyChartImage(path, chart.Size);
				Record("ChartingThumbnail=" + tile.Sample.Id);
			}
			pageCount++;
			Record("ChartingGalleryPage=" + pages.Text);
			CaptureControl(gallery, "chart-gallery-page-" + pageCount + ".png");
			if (!next.Enabled)
				break;
			Guard.Ensure(pageCount < 7, "Chart gallery pagination did not terminate.");
			next.PerformClick();
		}
		Guard.Ensure(pageCount == 7 && thumbnails.SetEquals(ChartGallerySamples.All.Select(sample => sample.Id)));
		for (var index = 1; index < categories.Items.Count; index++) {
			categories.SelectedIndex = index;
			await Task.Yield();
			var category = categories.SelectedItem.ToString();
			var visible = tiles.Where(tile => tile.Sample != null).ToArray();
			Guard.Ensure(visible.Length > 0 && visible.All(tile => tile.Visible && tile.Sample.Category == category));
			CaptureControl(gallery, "chart-family-" + index + ".png");
			Record("ChartingFamily=" + category);
		}
		categories.SelectedIndex = 0;
		var explorer = gallery.Controls.Find("galleryNativeChart", true).OfType<Chart>().Single();
		var chartTypes = new HashSet<SeriesChartType>();
		var explored = 0;
		foreach (var sample in ChartGallerySamples.All) {
			gallery.SelectExample(sample.Id);
			await Task.Yield();
			Guard.Ensure(gallery.SelectedExample == sample && views.SelectedTab.Name == "galleryExplorePage");
			VerifyChartData(explorer, sample.Id);
			foreach (var series in explorer.Series)
				chartTypes.Add(series.ChartType);
			var path = Path.Combine(_directory, "chart-explorer-" + sample.Id + ".png");
			await gallery.SavePngAsync(path);
			VerifyChartImage(path, explorer.Size);
			CaptureControl(gallery, "chart-live-" + sample.Id + ".png");
			explored++;
			Record("ChartingExample=" + sample.Id);
		}
		Guard.Ensure(explored == 39 && chartTypes.Count == 35, "The native gallery did not exercise every chart renderer.");
		Record("ChartingThumbnails=" + thumbnails.Count);
		Record("ChartingGalleryPages=" + pageCount);
		Record("ChartingExamples=" + explored);
		Record("ChartingTypes=" + chartTypes.Count);
		Record("ChartingVerified");
	}

	private static void VerifyChartData(Chart chart, string example) {
		Guard.Ensure(!chart.SuppressExceptions && chart.Width > 100 && chart.Height > 100, "Chart rendering must remain visible and exception reporting enabled: " + example);
		Guard.Ensure(chart.Series.Any(series => series.Enabled && series.Points.Any(point => !point.IsEmpty)), "Chart has no populated series: " + example);
		foreach (var series in chart.Series)
			Guard.Ensure(series.Points.All(point => point.YValues.Length >= series.YValuesPerPoint && point.YValues.All(double.IsFinite)), "Chart has incomplete observations: " + example);
	}

	private static void VerifyChartImage(string path, Size expectedSize) {
		using var image = new Bitmap(path);
		Guard.Ensure(image.Size == expectedSize, "Chart export dimensions do not match the displayed chart: " + path);
		var coloredPixels = 0;
		for (var vertical = 0; vertical < image.Height; vertical += 5)
			for (var horizontal = 0; horizontal < image.Width; horizontal += 5) {
				var color = image.GetPixel(horizontal, vertical);
				if (Math.Max(color.R, Math.Max(color.G, color.B)) - Math.Min(color.R, Math.Min(color.G, color.B)) > 30)
					coloredPixels++;
			}
		Guard.Ensure(coloredPixels > 5, "Chart export has no colored plotted data: " + path);
	}
}
