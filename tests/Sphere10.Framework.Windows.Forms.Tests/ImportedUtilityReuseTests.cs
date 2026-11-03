// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;
using Sphere10.Framework.Windows.Forms.Text.FixedLength;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Checks imported reflection and record parsing through their actual consumer APIs.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ImportedUtilityReuseTests {
	[TestCase(nameof(PropertyRow.Text), "Value")]
	[TestCase(nameof(PropertyRow.Optional), null)]
	[TestCase("Missing", "")]
	public void PropertyResolverPreservesValuesAndDistinguishesMissingFromNull(string name, object expected) {
		var resolver = new ReflectionPropertyResolver();

		Assert.That(resolver.ReadValue(new PropertyRow(), name), Is.EqualTo(expected));
	}

	[TestCase('.')]
	[TestCase('$')]
	[TestCase('[')]
	[TestCase('(')]
	[TestCase('\\')]
	[TestCase(' ')]
	[TestCase('|')]
	public void RecordSeparatorsAreLiteralAndRejectDifferentCharacters(char separator) {
		var parser = new LineParser(typeof(SeparatedRecord)) { Separator = separator };
		parser.LoadLine("AB" + separator + "07" + separator);

		Assert.That(parser.GetValue(nameof(SeparatedRecord.Name)), Is.EqualTo("AB"));
		Assert.That(parser.GetValue(nameof(SeparatedRecord.Number)), Is.EqualTo(7));
		parser.LoadLine("ABx07x");
		Assert.That(() => parser.GetValue(nameof(SeparatedRecord.Name)), Throws.TypeOf<RegExException>());
	}

	public class PropertyRow {
		public string Text { get; set; } = "Value";

		public string Optional { get; set; }
	}

	public class SeparatedRecord {
		[Field(0, 2)]
		public string Name { get; set; }

		[Field(1, 2)]
		public int Number { get; set; }
	}
}
