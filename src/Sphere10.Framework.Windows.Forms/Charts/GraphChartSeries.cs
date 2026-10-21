// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>An owned numeric series independent of any sampler or application model.</summary>
public sealed class GraphChartSeries {
	private readonly GraphChartPoint[] _points;

	public GraphChartSeries(string name, GraphChartPoint[] points, Color color = default, GraphChartStyle? style = null) {
		Guard.ArgumentNotNullOrWhitespace(name, nameof(name));
		Guard.ArgumentNotNull(points, nameof(points));
		Guard.Argument(!style.HasValue || Enum.IsDefined(style.Value), nameof(style), "Unknown chart style");
		_points = new GraphChartPoint[points.Length];
		for (var index = 0; index < points.Length; index++) {
			var point = points[index];
			Guard.Argument(double.IsFinite(point.X), nameof(points), "X coordinates must be finite");
			_points[index] = point with { Y = point.Y.HasValue && double.IsFinite(point.Y.Value) ? point.Y : null };
		}
		Name = name;
		Color = color;
		Style = style;
	}

	public string Name { get; }

	public GraphChartPoint[] Points => (GraphChartPoint[])_points.Clone();

	public Color Color { get; }

	public GraphChartStyle? Style { get; }

	internal ReadOnlySpan<GraphChartPoint> GetPoints() => _points;
}
