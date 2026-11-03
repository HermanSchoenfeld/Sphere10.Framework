// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Data;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Data.Csv;

namespace Sphere10.Framework.Data.Tests;

/// <summary>Checks shared CSV export framing, field escaping and DataTable compatibility.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CsvExportTests {

	/// <summary>Preserves raw headers, quotes, delimiters and embedded line endings through the existing reader.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public void TableRoundTripsStructuralCharacters(bool includeHeader) {
		using var table = new DataTable();
		table.Columns.Add("First, name");
		table.Columns.Add("Quoted \"name\"");
		table.Columns.Add("Notes");
		table.Rows.Add("first,second", "say \"hello\"", "first\r\nsecond");
		table.Rows.Add(DBNull.Value, string.Empty, "tail");

		var csv = table.ToCSV(includeHeader: includeHeader);

		var header = includeHeader ? "\"First, name\",\"Quoted \"\"name\"\"\",Notes" + Environment.NewLine : string.Empty;
		Assert.That(csv, Is.EqualTo(header + "\"first,second\",\"say \"\"hello\"\"\",\"first\r\nsecond\"" + Environment.NewLine + ",,tail" + Environment.NewLine));
		using var reader = new CsvReader(new StringReader(csv), includeHeader);
		Assert.That(reader.ReadNextRecord(), Is.True);
		Assert.That(reader[0], Is.EqualTo("first,second"));
		Assert.That(reader[1], Is.EqualTo("say \"hello\""));
		Assert.That(reader[2], Is.EqualTo("first\r\nsecond"));
		Assert.That(reader.ReadNextRecord(), Is.True);
		Assert.That(reader[0], Is.Empty);
		Assert.That(reader[1], Is.Empty);
		Assert.That(reader[2], Is.EqualTo("tail"));
		Assert.That(reader.ReadNextRecord(), Is.False);
	}

	/// <summary>Writes multi-character separators only between complete fields.</summary>
	[Test]
	public void MultiCharacterDelimiterDoesNotLeaveATrailingFragment() {
		using var table = new DataTable();
		table.Columns.Add("First||name");
		table.Columns.Add("Second");
		table.Rows.Add("value||with separator", "single|character");

		var csv = table.ToCSV("||");

		Assert.That(csv, Is.EqualTo("\"First||name\"||Second" + Environment.NewLine + "\"value||with separator\"||single|character" + Environment.NewLine));
	}

	/// <summary>Keeps the existing current-culture conversion before escaping typed DataTable values.</summary>
	[Test]
	[SetCulture("fr-FR")]
	public void TypedValuesUseTheCurrentCulture() {
		using var table = new DataTable();
		table.Columns.Add("Amount", typeof(decimal));
		table.Columns.Add("Captured", typeof(DateTime));
		table.Rows.Add(12.5m, new DateTime(2025, 12, 31, 13, 14, 15));

		var csv = table.ToCSV(includeHeader: false);

		Assert.That(csv, Is.EqualTo("\"12,5\",31/12/2025 13:14:15" + Environment.NewLine));
	}

	/// <summary>An empty schema emits one empty line per requested header or data row.</summary>
	[TestCase(false, 0)]
	[TestCase(true, 0)]
	[TestCase(false, 2)]
	[TestCase(true, 2)]
	public void ZeroColumnTablesHaveDefinedRowFraming(bool includeHeader, int rowCount) {
		using var table = new DataTable();
		for (var row = 0; row < rowCount; row++)
			table.Rows.Add(table.NewRow());

		var csv = table.ToCSV(includeHeader: includeHeader);

		Assert.That(csv, Is.EqualTo(string.Concat(Enumerable.Repeat(Environment.NewLine, rowCount + (includeHeader ? 1 : 0)))));
	}

	/// <summary>The caller retains its writer and can choose both delimiter and line separator.</summary>
	[Test]
	public void SharedWriterLeavesTheDestinationOpen() {
		using var destination = new StringWriter();

		Tools.Data.WriteCsvRow(destination, new[] { "first;second", "say \"hello\"", null },
			(value, writer) => Tools.Data.WriteCsvField(writer, value, ";"), ";", "<row>");
		destination.Write("still open");

		Assert.That(destination.ToString(), Is.EqualTo("\"first;second\";\"say \"\"hello\"\"\";<row>still open"));
	}
}
