// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExtendedListMemoryTests {

	[Test]
	public void ClearReleasesContainedReferencesWhileListRemainsAlive() {
		var (list, reference) = CreateClearedList();
		CollectUnreferencedObjects();
		Assert.That(list, Is.Empty);
		Assert.That(reference.IsAlive, Is.False, "An empty list must not retain its former item through unused array slots.");
		GC.KeepAlive(list);
	}

	[TestCase(0, 1)]
	[TestCase(1, 1)]
	[TestCase(2, 1)]
	[TestCase(1, 2)]
	public void RemoveRangeReleasesRemovedReferencesAndPreservesSurvivorOrder(int index, int count) {
		var (list, references) = CreatePartiallyRemovedList(index, count);
		CollectUnreferencedObjects();
		Assert.That(list.Count, Is.EqualTo(references.Length - count));
		var survivorIndex = 0;
		for (var itemIndex = 0; itemIndex < references.Length; itemIndex++) {
			var removed = itemIndex >= index && itemIndex < index + count;
			Assert.That(references[itemIndex].IsAlive, Is.EqualTo(!removed), "Removed items must be collectible while surviving items remain owned by the list.");
			if (!removed)
				Assert.That(list[survivorIndex++], Is.SameAs(references[itemIndex].Target));
		}
		GC.KeepAlive(list);
	}

	[Test]
	public void ClearReleasesReferencesContainedInsideValueTypeItems() {
		var (list, reference) = CreateClearedValueTypeList();
		CollectUnreferencedObjects();
		Assert.That(list, Is.Empty);
		Assert.That(reference.IsAlive, Is.False, "Value-type items containing references need the same cleanup as reference-type items.");
		GC.KeepAlive(list);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (ExtendedList<object> List, WeakReference Reference) CreateClearedList() {
		var item = new object();
		var list = new ExtendedList<object> { item };
		var reference = new WeakReference(item);
		list.Clear();
		return (list, reference);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (ExtendedList<object> List, WeakReference[] References) CreatePartiallyRemovedList(int index, int count) {
		var items = new[] { new object(), new object(), new object() };
		var list = new ExtendedList<object>();
		list.AddRange(items);
		var references = items.Select(item => new WeakReference(item)).ToArray();
		list.RemoveRange(index, count);
		return (list, references);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (ExtendedList<ReferenceItem> List, WeakReference Reference) CreateClearedValueTypeList() {
		var item = new object();
		var list = new ExtendedList<ReferenceItem> { new ReferenceItem(item) };
		var reference = new WeakReference(item);
		list.Clear();
		return (list, reference);
	}

	private static void CollectUnreferencedObjects() {
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private readonly struct ReferenceItem {
		public ReferenceItem(object value) => Value = value;

		public object Value { get; }
	}
}
