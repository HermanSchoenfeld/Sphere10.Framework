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

	[Test]
	public void SavingCallbacksObserveTheResidentPageAndCaptureReentrantChanges() {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		var expected = new[] { 1, 2, 3, 4 };
		list.AddRange(expected);
		var residentPage = list.Pages.Single();
		var saves = 0;
		var loads = 0;
		list.PageLoaded += (_, _) => loads++;
		list.PageSaving += (_, page) => {
			saves++;
			Assert.That(page, Is.SameAs(residentPage));
			Assert.That(list.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(page.State, Is.EqualTo(PageState.Loaded));
			Assert.That(list.ReadRange(0, expected.Length), Is.EqualTo(expected));
			expected[1] = 42;
			list[1] = expected[1];
		};
		list.PageSaved += (_, page) => {
			Assert.That(list.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(page.State, Is.EqualTo(PageState.Loaded));
			Assert.That(page.Dirty, Is.False);
			Assert.That(list[1], Is.EqualTo(expected[1]));
		};
		list.PageUnloading += (_, _) => Assert.That(list.CurrentOpenPages, Is.Zero);

		list.Flush();
		Assert.That(saves, Is.EqualTo(1));
		Assert.That(loads, Is.Zero, "Reentrant reads of the saving page must use its existing cache entry.");
		Assert.That(list.CurrentOpenPages, Is.Zero);
		Assert.That(residentPage.State, Is.EqualTo(PageState.Unloaded));
		Assert.That(list.ReadRange(0, expected.Length), Is.EqualTo(expected));
	}

	[Test]
	public void NestedFlushSavesOtherPagesWithoutRecursivelySavingTheCurrentPage() {
		using var list = new MemoryPagedList<byte>(16, 32, sizeof(byte));
		var expected = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();
		list.AddRange(expected);
		var saves = new Dictionary<long, int>();
		var nested = false;
		list.PageSaving += (_, page) => {
			saves[page.Number] = saves.GetValueOrDefault(page.Number) + 1;
			Assert.That(saves[page.Number], Is.EqualTo(1), "A nested flush must not recursively save an in-progress page.");
			if (nested)
				return;
			nested = true;
			Assert.That(list.CurrentOpenPages, Is.EqualTo(2));
			list.Flush();
			Assert.That(list.CurrentOpenPages, Is.EqualTo(1));
			Assert.That(page.State, Is.EqualTo(PageState.Loaded));
			Assert.That(page.Dirty, Is.True);
			Assert.That(list[page.StartIndex], Is.EqualTo(expected[page.StartIndex]));
		};

		list.Flush();
		Assert.That(nested, Is.True);
		Assert.That(saves.Count, Is.EqualTo(2));
		Assert.That(saves.Values, Is.All.EqualTo(1));
		Assert.That(list.CurrentOpenPages, Is.Zero);
		Assert.That(list.Pages.All(page => page.State == PageState.Unloaded), Is.True);
		Assert.That(list.ReadRange(0, expected.Length), Is.EqualTo(expected));
	}

	[Test]
	public void SavedNotificationFailureKeepsTheCleanPageCachedUntilRetry() {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		var expected = new[] { 1, 2, 3, 4 };
		list.AddRange(expected);
		var saves = 0;
		list.PageSaving += (_, _) => saves++;
		EventHandlerEx<object, IMemoryPage<int>> failSaved = (_, _) => throw new InvalidOperationException("Injected PageSaved failure");
		list.PageSaved += failSaved;

		Assert.That(() => list.Flush(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Injected PageSaved failure"));
		Assert.That(list.CurrentOpenPages, Is.EqualTo(1));
		Assert.That(list.Pages[0].State, Is.EqualTo(PageState.Loaded));
		Assert.That(list.Pages[0].Dirty, Is.False);
		Assert.That(list.ReadRange(0, expected.Length), Is.EqualTo(expected));
		list.PageSaved -= failSaved;
		list.Flush();
		Assert.That(saves, Is.EqualTo(1), "Retrying eviction of an already saved page must not save it again.");
		Assert.That(list.CurrentOpenPages, Is.Zero);
		Assert.That(list.ReadRange(0, expected.Length), Is.EqualTo(expected));
	}

	[Test]
	public void SavedCallbackChangesRemainDirtyAndCanBeSavedOnRetry() {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		var expected = new[] { 1, 2, 3, 4 };
		list.AddRange(expected);
		var saves = 0;
		list.PageSaving += (_, _) => saves++;
		EventHandlerEx<object, IMemoryPage<int>> changeSavedPage = (_, _) => list[0] = 99;
		list.PageSaved += changeSavedPage;

		Assert.That(() => list.Flush(), Throws.TypeOf<InvalidOperationException>());
		Assert.That(list.CurrentOpenPages, Is.EqualTo(1));
		Assert.That(list.Pages[0].State, Is.EqualTo(PageState.Loaded));
		Assert.That(list.Pages[0].Dirty, Is.True);
		Assert.That(list[0], Is.EqualTo(99));
		list.PageSaved -= changeSavedPage;
		list.Flush();
		expected[0] = 99;
		Assert.That(saves, Is.EqualTo(2));
		Assert.That(list.CurrentOpenPages, Is.Zero);
		Assert.That(list.ReadRange(0, expected.Length), Is.EqualTo(expected));
	}
}
