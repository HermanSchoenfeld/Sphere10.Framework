// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Globalization;
using System.IO;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Utils.WinFormsTester;

public static class ChartGalleryRenderer {
	public static void Render(Chart chart, ChartGallerySample sample, bool preview = false) {
		Guard.ArgumentNotNull(chart, nameof(chart));
		Guard.ArgumentNotNull(sample, nameof(sample));
		chart.BeginInit();
		using var finish = Tools.Scope.ExecuteOnDispose(chart.EndInit);
		chart.Series.Clear();
		chart.Annotations.Clear();
		chart.Titles.Clear();
		chart.ChartAreas.Clear();
		chart.Legends.Clear();
		chart.ChartAreas.Add(new ChartArea("Main"));
		chart.Legends.Add(new Legend("Legend") { Docking = Docking.Bottom });
		chart.BackColor = Color.White;
		chart.ForeColor = Color.FromArgb(45, 53, 65);
		chart.Font = SystemFonts.MessageBoxFont;
		chart.Palette = ChartColorPalette.BrightPastel;
		sample.Configure(chart);
		foreach (var area in chart.ChartAreas) {
			area.BackColor = Color.White;
			foreach (var axis in new[] { area.AxisX, area.AxisY, area.AxisX2, area.AxisY2 }) {
				axis.LineColor = Color.FromArgb(175, 185, 195);
				axis.MajorGrid.LineColor = Color.FromArgb(230, 235, 240);
				axis.LabelStyle.ForeColor = chart.ForeColor;
				if (preview) {
					axis.Title = string.Empty;
					axis.LabelStyle.Font = SystemFonts.SmallCaptionFont;
				}
			}
		}
		foreach (var series in chart.Series)
			if (string.IsNullOrEmpty(series.ToolTip))
				series.ToolTip = "#SERIESNAME\n#AXISLABEL\n#VALY";
		if (preview) {
			chart.Titles.Clear();
			foreach (var legend in chart.Legends)
				legend.Enabled = false;
		}
	}

	public static string CaptureConfiguration(Chart chart) {
		Guard.ArgumentNotNull(chart, nameof(chart));
		chart.Serializer.Content = SerializationContents.All;
		chart.Serializer.Format = SerializationFormat.Xml;
		using var writer = new StringWriter(CultureInfo.InvariantCulture);
		chart.Serializer.Save(writer);
		return writer.ToString();
	}

	/// <summary>A pasteable C# factory with the current native chart layout and data, including advanced elements.</summary>
	public static string CreateCSharpExample(Chart chart) {
		var xml = CaptureConfiguration(chart).Replace("\"", "\"\"");
		return "using System.IO;\nusing System.Windows.Forms;\nusing Sphere10.Framework.Windows.Forms.DataVisualization;\n\n"
			+ "// Place this method inside your form or control. The XML includes the current chart settings and sample data.\n"
			+ "private static Chart CreateExampleChart() {\n\tvar chart = new Chart { Dock = DockStyle.Fill };\n"
			+ "\tusing var configuration = new StringReader(@\"" + xml + "\");\n"
			+ "\tchart.Serializer.Load(configuration);\n\treturn chart;\n}\n";
	}
}
