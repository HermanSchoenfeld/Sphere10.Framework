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
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.DataVisualization;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartAssemblyConsolidationTests {
	[Test]
	public void ChartAndFormerUtilityPublicEnumsComeFromOneAssembly() {
		var assembly = typeof(Chart).Assembly;
		Assert.That(typeof(Chart).Namespace, Is.EqualTo("Sphere10.Framework.Windows.Forms.DataVisualization"));
		var types = new[] { typeof(AntiAliasingStyles), typeof(LabelAlignmentStyles), typeof(ScrollBarButtonStyles),
			typeof(AreaAlignmentOrientations), typeof(AreaAlignmentStyles), typeof(GridTickTypes), typeof(LabelAutoFitStyles),
			typeof(MarkerStyle), typeof(GradientStyle), typeof(ChartHatchStyle), typeof(SeriesChartType), typeof(AxisName), typeof(ChartColorPalette) };
		foreach (var type in types)
			Assert.That(type.Assembly, Is.SameAs(assembly), $"{type.FullName} must remain usable without a separate Utilities assembly.");
		Assert.That(assembly.GetReferencedAssemblies().Select(reference => reference.Name), Does.Not.Contain("WinForms.DataVisualization.Utilities"));
		Assert.That(assembly.GetManifestResourceNames(), Does.Contain("Sphere10.Framework.Windows.Forms.DataVisualization.SR.resources")
			.And.Contain("Sphere10.Framework.Windows.Forms.DataVisualization.ChartControl.bmp"));
		Assert.That(assembly.GetExportedTypes().Select(type => type.Namespace), Has.None.StartsWith("System.Windows.Forms.DataVisualization"));
	}

	[TestCase("en-US")]
	[TestCase("fr-FR")]
	public void DescriptionsCategoriesAndPositionConverterResolveAfterResourceMove(string cultureName) {
		var previousCulture = CultureInfo.CurrentUICulture;
		using var restoreCulture = Tools.Scope.ExecuteOnDispose(() => CultureInfo.CurrentUICulture = previousCulture);
		CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
		var resources = new ResourceManager("Sphere10.Framework.Windows.Forms.DataVisualization.SR", typeof(Chart).Assembly);
		using var releaseResources = Tools.Scope.ExecuteOnDispose(resources.ReleaseAllResources);
		using var chart = new Chart();
		var palette = TypeDescriptor.GetProperties(chart)[nameof(Chart.Palette)];
		var expectedDescription = resources.GetString("DescriptionAttributePalette", CultureInfo.CurrentUICulture);
		var expectedCategory = resources.GetString("CategoryAttributeAppearance", CultureInfo.CurrentUICulture);
		Assert.That(expectedDescription, Is.Not.Null.And.Not.Empty);
		Assert.That(palette.Description, Is.EqualTo(expectedDescription));
		Assert.That(palette.Category, Is.EqualTo(expectedCategory));
		Assert.That(palette.ShouldSerializeValue(chart), Is.False, "Standard DefaultValue metadata must still suppress an unchanged palette.");
		chart.Palette = ChartColorPalette.Excel;
		Assert.That(palette.ShouldSerializeValue(chart), Is.True);
		var position = TypeDescriptor.GetProperties(typeof(ChartArea))[nameof(ChartArea.Position)];
		Assert.That(position.Converter.GetType().Assembly, Is.SameAs(typeof(Chart).Assembly));
		Assert.That(position.Converter.CanConvertFrom(typeof(string)), Is.True);
		var converted = (ElementPosition)position.Converter.ConvertFromInvariantString("10, 15, 70, 65");
		Assert.That(new[] { converted.X, converted.Y, converted.Width, converted.Height }, Is.EqualTo(new[] { 10F, 15F, 70F, 65F }));
	}

	[Test]
	public void StandardWinFormsCollectionSerializationMetadataIsRetained() {
		using var chart = new Chart();
		var properties = TypeDescriptor.GetProperties(chart);
		foreach (var name in new[] { nameof(Chart.Series), nameof(Chart.ChartAreas), nameof(Chart.Legends), nameof(Chart.Titles), nameof(Chart.Annotations), nameof(Chart.Images) })
			Assert.That(properties[name].SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Content), name);
		foreach (var name in new[] { nameof(Chart.Serializer), nameof(Chart.Printing) })
			Assert.That(properties[name].SerializationVisibility, Is.EqualTo(DesignerSerializationVisibility.Hidden), name);
	}

	[TestCase(SeriesChartType.Line)]
	[TestCase(SeriesChartType.Bar)]
	public void XmlSerializerRoundTripsSeriesAppearanceAndNestedChartContents(SeriesChartType chartType) {
		using var original = new Chart { BackColor = Color.AliceBlue, Palette = ChartColorPalette.BrightPastel, AntiAliasing = AntiAliasingStyles.Text };
		original.ChartAreas.Add(new ChartArea("Telemetry") { Position = new ElementPosition(10, 15, 70, 65) });
		original.Legends.Add("Sensor Legend");
		original.Titles.Add("CPU Core Temperatures");
		original.Annotations.Add(new TextAnnotation { Name = "Sensor Note", Text = "Captured sensor values", X = 10, Y = 15, Width = 20, Height = 8 });
		var samples = new Series("CPU Core 0") { ChartArea = "Telemetry", Legend = "Sensor Legend", ChartType = chartType,
			Color = Color.FromArgb(255, 24, 94, 160), MarkerStyle = MarkerStyle.Diamond, BorderWidth = 2 };
		samples.Points.AddXY("Core A", 23.5);
		samples.Points.AddXY("Core B", -4.25);
		original.Series.Add(samples);
		original.Serializer.Content = SerializationContents.All;
		original.Serializer.Format = SerializationFormat.Xml;
		using var output = new StringWriter(CultureInfo.InvariantCulture);
		original.Serializer.Save(output);
		var xml = output.ToString();
		Assert.That(xml, Does.Contain("CPU Core 0").And.Contain(chartType.ToString()).And.Not.Contain("WinForms.DataVisualization.Utilities"));
		using var restored = new Chart();
		restored.Serializer.Content = SerializationContents.All;
		restored.Serializer.Format = SerializationFormat.Xml;
		using var input = new StringReader(xml);
		restored.Serializer.Load(input);
		Assert.That(restored.ChartAreas.Select(area => area.Name), Is.EqualTo(new[] { "Telemetry" }));
		Assert.That(restored.Series.Select(series => series.Name), Is.EqualTo(new[] { "CPU Core 0" }));
		var actual = restored.Series["CPU Core 0"];
		Assert.That(actual.ChartType, Is.EqualTo(chartType));
		Assert.That(actual.ChartArea, Is.EqualTo("Telemetry"));
		Assert.That(actual.Legend, Is.EqualTo("Sensor Legend"));
		Assert.That(actual.Color.ToArgb(), Is.EqualTo(samples.Color.ToArgb()));
		Assert.That(actual.MarkerStyle, Is.EqualTo(MarkerStyle.Diamond));
		Assert.That(actual.BorderWidth, Is.EqualTo(2));
		Assert.That(actual.Points.Select(point => point.YValues[0]), Is.EqualTo(new[] { 23.5, -4.25 }));
		Assert.That(actual.Points.Select(point => point.AxisLabel), Is.EqualTo(new[] { "Core A", "Core B" }));
		Assert.That(restored.BackColor.ToArgb(), Is.EqualTo(original.BackColor.ToArgb()));
		Assert.That(restored.Palette, Is.EqualTo(ChartColorPalette.BrightPastel));
		Assert.That(restored.AntiAliasing, Is.EqualTo(AntiAliasingStyles.Text));
		Assert.That(restored.ChartAreas["Telemetry"].Position.X, Is.EqualTo(10));
		Assert.That(restored.Titles.Single().Text, Is.EqualTo("CPU Core Temperatures"));
		Assert.That(restored.Legends.Single().Name, Is.EqualTo("Sensor Legend"));
		Assert.That(((TextAnnotation)restored.Annotations.Single()).Text, Is.EqualTo("Captured sensor values"));
	}

	[Test]
	public void RuntimeMetadataDoesNotPointAtRemovedCustomDesignerAssemblies() {
		var assembly = typeof(Chart).Assembly;
		Assert.That(assembly.GetReferencedAssemblies().Any(reference => reference.Name.StartsWith("WinForms.DataVisualization.Designer", StringComparison.Ordinal)), Is.False);
		foreach (var type in assembly.GetExportedTypes()) {
			AssertNoCustomDesigner(type, type.GetCustomAttributesData());
			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
				AssertNoCustomDesigner(property, property.GetCustomAttributesData());
		}
	}

	private static void AssertNoCustomDesigner(MemberInfo member, IList<CustomAttributeData> attributes) {
		foreach (var attribute in attributes.Where(attribute => attribute.AttributeType == typeof(EditorAttribute)
			|| attribute.AttributeType == typeof(DesignerAttribute) || attribute.AttributeType == typeof(DesignerSerializerAttribute))) {
			foreach (var argument in attribute.ConstructorArguments.Where(argument => argument.Value is string))
				Assert.That((string)argument.Value, Does.Not.Contain("WinForms.DataVisualization.Designer"), member.DeclaringType?.FullName + "." + member.Name);
		}
	}
}
