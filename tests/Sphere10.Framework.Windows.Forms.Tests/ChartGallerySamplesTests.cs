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
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartGallerySamplesTests {
	public static IEnumerable<TestCaseData> Samples => ChartGallerySamples.All.Select(sample => new TestCaseData(sample).SetName("NativeGalleryRenders_" + sample.Id));

	[Test]
	public void GalleryCoversEveryNativeChartTypeWithUniqueDescribedExamples() {
		Assert.That(ChartGallerySamples.All.Select(sample => sample.Id), Is.Unique);
		Assert.That(ChartGallerySamples.All.Select(sample => sample.Category).Distinct().Count(), Is.EqualTo(8));
		Assert.That(ChartGallerySamples.All.All(sample => !string.IsNullOrWhiteSpace(sample.Name) && !string.IsNullOrWhiteSpace(sample.Description)), Is.True);
		foreach (var type in Enum.GetValues<SeriesChartType>()) {
			var sample = ChartGallerySamples.All.Single(candidate => candidate.Id == type.ToString());
			using var chart = CreateChart();
			sample.Configure(chart);
			Assert.That(chart.Series.Any(series => series.ChartType == type && series.Points.Count > 0), Is.True, $"The {type} example must actually use its advertised renderer.");
		}
	}

	[TestCaseSource(nameof(Samples))]
	public void NativeGalleryRendersRealSeriesAndExportableImages(ChartGallerySample sample) {
		using var chart = CreateChart();
		sample.Configure(chart);
		Assert.That(chart.Titles.Count, Is.GreaterThan(0), "Every example needs a meaningful chart heading.");
		Assert.That(chart.Series.Any(series => series.Enabled && series.Points.Any(point => !point.IsEmpty)), Is.True);
		foreach (var series in chart.Series) {
			Assert.That(series.Points.All(point => point.YValues.Length >= series.YValuesPerPoint), Is.True, $"{sample.Id}/{series.Name} has an incomplete multivalue observation.");
			Assert.That(series.Points.All(point => point.YValues.All(double.IsFinite)), Is.True);
		}
		chart.CreateControl();
		using var stream = new MemoryStream();
		chart.SaveImage(stream, ChartImageFormat.Png);
		Assert.That(stream.Length, Is.GreaterThan(4000), "An exported image should contain the plotted example, axes and labels.");
		stream.Position = 0;
		using var image = new Bitmap(stream);
		Assert.That(image.Size, Is.EqualTo(chart.Size));
		var coloredPixels = 0;
		for (var vertical = 0; vertical < image.Height; vertical += 5) {
			for (var horizontal = 0; horizontal < image.Width; horizontal += 5) {
				var color = image.GetPixel(horizontal, vertical);
				if (Math.Max(color.R, Math.Max(color.G, color.B)) - Math.Min(color.R, Math.Min(color.G, color.B)) > 30)
					coloredPixels++;
			}
		}
		Assert.That(coloredPixels, Is.GreaterThan(25), "The renderer should draw colored data rather than an empty white/gray plot.");
		var output = Environment.GetEnvironmentVariable("SPHERE10_CHART_VALIDATION_DIRECTORY");
		if (!string.IsNullOrEmpty(output)) {
			Directory.CreateDirectory(output);
			image.Save(Path.Combine(output, "GallerySample-" + sample.Id + ".png"), ImageFormat.Png);
		}
	}

	[TestCase("Stock", 4)]
	[TestCase("Candlestick", 4)]
	[TestCase("PointAndFigure", 2)]
	[TestCase("Range", 2)]
	[TestCase("SplineRange", 2)]
	[TestCase("RangeColumn", 2)]
	[TestCase("RangeBar", 2)]
	[TestCase("Bubble", 2)]
	[TestCase("ErrorBar", 3)]
	[TestCase("BoxPlot", 7)]
	public void MultivalueExamplesSupplyTheCorrectObservationShape(string id, int values) {
		using var chart = CreateChart();
		ChartGallerySamples.All.Single(sample => sample.Id == id).Configure(chart);
		var series = chart.Series.Single(candidate => candidate.ChartType.ToString() == id);
		Assert.That(series.YValuesPerPoint, Is.EqualTo(values));
		Assert.That(series.Points.All(point => point.YValues.Length == values), Is.True);
	}

	[Test]
	public void AdvancedExamplesDemonstrateIndependentAxesAreasAndRealAnnotations() {
		using var dualAxis = CreateChart();
		ChartGallerySamples.All.Single(sample => sample.Id == "DualAxis").Configure(dualAxis);
		Assert.That(dualAxis.Series.Any(series => series.YAxisType == AxisType.Secondary), Is.True);
		Assert.That(dualAxis.ChartAreas["Main"].AxisY2.Title, Does.Contain("Growth"));
		using var panels = CreateChart();
		ChartGallerySamples.All.Single(sample => sample.Id == "AlignedAreas").Configure(panels);
		Assert.That(panels.ChartAreas, Has.Count.EqualTo(2));
		Assert.That(panels.ChartAreas["Volume"].AlignWithChartArea, Is.EqualTo("Main"));
		using var logarithmic = CreateChart();
		ChartGallerySamples.All.Single(sample => sample.Id == "Logarithmic").Configure(logarithmic);
		Assert.That(logarithmic.ChartAreas["Main"].AxisY.IsLogarithmic, Is.True);
		Assert.That(logarithmic.Series.SelectMany(series => series.Points).All(point => point.YValues[0] > 0), Is.True);
		using var annotations = CreateChart();
		ChartGallerySamples.All.Single(sample => sample.Id == "Thresholds").Configure(annotations);
		Assert.That(annotations.Annotations, Has.Count.EqualTo(1));
		Assert.That(annotations.ChartAreas["Main"].AxisY.StripLines, Has.Count.EqualTo(1));
	}

	private static Chart CreateChart() {
		var chart = new Chart { Size = new Size(1000, 620), BackColor = Color.White };
		chart.ChartAreas.Add(new ChartArea("Main"));
		chart.Legends.Add(new Legend("Legend"));
		return chart;
	}
}
