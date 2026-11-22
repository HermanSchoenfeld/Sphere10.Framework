// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.NUnit;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemoryPagedListCacheTests {

	[Test]
	public void DisposedListRejectsFurtherAccess() {
		var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		using (list)
			list.Add(1);
		Assert.That(list.Disposed, Is.True);
		Assert.That(() => list.Add(2), Throws.TypeOf<ObjectDisposedException>());
	}

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

	[Test]
	[TestCase(CacheReapPolicy.LeastUsed, 1L)]
	[TestCase(CacheReapPolicy.LongestIdle, 2L)]
	public void PageReapPolicyChoosesTheEvictedPage(CacheReapPolicy policy, long retainedPage) {
		using var list = new MemoryPagedList<int>(16, 2 * 16, sizeof(int), policy);
		list.AddRange(Enumerable.Range(0, 12));
		Assert.That(list.PageReapPolicy, Is.EqualTo(policy));
		AssertEx.HasLoadedPages(list, 1, 2);
		// Page 1 is used most, page 2 most recently; loading page 0 evicts one of them.
		for (var read = 0; read < 8; read++)
			Assert.That(list[4], Is.EqualTo(4));
		Assert.That(list[8], Is.EqualTo(8));
		Assert.That(list[0], Is.EqualTo(0));
		AssertEx.HasLoadedPages(list, 0, retainedPage);
	}

	[Test]
	public void LargeCollectionWithoutASizerMeasuresItemsWithTheirDefaultSerializer() {
		using var collection = new LargeCollection<int>(4 * sizeof(int), 2 * 4 * sizeof(int));
		var values = Enumerable.Range(1, 10).ToArray();
		foreach (var value in values)
			collection.Add(value);
		Assert.That(collection, Is.EqualTo(values));
		Assert.That(collection.Count, Is.EqualTo(values.Length));
		Assert.That(collection.Pages.Count, Is.EqualTo(3), "Four-byte integers fill each 16-byte page four at a time.");
	}

	[Test]
	public void EveryMemoryPagedEntryPointTakesItsPageReapPolicy() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var deleteDirectory = Tools.Scope.ExecuteOnDispose(() => Tools.FileSystem.DeleteDirectory(directory));
		using (var buffer = new MemoryPagedBuffer(16, 32))
			Assert.That(buffer.PageReapPolicy, Is.EqualTo(Sphere10FrameworkDefaults.PageCacheReapPolicy));
		using (var buffer = new MemoryPagedBuffer(16, 32, CacheReapPolicy.LeastUsed))
			Assert.That(buffer.PageReapPolicy, Is.EqualTo(CacheReapPolicy.LeastUsed));
		using (var stream = new MemoryPagedStream(new MemoryStream(), 16, 32, pageReapPolicy: CacheReapPolicy.Oldest))
			Assert.That(((MemoryPagedBuffer)stream.Buffer).PageReapPolicy, Is.EqualTo(CacheReapPolicy.Oldest));
		using (var collection = new LargeCollection<int>(16, 32, _ => sizeof(int), CacheReapPolicy.Largest))
			Assert.That(collection.PageReapPolicy, Is.EqualTo(CacheReapPolicy.Largest));
		using (var collection = new LargeCollection<int>(16, 32))
			Assert.That(collection.PageReapPolicy, Is.EqualTo(Sphere10FrameworkDefaults.PageCacheReapPolicy));
		using (var file = new FileMappedBuffer(PagedFileDescriptor.From(Path.Combine(directory, "Mapped.dat"), 16, 32, CacheReapPolicy.LeastUsed)))
			Assert.That(file.PageReapPolicy, Is.EqualTo(CacheReapPolicy.LeastUsed));
		using (var transactional = new TransactionalFileMappedBuffer(TransactionalFileDescriptor.From(Path.Combine(directory, "Transactional.dat"), directory, 16, 32, CacheReapPolicy.Smallest)))
			Assert.That(transactional.PageReapPolicy, Is.EqualTo(CacheReapPolicy.Smallest));
		Assert.That(() => new MemoryPagedBuffer(16, 32, CacheReapPolicy.None), Throws.ArgumentException);
	}
}
