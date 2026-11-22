// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.Data.SQLBuilder;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class VeryLargeSQLBuilderTests {

	[Test]
	public void StatementsBeyondOneScriptPageSwapOutAndReadBackInOrder() {
		// Each 21-character statement measures 4 + 2 × 21 bytes, so a 100-byte script page holds two and one page stays in memory.
		using var builder = new VeryLargeSQLBuilder(new SqliteSQLBuilder(), 100);
		var expected = Enumerable.Range(0, 25).Select(index => $"SELECT {index:D4} AS VALUE_").ToArray();
		foreach (var sql in expected)
			builder.Emit(sql).EndOfStatement();
		Assert.That(builder.Statements.Select(statement => statement.SQL.Trim().TrimEnd(';')), Is.EqualTo(expected));
	}
}
