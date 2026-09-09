// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemorizingIteratorTests {
	[Test]
	public void ConstructorRejectsNullSource() {
		Assert.That(() => new MemorizingIterator<int>(null), Throws.ArgumentNullException);
	}

	[Test]
	public void DefaultInstanceReportsMissingSource() {
		var iterator = default(MemorizingIterator<int>);
		Assert.That(() => iterator.GetEnumerator(), Throws.InvalidOperationException);
		Assert.That(() => iterator.Count, Throws.InvalidOperationException);
	}

	[Test]
	public void EnumerationIsLazy() {
		var source = new ProbeEnumerable<int>(new[] { 1, 2, 3 });
		var iterator = new MemorizingIterator<int>(source);
		using var cursor = iterator.GetEnumerator();
		Assert.That(source.EnumeratorCount, Is.Zero);
		Assert.That(cursor.MoveNext(), Is.True);
		Assert.That(cursor.Current, Is.EqualTo(1));
		Assert.That(source.YieldCount, Is.EqualTo(1));
		Assert.That(source.EnumeratorCount, Is.EqualTo(1));
		AssertSequence(iterator, 1, 2, 3);
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(5)]
	public void RepeatedEnumerationReadsSourceOnce(int length) {
		var expected = Enumerable.Range(0, length).ToArray();
		var source = new ProbeEnumerable<int>(expected);
		var iterator = new MemorizingIterator<int>(source);
		AssertSequence(iterator, expected);
		AssertSequence(iterator, expected);
		Assert.That(source.EnumeratorCount, Is.EqualTo(1));
		Assert.That(source.YieldCount, Is.EqualTo(length));
		Assert.That(source.DisposeCount, Is.EqualTo(1));
	}

	[Test]
	public void StructCopiesAndInterfaceBoxesShareCache() {
		var source = new ProbeEnumerable<int>(new[] { 10, 20 });
		var iterator = new MemorizingIterator<int>(source);
		var copy = iterator;
		IEnumerable<int> firstBox = iterator;
		IEnumerable<int> secondBox = iterator;
		AssertSequence(firstBox, 10, 20);
		AssertSequence(secondBox, 10, 20);
		AssertSequence(copy, 10, 20);
		Assert.That(iterator.Count, Is.EqualTo(2));
		Assert.That(source.EnumeratorCount, Is.EqualTo(1));
	}

	[Test]
	public void InterleavedEnumeratorsHaveIndependentPositions() {
		var source = new ProbeEnumerable<int>(new[] { 1, 2, 3 });
		var iterator = new MemorizingIterator<int>(source);
		using var first = iterator.GetEnumerator();
		using var second = iterator.GetEnumerator();
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(first.MoveNext(), Is.True);
		Assert.That(first.Current, Is.EqualTo(1));
		Assert.That(first.MoveNext(), Is.True);
		Assert.That(first.Current, Is.EqualTo(2));
		Assert.That(second.MoveNext(), Is.True);
		Assert.That(second.Current, Is.EqualTo(1));
		Assert.That(first.Current, Is.EqualTo(2));
		Assert.That(source.YieldCount, Is.EqualTo(2));
		AssertSequence(iterator, 1, 2, 3);
		Assert.That(second.MoveNext(), Is.True);
		Assert.That(second.Current, Is.EqualTo(2));
		Assert.That(second.MoveNext(), Is.True);
		Assert.That(second.Current, Is.EqualTo(3));
		Assert.That(second.MoveNext(), Is.False);
	}

	[Test]
	public void DisposingPartialTraversalPreservesUncachedTail() {
		var source = new ProbeEnumerable<int>(new[] { 1, 2, 3 });
		var iterator = new MemorizingIterator<int>(source);
		using (var cursor = iterator.GetEnumerator()) {
			Assert.That(cursor.MoveNext(), Is.True);
			Assert.That(cursor.Current, Is.EqualTo(1));
		}
		Assert.That(source.YieldCount, Is.EqualTo(1));
		Assert.That(source.DisposeCount, Is.Zero);
		AssertSequence(iterator, 1, 2, 3);
		Assert.That(source.DisposeCount, Is.EqualTo(1));
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(3)]
	public void CountMaterializesAndPreservesCursorPosition(int prefixLength) {
		var source = new ProbeEnumerable<int>(new[] { 1, 2, 3 });
		var iterator = new MemorizingIterator<int>(source);
		using var cursor = iterator.GetEnumerator();
		for (var index = 0; index < prefixLength; index++)
			Assert.That(cursor.MoveNext(), Is.True);
		Assert.That(iterator.Count, Is.EqualTo(3));
		Assert.That(iterator.Count, Is.EqualTo(3));
		Assert.That(source.YieldCount, Is.EqualTo(3));
		Assert.That(source.DisposeCount, Is.EqualTo(1));
		if (prefixLength > 0)
			Assert.That(cursor.Current, Is.EqualTo(prefixLength));
		AssertSequence(iterator, 1, 2, 3);
	}

	[Test]
	public void CountAndReplayKeepTheMemorizedSnapshot() {
		var source = new List<int> { 1, 2, 3 };
		var iterator = new MemorizingIterator<int>(source);
		Assert.That(iterator.Count, Is.EqualTo(3));
		source.Clear();
		source.Add(99);
		Assert.That(iterator.Count, Is.EqualTo(3));
		AssertSequence(iterator, 1, 2, 3);
	}

	[Test]
	public void LinqMaterializationUsesReadOnlyCollectionMembers() {
		var source = new ProbeEnumerable<int>(new[] { 1, 2, 3 });
		var iterator = new MemorizingIterator<int>(source);
		Assert.That(iterator.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
		Assert.That(iterator.ToList(), Is.EqualTo(new[] { 1, 2, 3 }));
		Assert.That(iterator.Count(), Is.EqualTo(3));
		Assert.That(source.EnumeratorCount, Is.EqualTo(1));
	}

	[TestCase("a", true)]
	[TestCase(null, true)]
	[TestCase("missing", false)]
	public void ContainsSupportsValuesAndNull(string value, bool expected) {
		var iterator = new MemorizingIterator<string>(new[] { "a", null, "b", "a" });
		Assert.That(iterator.Contains(value), Is.EqualTo(expected));
		AssertSequence(iterator, "a", null, "b", "a");
	}

	[Test]
	public void ContainsStopsAtTheFirstMatch() {
		var source = new ProbeEnumerable<int>(new[] { 1, 2, 3 });
		var iterator = new MemorizingIterator<int>(source);
		Assert.That(iterator.Contains(2), Is.True);
		Assert.That(source.YieldCount, Is.EqualTo(2));
		AssertSequence(iterator, 1, 2, 3);
	}

	[Test]
	public void CopyToPreservesValuesOutsideTheDestinationRange() {
		var iterator = new MemorizingIterator<int>(new[] { 1, 2, 3 });
		var destination = new[] { 99, 99, 99, 99, 99 };
		iterator.CopyTo(destination, 1);
		Assert.That(destination, Is.EqualTo(new[] { 99, 1, 2, 3, 99 }));
		AssertSequence(iterator, 1, 2, 3);
	}

	[Test]
	public void CopyToValidatesDestination() {
		var iterator = new MemorizingIterator<int>(new[] { 1, 2 });
		Assert.That(() => iterator.CopyTo(null, 0), Throws.ArgumentNullException);
		Assert.That(() => iterator.CopyTo(new int[2], -1), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => iterator.CopyTo(new int[2], 3), Throws.TypeOf<ArgumentOutOfRangeException>());
		var destination = new[] { 99, 99 };
		Assert.That(() => iterator.CopyTo(destination, 1), Throws.ArgumentException);
		Assert.That(destination, Is.EqualTo(new[] { 99, 99 }));
	}

	[Test]
	public void EmptySourceCanBeCopiedAtArrayEnd() {
		var iterator = new MemorizingIterator<int>(Array.Empty<int>());
		var destination = new[] { 99 };
		iterator.CopyTo(destination, destination.Length);
		Assert.That(iterator.Count, Is.Zero);
		Assert.That(destination, Is.EqualTo(new[] { 99 }));
	}

	[Test]
	public void CollectionRejectsMutation() {
		ICollection<int> collection = new MemorizingIterator<int>(new[] { 1, 2 });
		Assert.That(collection.IsReadOnly, Is.True);
		Assert.That(() => collection.Add(3), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => collection.Clear(), Throws.TypeOf<NotSupportedException>());
		Assert.That(() => collection.Remove(1), Throws.TypeOf<NotSupportedException>());
		AssertSequence(collection, 1, 2);
	}

	[Test]
	public void NonGenericEnumerationUsesTheSameCache() {
		var source = new ProbeEnumerable<int>(new[] { 1, 2 });
		var iterator = new MemorizingIterator<int>(source);
		var cursor = ((IEnumerable)iterator).GetEnumerator();
		using var cleanup = new ActionScope(() => ((IDisposable)cursor).Dispose());
		Assert.That(cursor.MoveNext(), Is.True);
		Assert.That(cursor.Current, Is.EqualTo(1));
		Assert.That(cursor.MoveNext(), Is.True);
		Assert.That(cursor.Current, Is.EqualTo(2));
		Assert.That(cursor.MoveNext(), Is.False);
		Assert.That(cursor.MoveNext(), Is.False);
		AssertSequence(iterator, 1, 2);
		Assert.That(source.EnumeratorCount, Is.EqualTo(1));
	}

	[Test]
	public void SourceFailureIsReplayedAfterItsCachedPrefix() {
		var failure = new InvalidOperationException("source failure");
		var source = new ProbeEnumerable<int>(FailingSource());
		var iterator = new MemorizingIterator<int>(source);
		for (var attempt = 0; attempt < 2; attempt++) {
			using var cursor = iterator.GetEnumerator();
			Assert.That(cursor.MoveNext(), Is.True);
			Assert.That(cursor.Current, Is.EqualTo(1));
			Assert.That(() => cursor.MoveNext(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		}
		Assert.That(source.EnumeratorCount, Is.EqualTo(1));
		Assert.That(source.DisposeCount, Is.EqualTo(1));

		IEnumerable<int> FailingSource() {
			yield return 1;
			throw failure;
		}
	}

	[Test]
	public void SourceEnumeratorCreationFailureIsNotRetried() {
		var attempts = 0;
		var failure = new InvalidOperationException("creation failure");
		var source = new FactoryEnumerable<int>(() => {
			attempts++;
			throw failure;
		});
		var iterator = new MemorizingIterator<int>(source);
		for (var attempt = 0; attempt < 2; attempt++) {
			using var cursor = iterator.GetEnumerator();
			Assert.That(() => cursor.MoveNext(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		}
		Assert.That(attempts, Is.EqualTo(1));
	}

	[Test]
	public void CurrentFailureDisposesSourceAndRemainsVisibleToCount() {
		var disposals = 0;
		var failure = new InvalidOperationException("current failure");
		var values = ((IEnumerable<int>)new[] { 1 }).GetEnumerator().OnDispose(() => disposals++);
		var source = new FactoryEnumerable<int>(() => new ProjectedEnumerator<int, int>(values, _ => throw failure));
		var iterator = new MemorizingIterator<int>(source);
		using var cursor = iterator.GetEnumerator();
		Assert.That(() => cursor.MoveNext(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		Assert.That(() => iterator.Count, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		Assert.That(disposals, Is.EqualTo(1));
	}

	[Test]
	public void SourceDisposalFailureIsReplayedWithoutAnotherDisposal() {
		var disposals = 0;
		var failure = new InvalidOperationException("disposal failure");
		var source = new FactoryEnumerable<int>(() => ((IEnumerable<int>)new[] { 1 }).GetEnumerator().OnDispose(() => {
			disposals++;
			throw failure;
		}));
		var iterator = new MemorizingIterator<int>(source);
		for (var attempt = 0; attempt < 2; attempt++) {
			using var cursor = iterator.GetEnumerator();
			Assert.That(cursor.MoveNext(), Is.True);
			Assert.That(cursor.Current, Is.EqualTo(1));
			Assert.That(() => cursor.MoveNext(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(failure.Message));
		}
		Assert.That(disposals, Is.EqualTo(1));
	}

	[Test]
	public void SourceFailureTakesPrecedenceOverDisposalFailure() {
		var disposals = 0;
		var sourceFailure = new InvalidOperationException("source failure");
		var source = new FactoryEnumerable<int>(() => FailingSource().GetEnumerator().OnDispose(() => {
			disposals++;
			throw new InvalidOperationException("disposal failure");
		}));
		var iterator = new MemorizingIterator<int>(source);
		for (var attempt = 0; attempt < 2; attempt++) {
			using var cursor = iterator.GetEnumerator();
			Assert.That(cursor.MoveNext(), Is.True);
			Assert.That(cursor.Current, Is.EqualTo(1));
			Assert.That(() => cursor.MoveNext(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(sourceFailure.Message));
		}
		Assert.That(() => iterator.Count, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo(sourceFailure.Message));
		Assert.That(disposals, Is.EqualTo(1));

		IEnumerable<int> FailingSource() {
			yield return 1;
			throw sourceFailure;
		}
	}

	private static void AssertSequence<T>(IEnumerable<T> sequence, params T[] expected) {
		using var cursor = sequence.GetEnumerator();
		foreach (var value in expected) {
			Assert.That(cursor.MoveNext(), Is.True);
			Assert.That(cursor.Current, Is.EqualTo(value));
		}
		Assert.That(cursor.MoveNext(), Is.False, "The traversal must terminate after the expected values.");
	}

	private sealed class FactoryEnumerable<T> : IEnumerable<T> {
		private readonly Func<IEnumerator<T>> _factory;

		public FactoryEnumerable(Func<IEnumerator<T>> factory) {
			_factory = factory;
		}

		public IEnumerator<T> GetEnumerator() => _factory();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	private sealed class ProbeEnumerable<T> : IEnumerable<T> {
		private readonly IEnumerable<T> _items;

		public ProbeEnumerable(IEnumerable<T> items) {
			_items = items;
		}

		public int EnumeratorCount { get; private set; }

		public int YieldCount { get; private set; }

		public int DisposeCount { get; private set; }

		public IEnumerator<T> GetEnumerator() {
			EnumeratorCount++;
			Guard.Ensure(EnumeratorCount == 1, "The source must not be enumerated twice.");
			return Enumerate().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private IEnumerable<T> Enumerate() {
			using var scope = new ActionScope(() => DisposeCount++);
			foreach (var item in _items) {
				YieldCount++;
				yield return item;
			}
		}
	}
}
