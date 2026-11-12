// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.DataVisualization;
using Sphere10.Framework.Windows.Forms.DataVisualization.ChartTypes;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ChartTypeRegistryTests {
	[Test]
	public void BuiltInRendererIsCreatedOnceAcrossEnumAndCaseInsensitiveName([Values] SeriesChartType chartType) {
		using var chart = new Chart();
		var registry = (ChartTypeRegistry)chart.GetService(typeof(ChartTypeRegistry));
		var renderer = registry.GetChartType(chartType);
		var name = ChartTypeNames.GetChartTypeName(chartType);
		Assert.That(renderer.Name, Is.EqualTo(name).IgnoreCase);
		Assert.That(registry.GetChartType(name.ToUpperInvariant()), Is.SameAs(renderer));
	}

	[Test]
	public void RegisteringSameTypePreservesCachedRendererAndDifferentTypeIsRejected() {
		using var registry = new ChartTypeRegistry();
		registry.Register("Example", typeof(ColumnChart));
		var renderer = registry.GetChartType("Example");
		registry.Register("EXAMPLE", typeof(ColumnChart));
		Assert.That(registry.GetChartType("example"), Is.SameAs(renderer));
		Assert.That(() => registry.Register("Example", typeof(LineChart)), Throws.TypeOf<ArgumentException>());
	}

	[Test]
	public void InvalidRendererAndUnknownNameAreRejected() {
		using var registry = new ChartTypeRegistry();
		Assert.That(() => registry.Register("Invalid", typeof(object)), Throws.TypeOf<ArgumentException>());
		Assert.That(() => registry.GetChartType("Missing"), Throws.TypeOf<ArgumentException>());
	}
}
