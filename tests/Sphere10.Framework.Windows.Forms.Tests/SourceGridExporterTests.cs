// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Exporter;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks raw grid export through the shared CSV escaping rules.</summary>
[TestFixture]
[Apartment(ApartmentState.STA)]
public class SourceGridExporterTests {

	/// <summary>Uses common CSV escaping while preserving flattened grid display-text lines.</summary>
	[TestCase("plain", "plain")]
	[TestCase("first,second", "\"first,second\"")]
	[TestCase("say \"hello\"", "\"say \"\"hello\"\"\"")]
	[TestCase("first\r\nsecond", "first second")]
	[TestCase("first\nsecond", "first second")]
	[TestCase("first\rsecond", "first second")]
	public void CsvEscapesStructuralCharactersAndPreservesItsFlatLineConvention(string value, string expected) {
		using var grid = new Grid();
		grid.Redim(1, 2);
		grid[0, 0] = new Cell(value);
		grid[0, 1] = new Cell("tail");
		using var writer = new StringWriter();

		new CSV { FieldSeparator = ",", LineSeparator = "\n" }.Export(grid, writer);

		Assert.That(writer.ToString(), Is.EqualTo(expected + ",tail\n"));
	}

	/// <summary>Retains custom row/field separators, empty cells and caller ownership of the writer.</summary>
	[TestCase("<row>")]
	[TestCase("")]
	[TestCase(null)]
	public void CsvPreservesCustomFramingAndLeavesTheWriterOpen(string lineSeparator) {
		using var grid = new Grid();
		grid.Redim(2, 3);
		grid[0, 0] = new Cell("left||right");
		grid[0, 1] = new Cell(null);
		grid[1, 0] = new Cell("next\r\nline");
		grid[1, 1] = new Cell("say \"hello\"");
		grid[1, 2] = new Cell("tail");
		using var writer = new StringWriter();

		new CSV { FieldSeparator = "||", LineSeparator = lineSeparator }.Export(grid, writer);
		writer.Write("<open>");

		var expected = "\"left||right\"||||" + lineSeparator + "next line||\"say \"\"hello\"\"\"||tail" + lineSeparator + "<open>";
		Assert.That(writer.ToString(), Is.EqualTo(expected));
	}

	/// <summary>Existing derived exporters retain direct field formatting through the original writer.</summary>
	[Test]
	public void CsvCallsTheProtectedCellFormatterForEveryPosition() {
		using var grid = new Grid();
		grid.Redim(2, 2);
		using var writer = new StringWriter();
		var exporter = new CellFormatterCsv { Destination = writer, FieldSeparator = ",", LineSeparator = "\n" };

		exporter.Export(grid, writer);

		Assert.That(writer.ToString(), Is.EqualTo("\"0,0\",\"0,1\"\n\"1,0\",\"1,1\"\n"));
		Assert.That(exporter.Positions, Is.EqualTo(new[] { new Position(0, 0), new Position(0, 1), new Position(1, 0), new Position(1, 1) }));
	}

	/// <summary>An empty grid emits nothing, while zero-column rows retain their line separators.</summary>
	[TestCase(0, 0, "")]
	[TestCase(2, 0, "\n\n")]
	public void CsvFramesEmptyRowsWithoutFabricatingFields(int rows, int columns, string expected) {
		using var grid = new Grid();
		grid.Redim(rows, columns);
		using var writer = new StringWriter();

		new CSV { FieldSeparator = ",", LineSeparator = "\n" }.Export(grid, writer);

		Assert.That(writer.ToString(), Is.EqualTo(expected));
	}

	#region Inner Types

	// Exercises the existing customization hook without depending on default field serialization.
	private sealed class CellFormatterCsv : CSV {

		/// <summary>The caller's writer expected by each cell callback.</summary>
		public TextWriter Destination { get; set; }

		/// <summary>The grid positions supplied to the protected customization hook.</summary>
		public List<Position> Positions { get; } = new();

		/// <summary>Writes an already-escaped custom field directly into the supplied writer.</summary>
		protected override void ExportCSVCell(CellContext context, TextWriter stream) {
			Assert.That(stream, Is.SameAs(Destination));
			Positions.Add(context.Position);
			stream.Write($"\"{context.Position.Row},{context.Position.Column}\"");
		}
	}

	#endregion
}
