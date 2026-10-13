// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemoryPagedListCacheTests {

	[Test]
	public void CacheBudgetAboveFourGiBRemainsLongWithoutAllocatingTheWholeBudget() {
		const long budget = 1L << 32;
		using var list = new MemoryPagedList<int>(64, budget, sizeof(int));
		var values = Enumerable.Range(0, 64).ToArray();
		list.AddRange(values);
		Assert.That(list.MaxMemory, Is.EqualTo(budget));
		Assert.That(list.ReadRange(0, values.Length), Is.EqualTo(values));
		Assert.That(list.CurrentOpenPages, Is.EqualTo(4));
	}

	[Test]
	public void DirtyPagesAreSavedBeforeReapingAndReloadTheirUpdatedValues() {
		using var list = new MemoryPagedList<int>(16, 128, sizeof(int));
		var savedPages = new HashSet<long>();
		var unloadedPages = new HashSet<long>();
		list.PageSaved += (_, page) => savedPages.Add(page.Number);
		list.PageUnloaded += (_, page) => unloadedPages.Add(page.Number);
		var values = Enumerable.Range(0, 64).ToArray();
		list.AddRange(values);
		Assert.That(list.CurrentOpenPages, Is.LessThanOrEqualTo(8));
		Assert.That(savedPages, Is.Not.Empty);
		Assert.That(unloadedPages, Is.SubsetOf(savedPages), "Dirty pages must be saved before their backing data is unloaded.");

		for (var index = 0; index < values.Length; index++) {
			values[index] *= 17;
			list[index] = values[index];
		}
		list.Flush();
		Assert.That(list.CurrentOpenPages, Is.Zero);
		Assert.That(list.ReadRange(0, values.Length), Is.EqualTo(values));
		Assert.That(list.CurrentOpenPages, Is.LessThanOrEqualTo(8));
	}
}
