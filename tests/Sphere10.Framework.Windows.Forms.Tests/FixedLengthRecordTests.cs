// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.Text.FixedLength;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies the fixed-length record domain owned by the native library.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FixedLengthRecordTests {

	[Test]
	public void FixedLengthRecordsRoundTripWithPaddingAndRejectOversizedFields() {
		var writer = new LineWriter(typeof(FixedRecord));
		var record = new FixedRecord { Name = "AB", Number = 7 };
		var line = writer.CreateLineFromClass(record);
		Assert.That(line, Is.EqualTo("AB  007"));

		var parser = new LineParser(typeof(FixedRecord));
		parser.LoadLine(line);
		Assert.That(parser.GetValue(nameof(FixedRecord.Name)), Is.EqualTo("AB"));
		Assert.That(parser.GetValue(nameof(FixedRecord.Number)), Is.EqualTo(7));
		record.Name = "Too long";
		Assert.That(() => writer.CreateLineFromClass(record), Throws.TypeOf<FieldStringConvertException>());
	}

	public sealed class FixedRecord {
		[Field(0, 4)]
		public string Name { get; set; }

		[Field(1, 3)]
		public int Number { get; set; }
	}
}
