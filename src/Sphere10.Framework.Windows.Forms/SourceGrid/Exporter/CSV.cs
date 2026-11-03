// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

/// <summary>
/// Exports grid display text through the shared CSV row and field writers.
/// </summary>
public class CSV {

	/// <summary>Uses the current culture’s list separator and the platform line separator.</summary>
	public CSV() {
		FieldSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
		LineSeparator = Environment.NewLine;
	}

	/// <summary>The separator written between fields.</summary>
	public string FieldSeparator { get; set; }

	/// <summary>The separator written after each row; null or empty omits it.</summary>
	public string LineSeparator { get; set; }

	/// <summary>Writes every grid row and leaves the caller’s writer open.</summary>
	public virtual void Export(GridVirtual grid, TextWriter stream) {
		for (var row = 0; row < grid.Rows.Count; row++)
			Tools.Data.WriteCsvRow(stream, Enumerable.Range(0, grid.Columns.Count), (column, destination) => {
				var cell = grid.GetCell(row, column);
				var context = new CellContext(grid, new Position(row, column), cell);
				ExportCSVCell(context, destination);
			}, FieldSeparator, LineSeparator);
	}

	/// <summary>Flattens display-text line endings before the shared writer escapes one field.</summary>
	protected virtual void ExportCSVCell(CellContext context, TextWriter stream) {
		var text = context.Cell == null ? string.Empty : context.DisplayText ?? string.Empty;
		text = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

		// Reuse the shared escaping rules for separators and embedded quotation marks.
		Tools.Data.WriteCsvField(stream, text, FieldSeparator);
	}
}
