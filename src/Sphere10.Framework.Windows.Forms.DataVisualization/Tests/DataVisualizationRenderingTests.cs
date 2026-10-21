// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using Sphere10.Framework.Windows.Forms.DataVisualization;

using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class DataVisualizationRenderingTests
{
    [TestCase(SeriesChartType.Line)]
    [TestCase(SeriesChartType.Bar)]
    [TestCase(SeriesChartType.Column)]
    public void RenderSeriesToPng(SeriesChartType chartType)
    {
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                using Chart chart = new() { Width = 640, Height = 320, BackColor = Color.White };
                chart.ChartAreas.Add("Plot");
                chart.Legends.Add("Legend");
                Series series = chart.Series.Add("Samples");
                series.ChartType = chartType;
                series.Color = Color.Crimson;
                series.BorderWidth = 2;
                series.Points.AddXY("First", 23);
                series.Points.AddXY("Second", 41);
                series.Points.AddXY("Third", 36);
                using MemoryStream stream = new();
                chart.SaveImage(stream, ChartImageFormat.Png);
                Assert.That(stream.Length, Is.GreaterThan(2000), "A real chart image should include its axes, labels and data.");
                stream.Position = 0;
                using Image image = Image.FromStream(stream);
                Assert.That(image.Size, Is.EqualTo(chart.Size));
                using Bitmap bitmap = new(image);
                bool hasSeriesColor = false;
                for (int y = 0; y < bitmap.Height && !hasSeriesColor; y++)
                    for (int x = 0; x < bitmap.Width && !hasSeriesColor; x++)
                    {
                        Color pixel = bitmap.GetPixel(x, y);
                        hasSeriesColor = pixel.R > 150 && pixel.G < 100 && pixel.B < 150;
                    }
                Assert.That(hasSeriesColor, Is.True, "The chart must draw the series, not only its background.");
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        Assert.That(thread.Join(TimeSpan.FromSeconds(30)), Is.True, "Chart rendering should finish on its STA thread.");
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}