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

/// <summary>Checks the operation arguments delivered by observable collection notifications.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ObservableCallArgumentsTests {
	[TestCase(false)]
	[TestCase(true)]
	public void ExtendedRemovalReportsTheOriginalRangeArguments(bool range) {
		var source = new ExtendedList<string>(new[] { "one", "two", "three", "four" });
		var list = new ObservableExtendedList<string>(source);
		IndexCountCallArgs before = null;
		IndexCountCallArgs after = null;
		list.RemovingRange += (_, args) => before = args.CallArgs;
		list.RemovedRange += (_, args) => after = args.CallArgs;

		if (range)
			list.RemoveRange(1, 2);
		else
			list.RemoveAt(1L);

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Index, Is.EqualTo(1L));
		Assert.That(after.Count, Is.EqualTo(range ? 2L : 1L));
		Assert.That(source, Is.EqualTo(range ? new[] { "one", "four" } : new[] { "one", "three", "four" }));
	}

	[Test]
	public void ListRemovalReportsTheOriginalRangeArguments() {
		var source = new List<string> { "one", "two", "three" };
		var list = new ObservableList<string>(source);
		IndexCountCallArgs before = null;
		IndexCountCallArgs after = null;
		list.RemovingRange += (_, args) => before = args.CallArgs;
		list.RemovedRange += (_, args) => after = args.CallArgs;

		list.RemoveAt(1);

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Index, Is.EqualTo(1L));
		Assert.That(after.Count, Is.EqualTo(1L));
		Assert.That(source, Is.EqualTo(new[] { "one", "three" }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExtendedInsertionReportsItsIndexAndItems(bool range) {
		var source = new ExtendedList<string>();
		source.AddRange(new[] { "one", "four" });
		var list = new ObservableExtendedList<string>(source);
		IndexItemsCallArgs<string> before = null;
		IndexItemsCallArgs<string> after = null;
		list.Inserting += (_, args) => before = args.CallArgs;
		list.Inserted += (_, args) => after = args.CallArgs;

		if (range)
			list.InsertRange(1, new[] { "two", "three" });
		else
			list.Insert(1L, "two");

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Index, Is.EqualTo(1L));
		Assert.That(after.Items, Is.EqualTo(range ? new[] { "two", "three" } : new[] { "two" }));
		Assert.That(source, Is.EqualTo(range ? new[] { "one", "two", "three", "four" } : new[] { "one", "two", "four" }));
	}

	[Test]
	public void ListInsertionReportsItsIndexAndItems() {
		var source = new List<string> { "one", "three" };
		var list = new ObservableList<string>(source);
		IndexItemsCallArgs<string> before = null;
		IndexItemsCallArgs<string> after = null;
		list.Inserting += (_, args) => before = args.CallArgs;
		list.Inserted += (_, args) => after = args.CallArgs;

		list.Insert(1, "two");

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Index, Is.EqualTo(1L));
		Assert.That(after.Items, Is.EqualTo(new[] { "two" }));
		Assert.That(source, Is.EqualTo(new[] { "one", "two", "three" }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExtendedUpdatesReportTheirIndexAndReplacementItems(bool range) {
		var source = new ExtendedList<string>(new[] { "one", "two", "three" });
		var list = new ObservableExtendedList<string>(source);
		IndexItemsCallArgs<string> before = null;
		IndexItemsCallArgs<string> after = null;
		list.Updating += (_, args) => before = args.CallArgs;
		list.Updated += (_, args) => after = args.CallArgs;

		if (range)
			list.UpdateRange(1, new[] { "TWO", "THREE" });
		else
			list.Update(1, "TWO");

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Index, Is.EqualTo(1L));
		Assert.That(after.Items, Is.EqualTo(range ? new[] { "TWO", "THREE" } : new[] { "TWO" }));
		Assert.That(source, Is.EqualTo(range ? new[] { "one", "TWO", "THREE" } : new[] { "one", "TWO", "three" }));
	}

	[Test]
	public void ListUpdatesReportTheirIndexAndReplacementItem() {
		var source = new List<string> { "one", "two" };
		var list = new ObservableList<string>(source);
		IndexItemsCallArgs<string> before = null;
		IndexItemsCallArgs<string> after = null;
		list.Updating += (_, args) => before = args.CallArgs;
		list.Updated += (_, args) => after = args.CallArgs;

		list[1] = "TWO";

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Index, Is.EqualTo(1L));
		Assert.That(after.Items, Is.EqualTo(new[] { "TWO" }));
		Assert.That(source, Is.EqualTo(new[] { "one", "TWO" }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ExtendedReadsReportTheRequestedRangeAlongsideTheResult(bool range) {
		var list = new ObservableExtendedList<string>(new ExtendedList<string>(new[] { "one", "two", "three" }));
		IndexCountCallArgs before = null;
		FetchedByRangeEventArgs<string> after = null;
		list.Fetching += (_, args) => before = args.CallArgs;
		list.Fetched += (_, args) => after = args;

		var values = range ? list.ReadRange(1, 2).ToArray() : new[] { list.Read(1) };

		Assert.That(before, Is.Not.Null);
		Assert.That(after.CallArgs, Is.SameAs(before));
		Assert.That(after.CallArgs.Index, Is.EqualTo(1L));
		Assert.That(after.CallArgs.Count, Is.EqualTo(range ? 2L : 1L));
		Assert.That(values, Is.EqualTo(range ? new[] { "two", "three" } : new[] { "two" }));
		Assert.That(after.Result, Is.EqualTo(values));
	}

	[Test]
	public void ListReadsReportTheRequestedIndexAlongsideTheResult() {
		var list = new ObservableList<string>(new List<string> { "one", "two" });
		IndexCountCallArgs before = null;
		FetchedByRangeEventArgs<string> after = null;
		list.Fetching += (_, args) => before = args.CallArgs;
		list.Fetched += (_, args) => after = args;

		var value = list[1];

		Assert.That(before, Is.Not.Null);
		Assert.That(after.CallArgs, Is.SameAs(before));
		Assert.That(after.CallArgs.Index, Is.EqualTo(1L));
		Assert.That(after.CallArgs.Count, Is.EqualTo(1L));
		Assert.That(value, Is.EqualTo("two"));
		Assert.That(after.Result, Is.EqualTo(new[] { value }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DictionaryRemovalReportsTheKeyAndActualResult(bool missing) {
		var source = new Dictionary<string, int> { ["present"] = 7 };
		var dictionary = new ObservableDictionary<string, int>(source);
		ItemsCallArgs<string> before = null;
		RemovedItemsEventArgs<string> after = null;
		dictionary.RemovingKeys += (_, args) => before = args.CallArgs;
		dictionary.RemovedKeys += (_, args) => after = args;
		var key = missing ? "absent" : "present";

		var removed = dictionary.Remove(key);

		Assert.That(before, Is.Not.Null);
		Assert.That(after.CallArgs, Is.SameAs(before));
		Assert.That(after.CallArgs.Items, Is.EqualTo(new[] { key }));
		Assert.That(removed, Is.EqualTo(!missing));
		Assert.That(after.Result, Is.EqualTo(new[] { removed }));
		Assert.That(source.ContainsKey("present"), Is.EqualTo(missing));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DictionaryReadsReportKeysWithoutLosingTheReturnedValue(bool tryGet) {
		var dictionary = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["present"] = 7 });
		ItemsCallArgs<string> before = null;
		FetchedByItemsEventArgs<string, Tuple<bool, int>> after = null;
		dictionary.Fetching += (_, args) => before = args.CallArgs;
		dictionary.Fetched += (_, args) => after = args;

		int value;
		if (tryGet)
			Assert.That(dictionary.TryGetValue("present", out value), Is.True);
		else
			value = dictionary["present"];

		Assert.That(before, Is.Not.Null);
		Assert.That(after.CallArgs, Is.SameAs(before));
		Assert.That(after.CallArgs.Items, Is.EqualTo(new[] { "present" }));
		Assert.That(value, Is.EqualTo(7));
		Assert.That(after.Result.Single(), Is.EqualTo(Tuple.Create(true, value)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DictionaryKeySearchReportsKeysAlongsideMembership(bool missing) {
		var dictionary = new ObservableDictionary<string, int>(new Dictionary<string, int> { ["present"] = 7 });
		ItemsCallArgs<string> before = null;
		SearchedMembershipEventArgs<string> after = null;
		dictionary.SearchingKeyMembership += (_, args) => before = args.CallArgs;
		dictionary.SearchedKeyMembership += (_, args) => after = args;
		var key = missing ? "absent" : "present";

		var found = dictionary.ContainsKey(key);

		Assert.That(before, Is.Not.Null);
		Assert.That(after.CallArgs, Is.SameAs(before));
		Assert.That(after.CallArgs.Items, Is.EqualTo(new[] { key }));
		Assert.That(found, Is.EqualTo(!missing));
		Assert.That(after.Result, Is.EqualTo(new[] { found }));
	}

	[Test]
	public void ExistingItemPayloadsStillRetainTheirOriginalReference() {
		var list = new ObservableExtendedList<string>();
		ItemsCallArgs<string> before = null;
		ItemsCallArgs<string> after = null;
		list.Adding += (_, args) => before = args.CallArgs;
		list.Added += (_, args) => after = args.CallArgs;

		list.AddRange(new[] { "one", "two" });

		Assert.That(before, Is.Not.Null);
		Assert.That(after, Is.SameAs(before));
		Assert.That(after.Items, Is.EqualTo(new[] { "one", "two" }));
		Assert.That(list, Is.EqualTo(new[] { "one", "two" }));
	}

	[Test]
	public void APreHandlerReplacementIsThePayloadDeliveredAfterTheOperation() {
		var list = new ObservableExtendedList<string>(new ExtendedList<string>(new[] { "one", "two", "three" }));
		var replacement = new IndexCountCallArgs(1, 1);
		IndexCountCallArgs after = null;
		list.RemovingRange += (_, args) => args.CallArgs = replacement;
		list.RemovedRange += (_, args) => after = args.CallArgs;

		list.RemoveAt(1L);

		Assert.That(after, Is.SameAs(replacement));
		Assert.That(list, Is.EqualTo(new[] { "one", "three" }));
	}

	[Test]
	public void ReentrantRemovalsKeepTheirOwnArgumentReferences() {
		var source = new ExtendedList<string>(new[] { "one", "two", "three" });
		var list = new ObservableExtendedList<string>(source);
		var before = new List<IndexCountCallArgs>();
		var after = new List<IndexCountCallArgs>();
		list.RemovingRange += (_, args) => {
			before.Add(args.CallArgs);
			if (args.CallArgs.Index == 0)
				list.RemoveAt(2L);
		};
		list.RemovedRange += (_, args) => after.Add(args.CallArgs);

		list.RemoveAt(0L);

		Assert.That(before, Has.Count.EqualTo(2));
		Assert.That(after, Has.Count.EqualTo(2));
		Assert.That(after[0], Is.SameAs(before[1]));
		Assert.That(after[1], Is.SameAs(before[0]));
		Assert.That(after.Select(args => args.Index), Is.EqualTo(new[] { 2L, 0L }));
		Assert.That(source, Is.EqualTo(new[] { "two" }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void FilteredOrSuppressedNotificationsDoNotChangeTheMutation(bool suppress) {
		var source = new ExtendedList<string>(new[] { "one", "two", "three" });
		var list = new ObservableExtendedList<string>(source) {
			EventFilter = suppress ? EventTraits.All : EventTraits.Add,
			SuppressNotifications = suppress
		};
		var notifications = 0;
		list.RemovingRange += (_, _) => notifications++;
		list.RemovedRange += (_, _) => notifications++;

		list.RemoveRange(1, 2);

		Assert.That(notifications, Is.Zero);
		Assert.That(source, Is.EqualTo(new[] { "one" }));
	}

	[Test]
	public void ArgumentlessClearNotificationsRemainUsable() {
		var source = new ExtendedList<string>(new[] { "one", "two" });
		var list = new ObservableExtendedList<string>(source);
		var notifications = new List<string>();
		list.Clearing += (_, _) => notifications.Add("before");
		list.Cleared += (_, _) => notifications.Add("after");

		list.Clear();

		Assert.That(notifications, Is.EqualTo(new[] { "before", "after" }));
		Assert.That(source, Is.Empty);
	}

	[TestCase(0L, 1L)]
	[TestCase(2147483648L, 4294967297L)]
	[TestCase(long.MaxValue, long.MaxValue)]
	public void IndexCountArgumentsRetainLongValuesFromConstructionAndSetters(long index, long count) {
		var constructed = new IndexCountCallArgs(index, count);
		Assert.That(constructed.Index, Is.EqualTo(index));
		Assert.That(constructed.Count, Is.EqualTo(count));

		var assigned = new IndexCountCallArgs(0, 0) { Index = index, Count = count };
		Assert.That(assigned.Index, Is.EqualTo(index));
		Assert.That(assigned.Count, Is.EqualTo(count));
	}

	[TestCase(0L, false)]
	[TestCase(0L, true)]
	[TestCase(2147483648L, false)]
	[TestCase(2147483648L, true)]
	[TestCase(long.MaxValue, false)]
	[TestCase(long.MaxValue, true)]
	public void IndexItemsArgumentsRetainLongIndicesForBothConstructors(long index, bool range) {
		var args = range ? new IndexItemsCallArgs<string>(index, new[] { "one", "two" }) : new IndexItemsCallArgs<string>(index, "one");
		Assert.That(args.Index, Is.EqualTo(index));
		Assert.That(args.Items, Is.EqualTo(range ? new[] { "one", "two" } : new[] { "one" }));

		args.Index = index;
		Assert.That(args.Index, Is.EqualTo(index));
	}
}
