// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScopeDecoratorConstructionTests {

	[Test]
	public void NullTransactionalScopeIsRejectedDuringConstruction() {
		Assert.That(() => new TransactionalScopeDecorator<ActionTransactionalScope<object>>(null),
			Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("internalScope"));
	}
}
