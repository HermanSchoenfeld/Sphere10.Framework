// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Data.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SqliteSQLBuilderTests {
	[Test]
	public void CastAutoThroughInterfacePreservesSqlExpression() {
		ISQLBuilder builder = new SqliteSQLBuilder();

		var result = builder.Cast(SQLBuilderStringValueKind.Auto, "1 + 2", typeof(int));

		Assert.That(result, Is.SameAs(builder));
		Assert.That(builder.ToString(), Is.EqualTo("CAST(1 + 2 AS INT)"));
	}
}
