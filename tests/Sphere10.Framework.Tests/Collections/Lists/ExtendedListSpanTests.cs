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
public class ExtendedListSpanTests {

	[Test]
	public void StackAllocatedRangesRoundTripThroughCallerOwnedStorage() {
		var list = new ExtendedList<int>(0, 3, 32);
		ReadOnlySpan<int> original = stackalloc int[] { 1, 2, 3, 4 };
		ReadOnlySpan<int> replacement = stackalloc int[] { 20, 30 };
		ReadOnlySpan<int> inserted = stackalloc int[] { 5, 6 };
		list.AddRange(original);
		list.UpdateRange(1, replacement);
		list.InsertRange(2, inserted);
		Span<int> destination = stackalloc int[6];
		list.ReadRange(0, destination);
		Assert.That(destination.ToArray(), Is.EqualTo(new[] { 1, 20, 5, 6, 30, 4 }));
	}

	[Test]
	public void SpanInsertionPreservesSourceAliasedToBackingArray() {
		var backing = new[] { 0, 1, 2, 3, 4, 5 };
		var list = new ExtendedList<int>(backing);
		list.RemoveRange(4, 2);
		list.InsertRange(1, backing.AsSpan(2, 2));
		Assert.That(list, Is.EqualTo(new[] { 0, 2, 3, 1, 2, 3 }));
	}

	[Test]
	public void SpanUpdateSupportsOverlappingSourceAndDestination() {
		var backing = new[] { 0, 1, 2, 3, 4 };
		var list = new ExtendedList<int>(backing);
		list.UpdateRange(1, backing.AsSpan(0, 4));
		Assert.That(list, Is.EqualTo(new[] { 0, 0, 1, 2, 3 }));
	}

	[Test]
	public void SpanAppendSupportsSourceAliasedToBackingArray() {
		var backing = new[] { 0, 1, 2, 3, 4, 5 };
		var list = new ExtendedList<int>(backing);
		list.RemoveRange(3, 3);
		list.AddRange(backing.AsSpan(0, 3));
		Assert.That(list, Is.EqualTo(new[] { 0, 1, 2, 0, 1, 2 }));
	}

	[TestCase(4)]
	[TestCase(8)]
	public void SelfInsertionPreservesSourceBeforeShiftingOrGrowing(int capacity) {
		var list = new ExtendedList<int>(capacity, 4, 8) { 1, 2, 3, 4 };
		list.InsertRange(2, list);
		Assert.That(list, Is.EqualTo(new[] { 1, 2, 1, 2, 3, 4, 3, 4 }));
	}

	[TestCase(4)]
	[TestCase(8)]
	public void SelfAppendPreservesSourceBeforeGrowing(int capacity) {
		var list = new ExtendedList<int>(capacity, 4, 8) { 1, 2, 3, 4 };
		list.AddRange(list);
		Assert.That(list, Is.EqualTo(new[] { 1, 2, 3, 4, 1, 2, 3, 4 }));
	}

	[TestCase(-1L)]
	[TestCase(4L)]
	[TestCase(2147483648L)]
	public void DestinationReadRejectsRangesBeyondLogicalContents(long index) {
		var list = new ExtendedList<int>(16) { 1, 2, 3 };
		Assert.That(() => list.ReadRange(index, new int[1].AsSpan()), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public void RangeReadsRetainSnapshotOwnership() {
		var list = new ExtendedList<int> { 1, 2, 3 };
		var snapshot = list.ReadRange(0, 3);
		list.Update(1, 9);
		Assert.That(snapshot, Is.EqualTo(new[] { 1, 2, 3 }));
		Assert.That(list, Is.EqualTo(new[] { 1, 9, 3 }));
	}

	[Test]
	public void FailingEnumerableIsMaterializedBeforeMutatingList() {
		var list = new ExtendedList<int> { 1, 2, 3 };
		using var enumerator = list.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(() => list.AddRange(FailingItems()), Throws.TypeOf<InvalidOperationException>());
		Assert.That(list, Is.EqualTo(new[] { 1, 2, 3 }));
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo(2));
	}

	[Test]
	public void ScalarSearchAndRemovalPreserveCustomComparerAndFirstMatch() {
		var list = new ExtendedList<string>(StringComparer.OrdinalIgnoreCase) { "first", "FIRST", "last" };
		Assert.That(list.Contains("FiRsT"), Is.True);
		Assert.That(list.IndexOfL("FIRST"), Is.Zero);
		Assert.That(list.Remove("FIRST"), Is.True);
		Assert.That(list, Is.EqualTo(new[] { "FIRST", "last" }));
		Assert.That(list.IndexOfL("absent"), Is.EqualTo(-1));
		Assert.That(list.Remove("absent"), Is.False);
	}

	[TestCase("Add")]
	[TestCase("Update")]
	[TestCase("Insert")]
	[TestCase("Remove")]
	[TestCase("SpanUpdate")]
	public void EnumerationDetectsMutations(string operation) {
		var list = new ExtendedList<int> { 1, 2, 3 };
		using var enumerator = list.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		switch (operation) {
			case "Add":
				list.Add(4);
				break;
			case "Update":
				list.Update(1, 4);
				break;
			case "Insert":
				list.Insert(1L, 4);
				break;
			case "Remove":
				list.Remove(2);
				break;
			default:
				list.UpdateRange(1, new[] { 4 }.AsSpan());
				break;
		}
		Assert.That(() => enumerator.MoveNext(), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void CopyToWritesOnlyRequestedDestinationSegment() {
		var list = new ExtendedList<int> { 1, 2, 3 };
		var destination = new[] { -1, -1, -1, -1, -1 };
		list.CopyTo(destination, 1);
		Assert.That(destination, Is.EqualTo(new[] { -1, 1, 2, 3, -1 }));
	}

	[Test]
	public void DerivedRangeOverridesRemainCanonicalForScalarAndArrayCalls() {
		var implementation = new RecordingExtendedList();
		ExtendedList<int> list = implementation;
		list.Add(1);
		list.AddRange(new[] { 2, 3 });
		list.Update(0, 4);
		list.UpdateRange(0, new[] { 5 });
		list.Insert(1L, 6);
		list.InsertRange(1, new[] { 7 });
		Assert.That(list.Read(0), Is.EqualTo(5));
		Assert.That(list.Contains(6), Is.True);
		Assert.That(list.IndexOfL(6), Is.EqualTo(2));
		Assert.That(list.Remove(6), Is.True);
		Assert.That(implementation.Operations, Is.EqualTo(new[] {
			"AddRange", "AddRange", "UpdateRange", "UpdateRange", "InsertRange", "InsertRange", "ReadRange", "IndexOfRange", "IndexOfRange", "IndexOfRange"
		}));
		implementation.ClearOperations();
		Assert.That(list.ToArray(), Is.EqualTo(new[] { 5, 7, 2, 3 }));
		Assert.That(implementation.Operations, Is.EqualTo(Enumerable.Repeat("ReadRange", 4)));
	}

	[Test]
	public void ScalarOperationsAndCopyingRemainEquivalentWhenInherited([Values] bool inherited) {
		RangedListBase<int> list = inherited ? new InheritedExtendedList(1025) : new ExtendedList<int>(1025);
		var copied = new int[1024];
		var checksum = ExerciseScalarOperations(list, copied);
		Assert.That(checksum, Is.GreaterThan(0));
		Assert.That(copied, Is.EqualTo(Enumerable.Range(1, 1024)));
	}

	[Test]
	public void ContiguousEnumerableRangesDoNotAllocateIntermediateArrays([Values] bool inherited) {
		var items = Enumerable.Range(0, 1024).ToList();
		ExtendedList<int> list = inherited ? new InheritedExtendedList(items.Count) : new ExtendedList<int>(items.Count);
		var copied = new int[items.Count];
		list.AddRange(items);
		list.UpdateRange(0, items);
		list.ReadRange(0, copied.AsSpan());
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var repeat = 0; repeat < 64; repeat++) {
			list.Clear();
			list.AddRange(items);
			list.UpdateRange(0, items);
			list.ReadRange(0, copied.AsSpan());
		}
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.WriteLine($"ExtendedList contiguous range workload (64 x 1024 items): {allocated:N0} allocated bytes.");
		Assert.That(copied, Is.EqualTo(items));
		Assert.That(allocated, Is.LessThan(1024), "Contiguous sources must not be materialized before span copies.");
	}

	private static long ExerciseScalarOperations(RangedListBase<int> list, int[] copied) {
		list.Clear();
		for (var index = 0; index < copied.Length; index++)
			list.Add(index);
		var checksum = 0L;
		for (var index = 0; index < copied.Length; index++) {
			checksum += list.Read(index);
			list.Update(index, index + 1);
			checksum += list.IndexOfL(1);
			if (list.Contains(1))
				checksum++;
		}
		list.Insert(0L, -1);
		if (list.Remove(-1))
			checksum++;
		foreach (var item in list)
			checksum += item;
		list.CopyTo(copied, 0);
		return checksum;
	}

	private static IEnumerable<int> FailingItems() {
		yield return 4;
		throw new InvalidOperationException("Source failed during enumeration");
	}

	private sealed class InheritedExtendedList : ExtendedList<int> {
		public InheritedExtendedList(long capacity) : base(capacity) {
		}
	}

	private sealed class RecordingExtendedList : ExtendedList<int> {
		private readonly List<string> _operations = new();

		public string[] Operations => _operations.ToArray();

		public void ClearOperations() => _operations.Clear();

		public override void AddRange(IEnumerable<int> items) {
			_operations.Add("AddRange");
			base.AddRange(items);
		}

		public override void UpdateRange(long index, IEnumerable<int> items) {
			_operations.Add("UpdateRange");
			base.UpdateRange(index, items);
		}

		public override void InsertRange(long index, IEnumerable<int> items) {
			_operations.Add("InsertRange");
			base.InsertRange(index, items);
		}

		public override IEnumerable<int> ReadRange(long index, long count) {
			_operations.Add("ReadRange");
			return base.ReadRange(index, count);
		}

		public override long[] IndexOfRange(IEnumerable<int> items) {
			_operations.Add("IndexOfRange");
			return base.IndexOfRange(items);
		}
	}
}
