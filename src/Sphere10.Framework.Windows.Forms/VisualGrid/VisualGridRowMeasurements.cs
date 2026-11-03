// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Tracks when a VisualGrid can retain its existing native row heights on the owning UI thread.</summary>
internal sealed class VisualGridRowMeasurements {
	private IDataSource<object> _dataSource;
	private IVisualGridColumn[] _bindings;
	private int[] _columnWidths = Array.Empty<int>();
	private Signature[] _signatures = Array.Empty<Signature>();
	private Geometry _geometry;

	/// <summary>Checks an existing width capture without allocating another array.</summary>
	internal bool Matches(IDataSource<object> dataSource, IVisualGridColumn[] bindings, Geometry geometry, int[] columnWidths) {
		return MatchesLayout(dataSource, bindings, geometry) && _columnWidths.AsSpan().SequenceEqual(columnWidths);
	}

	/// <summary>Checks live native widths without allocating a capture for synchronous sizing.</summary>
	internal bool Matches(IDataSource<object> dataSource, IVisualGridColumn[] bindings, Geometry geometry, SourceGrid.Grid grid) {
		if (!MatchesLayout(dataSource, bindings, geometry) || _columnWidths.Length != grid.ColumnsCount)
			return false;

		for (var column = 0; column < _columnWidths.Length; column++)
			if (_columnWidths[column] != grid.Columns[column].Width)
				return false;
		return true;
	}

	/// <summary>Requires real measurement for unsupported, new or changed row content.</summary>
	internal bool RequiresMeasurement(int row, Signature signature) {
		return signature.ImageHeight < 0 || row >= _signatures.Length || signature != _signatures[row];
	}

	/// <summary>Retains completed captures from the owning grid without copying their arrays.</summary>
	internal void Remember(IDataSource<object> dataSource, IVisualGridColumn[] bindings, Geometry geometry, int[] columnWidths, Signature[] signatures) {
		_dataSource = dataSource;
		_bindings = bindings;
		_geometry = geometry;
		_columnWidths = columnWidths;
		_signatures = signatures;
	}

	/// <summary>Updates row content while retaining the accepted native widths.</summary>
	internal void UpdateSignatures(Signature[] signatures) {
		UpdateSignatures(signatures, _columnWidths);
	}

	/// <summary>Updates row content and its accepted width capture after successful asynchronous sizing.</summary>
	internal void UpdateSignatures(Signature[] signatures, int[] columnWidths) {
		_signatures = signatures;
		_columnWidths = columnWidths;
	}

	/// <summary>Releases retained layout identities and invalidates every previous measurement.</summary>
	internal void Clear() {
		_dataSource = null;
		_bindings = null;
		_columnWidths = Array.Empty<int>();
		_signatures = Array.Empty<Signature>();
		_geometry = default;
	}

	/// <summary>Requires the same source and bindings together with equivalent native row geometry.</summary>
	private bool MatchesLayout(IDataSource<object> dataSource, IVisualGridColumn[] bindings, Geometry geometry) {
		return ReferenceEquals(_dataSource, dataSource) && ReferenceEquals(_bindings, bindings) && _geometry.Equals(geometry);
	}

	/// <summary>Names the native metrics and borrowed visual elements that affect row height.</summary>
	internal readonly record struct Geometry(
		Font GridFont, Font CellFont, int Dpi, int MinimumHeight, Padding Padding, SizeF BorderExtent,
		object TextElement, object ImageElement, ElementsDrawMode DrawMode
	);

	/// <summary>Describes reusable single-line content; a negative image height always requires measurement.</summary>
	internal readonly record struct Signature(int ImageHeight, bool HasText);
}
