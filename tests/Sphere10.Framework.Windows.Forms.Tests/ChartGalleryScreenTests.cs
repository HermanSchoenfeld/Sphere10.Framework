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
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartGalleryScreenTests {
	public static IEnumerable<TestCaseData> Examples => ChartGallerySamples.All.Select(sample => new TestCaseData(sample).SetName("GalleryConfigurationRoundtrip_" + sample.Id));

	[TestCase(950, 720)]
	[TestCase(1450, 950)]
	public void GalleryShowsVariedClickablePreviewsAndFiltersAcrossPages(int width, int height) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, height);
		using var screen = new ChartGalleryScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		await Task.Yield();
		var tiles = screen.Controls.Find("galleryTiles", true).Single().Controls.OfType<ChartGalleryTile>().ToArray();
		Assert.That(tiles.Select(tile => tile.Sample.Id), Is.EqualTo(new[] { "Funnel", "Radar", "Pie", "Column", "Candlestick", "DualAxis" }));
		Capture(screen, $"Gallery-{width}x{height}");
		var views = Find<TabControl>(screen, "galleryViews");
		tiles[2].Controls.OfType<Button>().Single().PerformClick();
		Assert.That(views.SelectedTab.Name, Is.EqualTo("galleryExplorePage"));
		Assert.That(screen.SelectedExample.Id, Is.EqualTo("Pie"));
		Assert.That(Find<Chart>(screen, "galleryNativeChart").Series.Single().ChartType, Is.EqualTo(SeriesChartType.Pie));
		Capture(screen, $"Gallery-Explore-Pie-{width}x{height}");
		var categories = Commands(screen, "galleryCommands").Items.OfType<ToolStripComboBox>().Single();
		categories.SelectedItem = "Finance";
		Assert.That(tiles.Where(tile => tile.Visible).All(tile => tile.Sample.Category == "Finance"), Is.True);
		categories.SelectedIndex = 0;
		var firstPage = tiles[0].Sample.Id;
		Commands(screen, "galleryCommands").Items.OfType<ToolStripButton>().Single(button => button.Text == "Next").PerformClick();
		Assert.That(tiles[0].Sample.Id, Is.Not.EqualTo(firstPage));
		Commands(screen, "galleryCommands").Items.OfType<ToolStripButton>().Single(button => button.Text == "Previous").PerformClick();
		Assert.That(tiles[0].Sample.Id, Is.EqualTo(firstPage));
	});

	[Test]
	public void ReusingExplorerAcrossEveryExampleKeepsTypesAndSupportsNativeImageExport() => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(1200, 820);
		using var screen = new ChartGalleryScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		var chart = Find<Chart>(screen, "galleryNativeChart");
		foreach (var sample in ChartGallerySamples.All) {
			screen.SelectExample(sample.Id);
			await Task.Yield();
			using var output = new MemoryStream();
			chart.SaveImage(output, ChartImageFormat.Png);
			Assert.That(output.Length, Is.GreaterThan(3000), sample.Id);
			Assert.That(chart.Series.Any(series => series.Points.Count > 0), Is.True, sample.Id);
		}
		screen.SelectExample("Thresholds");
		Capture(screen, "Gallery-Explore-Annotations");
		var path = Path.Combine(Path.GetTempPath(), "Gallery-" + Guid.NewGuid().ToString("N") + ".png");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		await screen.SavePngAsync(path);
		using var image = Image.FromFile(path);
		Assert.That(image.Size, Is.EqualTo(chart.Size));
	});

	[Test]
	public void ExplorerOptionsChangeTheActualNativeChartAndResetRestoresTheSample() => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(1200, 820);
		using var screen = new ChartGalleryScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		screen.SelectExample("Pie");
		var chart = Find<Chart>(screen, "galleryNativeChart");
		var tools = Commands(screen, "galleryExampleCommands");
		ToolStripButton Button(string name) => tools.Items.OfType<ToolStripButton>().Single(button => button.Name == name);
		Assert.That(Button("galleryThreeDimensions").Enabled, Is.True);
		Assert.That(chart.ChartAreas[0].Area3DStyle.Enable3D, Is.True);
		Button("galleryThreeDimensions").PerformClick();
		Assert.That(chart.ChartAreas[0].Area3DStyle.Enable3D, Is.False);
		Button("galleryLegend").PerformClick();
		Assert.That(chart.Legends.All(legend => !legend.Enabled), Is.True);
		Button("galleryLabels").Checked = true;
		Assert.That(chart.Series.All(series => series.IsValueShownAsLabel), Is.True);
		var palette = tools.Items.OfType<ToolStripComboBox>().Single(combo => combo.Name == "galleryPalette");
		palette.SelectedIndex = 3;
		Assert.That(chart.Palette, Is.EqualTo(ChartColorPalette.EarthTones));
		Button("galleryResetExample").PerformClick();
		Assert.That(chart.ChartAreas[0].Area3DStyle.Enable3D, Is.True);
		screen.SelectExample("Line");
		chart.ChartAreas[0].AxisX.ScaleView.Zoom(46027D, 46032D);
		Assert.That(chart.ChartAreas[0].AxisX.ScaleView.IsZoomed, Is.True);
		Button("galleryResetZoom").PerformClick();
		Assert.That(chart.ChartAreas[0].AxisX.ScaleView.IsZoomed, Is.False);
		await Task.Yield();
	});

	[TestCaseSource(nameof(Examples))]
	public void CurrentChartConfigurationRecreatesDataAxesAndAnnotations(ChartGallerySample sample) {
		using var original = new Chart { Size = new Size(850, 530) };
		ChartGalleryRenderer.Render(original, sample);
		var xml = ChartGalleryRenderer.CaptureConfiguration(original);
		using var recreated = new Chart { Size = original.Size };
		using var reader = new StringReader(xml);
		recreated.Serializer.Load(reader);
		Assert.That(recreated.Series.Select(series => series.ChartType), Is.EqualTo(original.Series.Select(series => series.ChartType)), sample.Id);
		Assert.That(recreated.Series.Select(series => series.Points.Count), Is.EqualTo(original.Series.Select(series => series.Points.Count)), sample.Id);
		Assert.That(recreated.ChartAreas.Count, Is.EqualTo(original.ChartAreas.Count));
		Assert.That(recreated.Annotations.Count, Is.EqualTo(original.Annotations.Count));
		using var image = new MemoryStream();
		recreated.SaveImage(image, ChartImageFormat.Png);
		Assert.That(image.Length, Is.GreaterThan(3000));
		var code = ChartGalleryRenderer.CreateCSharpExample(original);
		Assert.That(code, Does.Contain("CreateExampleChart()").And.Contain("chart.Serializer.Load(configuration)").And.Contain("Sphere10.Framework.Windows.Forms.DataVisualization"));
	}

	[TestCase("Pie", "PieLabelStyle")]
	[TestCase("Funnel", "FunnelLabelStyle")]
	public void ValueLabelToolAlsoControlsExplicitCircularAndFunnelPointLabels(string id, string styleProperty) => RunWithMessageLoop(async owner => {
		using var screen = new ChartGalleryScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(screen);
		screen.SelectExample(id);
		var chart = Find<Chart>(screen, "galleryNativeChart");
		var labels = Commands(screen, "galleryExampleCommands").Items.OfType<ToolStripButton>().Single(button => button.Name == "galleryLabels");
		Assert.That(labels.Checked, Is.True, "Explicit labels must be reflected in the initial tool state.");
		labels.PerformClick();
		Assert.That(chart.Series.Single()[styleProperty], Is.EqualTo("Disabled"));
		labels.PerformClick();
		Assert.That(chart.Series.Single()[styleProperty], Is.Not.EqualTo("Disabled"));
		using var image = new MemoryStream();
		chart.SaveImage(image, ChartImageFormat.Png);
		Assert.That(image.Length, Is.GreaterThan(3000));
		await Task.Yield();
	});

	private static ToolStrip Commands(Control parent, string name) => Find<ToolStrip>(parent, name);

	private static T Find<T>(Control parent, string name) where T : Control => parent.Controls.Find(name, true).OfType<T>().Single();

	private static void Capture(Control control, string name) {
		var path = Environment.GetEnvironmentVariable("SPHERE10_CHART_VALIDATION_DIRECTORY");
		if (string.IsNullOrEmpty(path)) return;
		Directory.CreateDirectory(path);
		using var image = new Bitmap(control.Width, control.Height);
		control.DrawToBitmap(image, control.ClientRectangle);
		image.Save(Path.Combine(path, name + ".png"), ImageFormat.Png);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, ClientSize = new Size(1200, 820), StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var timeout = new System.Windows.Forms.Timer { Interval = 45000 };
		timeout.Tick += (_, _) => { failure = new AssertionException("The chart gallery did not complete."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
		};
		timeout.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}
}
