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
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class DefaultCollectionInterfaceTests {

	[TestCase(0L)]
	[TestCase(2147483647L)]
	public void ExtendedCount_DefaultsPreserveIntBoundary(long count) {
		var collection = new PrimitiveExtendedList { CountOverride = count };
		Assert.That(((ICollection<int>)collection).Count, Is.EqualTo(count));
		Assert.That(((IReadOnlyCollection<int>)collection).Count, Is.EqualTo(count));
	}

	[Test]
	public void ExtendedCount_DefaultsRejectOverflow() {
		var collection = new PrimitiveExtendedList { CountOverride = (long)int.MaxValue + 1 };
		Assert.That(() => ((ICollection<int>)collection).Count, Throws.TypeOf<OverflowException>());
		Assert.That(() => ((IReadOnlyCollection<int>)collection).Count, Throws.TypeOf<OverflowException>());
	}

	[Test]
	public void ExtendedCollection_BaseContractsDispatchToExplicitCanonicalMembers() {
		var collection = new PrimitiveExtendedList();
		var mutable = (ICollection<int>)collection;
		var writeOnly = (IWriteOnlyExtendedCollection<int>)collection;
		var readOnly = (IReadOnlyExtendedCollection<int>)collection;
		mutable.Add(11);
		writeOnly.Add(22);
		Assert.That(readOnly.Contains(22), Is.True);
		Assert.That(mutable.Contains(11), Is.True);
		var copied = new int[2];
		readOnly.CopyTo(copied, 0);
		Assert.That(copied, Is.EqualTo(new[] { 11, 22 }));
		Assert.That(writeOnly.Remove(11), Is.True);
		Assert.That(mutable.Remove(22), Is.True);
		writeOnly.Add(33);
		mutable.Clear();
		writeOnly.Add(44);
		writeOnly.Clear();
		Assert.That(mutable.Count, Is.Zero);
	}

	[Test]
	public void ExtendedList_DefaultIndexersAndLegacyMethodsDispatchToLongPrimitives() {
		IExtendedList<int> list = new PrimitiveExtendedList();
		list.AddRange(new[] { 10, 20 });
		var legacy = (IList<int>)list;
		var readOnly = (IReadOnlyList<int>)list;
		var writeOnly = (IWriteOnlyExtendedList<int>)list;
		legacy.Insert(1, 15);
		Assert.That(legacy.IndexOf(15), Is.EqualTo(1));
		Assert.That(list[1], Is.EqualTo(15));
		Assert.That(readOnly[1], Is.EqualTo(15));
		writeOnly[1] = 16;
		Assert.That(((IReadOnlyExtendedList<int>)list)[1L], Is.EqualTo(16));
		writeOnly[1L] = 17;
		Assert.That(legacy[1], Is.EqualTo(17));
		legacy[1] = 18;
		list[1L] = 19;
		Assert.That(readOnly[1], Is.EqualTo(19));
		legacy.RemoveAt(1);
		Assert.That(list.ToArray(), Is.EqualTo(new[] { 10, 20 }));
	}

	[TestCase(-1L)]
	[TestCase(2147483647L)]
	public void ExtendedList_DefaultIndexOfPreservesResult(long index) {
		IExtendedList<int> list = new PrimitiveExtendedList { IndexOverride = index };
		Assert.That(list.IndexOf(0), Is.EqualTo(index));
		Assert.That(((IList<int>)list).IndexOf(0), Is.EqualTo(index));
	}

	[Test]
	public void ExtendedList_DefaultIndexOfRejectsOverflow() {
		IExtendedList<int> list = new PrimitiveExtendedList { IndexOverride = (long)int.MaxValue + 1 };
		Assert.That(() => list.IndexOf(0), Throws.TypeOf<OverflowException>());
		Assert.That(() => ((IList<int>)list).IndexOf(0), Throws.TypeOf<OverflowException>());
	}

	[Test]
	public void ReadOnlyList_DefaultIndexersUseReadPrimitive() {
		IReadOnlyExtendedList<int> list = new PrimitiveReadOnlyList();
		Assert.That(list[1L], Is.EqualTo(20));
		Assert.That(((IReadOnlyList<int>)list)[1], Is.EqualTo(20));
		var enumerator = ((IEnumerable)list).GetEnumerator();
		using ((IDisposable)enumerator)
			Assert.That(enumerator.MoveNext(), Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void WriteOnlyList_DefaultIndexersPreserveContravarianceAndLongAddressing(bool longIndex) {
		var implementation = new PrimitiveWriteOnlyList();
		IWriteOnlyExtendedList<string> list = implementation;
		var index = longIndex ? (long)int.MaxValue + 1 : 7;
		if (longIndex)
			list[index] = "value";
		else
			list[(int)index] = "value";
		Assert.That(implementation.LastIndex, Is.EqualTo(index));
		Assert.That(implementation.LastItem, Is.EqualTo("value"));
	}

	[Test]
	public void ConcreteLegacyApisKeepVirtualPrimitiveDispatch() {
		var list = new DispatchListDecorator();
		list.AddRange(new[] { 10, 20 });
		Assert.That(list.IndexOf(20), Is.EqualTo(1));
		Assert.That(list[0], Is.EqualTo(10));
		list[0] = 11;
		list.Insert(1, 15);
		list.RemoveAt(1);
		Assert.That(list.Operations, Is.EqualTo(new[] { "IndexOfL", "Read", "Update", "Insert", "RemoveAt" }));
		Assert.That(list.ReadRange(0, 2), Is.EqualTo(new[] { 11, 20 }));
	}

	[TestCase("Base")]
	[TestCase("Decorator")]
	[TestCase("Observable")]
	public void ConcreteLegacyApisDoNotUseSubclassExplicitInterfaceRemappings(string implementation) {
		IExtendedList<int> list = implementation switch {
			"Base" => new RemappedExtendedList(),
			"Decorator" => new RemappedListDecorator(),
			_ => new RemappedObservableList()
		};
		list.AddRange(new[] { 10, 20 });
		Assert.That(((IReadOnlyExtendedList<int>)list).Read(0), Is.EqualTo(999));
		Assert.That(((IReadOnlyExtendedList<int>)list)[0L], Is.EqualTo(888));
		Assert.That(list.IndexOf(20), Is.EqualTo(1));
		Assert.That(list[0], Is.EqualTo(10));
		Assert.That(list[0L], Is.EqualTo(10));
		list[0] = 11;
		list[0L] = 12;
		list.Insert(1, 15);
		list.RemoveAt(1);
		Assert.That(list.ReadRange(0, 2), Is.EqualTo(new[] { 12, 20 }));
	}

	[Test]
	public void SortedLegacyIndexerKeepsVirtualLongIndexerDispatch() {
		var list = new OverriddenSortedList();
		Assert.That(list[0], Is.EqualTo(42));
		Assert.That(((IReadOnlyList<int>)list)[0], Is.EqualTo(42));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void InheritedIntCountDoesNotBypassVirtualLongCount(bool observable) {
		ICollection<int> collection = observable ? new OverriddenObservableCount() : new OverriddenDecoratorCount();
		Assert.That(collection.Count, Is.EqualTo(99));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ClassCountMappingsDoNotUseSubclassExplicitLongCount(bool observable) {
		ICollection<int> collection = observable ? new RemappedObservableCount() : new RemappedDecoratorCount();
		Assert.That(((IExtendedCollection<int>)collection).Count, Is.EqualTo(999));
		Assert.That(collection.Count, Is.EqualTo(99));
	}

	[Test]
	public void ExtendedCollectionSubclassCanCombineStackDefaults() {
		var list = new StackExtendedList();
		list.Add(7);
		Assert.That(((ICollection<int>)list).Count, Is.EqualTo(1));
		var enumerator = ((IEnumerable)list).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(7));
		}
	}

	[Test]
	public void ExtendedCollectionBase_NongenericEnumerationKeepsGenericInterfaceRemapping() {
		var list = new RemappedExtendedList();
		list.Add(7);
		var enumerator = ((IEnumerable)list).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(999));
		}
	}

	[Test]
	public void PageSubclassCanCombineReadOnlyCollectionDefaults() {
		var page = new CollectionPage();
		var enumerator = ((IEnumerable)page).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(17));
		}
	}

	[Test]
	public void NongenericEnumerationUsesTypedEnumeratorAndPreservesDisposal() {
		var list = new PrimitiveExtendedList();
		((IExtendedCollection<int>)list).AddRange(new[] { 4, 5 });
		var enumerator = ((IEnumerable)list).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(4));
			Assert.That(list.EnumerationDisposals, Is.Zero);
		}
		Assert.That(list.EnumerationDisposals, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void StorageAttachments_DefaultAdaptersKeepAttachmentChecksAndEnumeratorScope(bool stack) {
		using var stream = new MemoryStream();
		using var streams = new ClusteredStreams(stream, 32, reservedStreams: 1);
		using ClusteredStreamsAttachmentBase attachment = stack
			? new StackStorageAttachment<int>(streams, "items", PrimitiveSerializer<int>.Instance)
			: new PagedListStorageAttachment<int>(streams, "items", PrimitiveSerializer<int>.Instance);
		var collection = (ICollection<int>)attachment;
		Assert.That(() => collection.Count, Throws.TypeOf<InvalidOperationException>());
		Assert.That(() => ((IEnumerable)attachment).GetEnumerator(), Throws.TypeOf<InvalidOperationException>());
		streams.RegisterAttachment(attachment);
		streams.Load();
		collection.Add(7);
		Assert.That(collection.Count, Is.EqualTo(1));
		Assert.That(streams.IsLocked, Is.False);
		var enumerator = ((IEnumerable)attachment).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(streams.IsLocked, Is.True);
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(7));
		}
		Assert.That(streams.IsLocked, Is.False);
		if (!stack)
			Assert.That(() => ((IWriteOnlyExtendedCollection<int>)attachment).Add(8), Throws.TypeOf<NotImplementedException>());
	}

	[Test]
	public void Stack_DefaultAddCountAndEnumerationUseStackPrimitives() {
		var stack = new PrimitiveStack();
		var collection = (ICollection<int>)stack;
		collection.Add(7);
		collection.Add(8);
		Assert.That(collection.Count, Is.EqualTo(2));
		Assert.That(stack.TryPeek(out var top), Is.True);
		Assert.That(top, Is.EqualTo(8));
		var enumerator = ((IEnumerable)stack).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(8));
		}
		stack.CountOverride = (long)int.MaxValue + 1;
		Assert.That(() => collection.Count, Throws.TypeOf<OverflowException>());
	}

	[Test]
	public void Queue_DefaultAddAndEnumerationUseQueuePrimitives() {
		var queue = new PrimitiveQueue();
		((ICollection<int>)queue).Add(7);
		((ICollection<int>)queue).Add(8);
		Assert.That(queue.TryDequeue(out var first), Is.True);
		Assert.That(first, Is.EqualTo(7));
		var enumerator = ((IEnumerable)queue).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(8));
		}
	}

	[TestCase("Stack")]
	[TestCase("Queue")]
	[TestCase("QueueList")]
	[TestCase("Heap")]
	public void ConcreteAdaptersPreservePublicAddAndNongenericEnumerationMappings(string implementation) {
		ICollection<int> collection = implementation switch {
			"Stack" => new RemappedStackAdapter(),
			"Queue" => new RemappedQueueAdapter(),
			"QueueList" => new RemappedQueueListAdapter(),
			_ => new RemappedHeap()
		};
		collection.Add(7);
		Assert.That(collection.Count, Is.EqualTo(1));
		using (var typedEnumerator = ((IEnumerable<int>)collection).GetEnumerator()) {
			Assert.That(typedEnumerator.MoveNext(), Is.True);
			Assert.That(typedEnumerator.Current, Is.EqualTo(999));
		}
		var enumerator = ((IEnumerable)collection).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(7));
		}
	}

	[Test]
	public void Heap_NongenericEnumerationPreservesSortedOrder() {
		var heap = new BinaryHeap<int>();
		heap.Add(9);
		heap.Add(2);
		var enumerator = ((IEnumerable)heap).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(2));
		}
	}

	[Test]
	public void BloomFilter_NongenericEnumerationUsesBitView() {
		var filter = new HashedBloomFilter<int>(64, 2, CHF.SHA2_256, PrimitiveSerializer<int>.Instance);
		filter.Add(7);
		var enumerator = ((IEnumerable)filter).GetEnumerator();
		var bits = new List<bool>();
		using ((IDisposable)enumerator)
			while (enumerator.MoveNext())
				bits.Add((bool)enumerator.Current);
		Assert.That(bits, Is.EqualTo(filter.ToArray()));
		Assert.That(bits.Any(value => value), Is.True);
	}

	[Test]
	public void Page_NongenericEnumerationPreservesLifecycleChecks() {
		using var page = new MemoryPagedBuffer.BufferPage(32);
		Assert.That(() => ((IEnumerable)page).GetEnumerator(), Throws.TypeOf<InvalidOperationException>());
		page.Load();
		Assert.That(page.AppendSpan(new byte[] { 3, 4 }, out _), Is.True);
		var enumerator = ((IEnumerable)page).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo((byte)3));
		}
		page.Unload();
		Assert.That(() => ((IEnumerable)page).GetEnumerator(), Throws.TypeOf<InvalidOperationException>());
	}

	[TestCase("List")]
	[TestCase("RecyclableList")]
	[TestCase("MerkleList")]
	[TestCase("MerkleRecyclableList")]
	public void StreamCollections_BasePropertyReturnsTypedStream(string implementation) {
		using var stream = new MemoryStream();
		IStreamMappedCollection<int> collection = implementation switch {
			"List" => StreamMappedFactory.CreateList<int>(stream, 32, PrimitiveSerializer<int>.Instance, autoLoad: true),
			"RecyclableList" => StreamMappedFactory.CreateRecyclableList<int>(stream, 32, PrimitiveSerializer<int>.Instance, autoLoad: true),
			"MerkleList" => new StreamMappedMerkleList<int>(stream, CHF.SHA2_256, 32, PrimitiveSerializer<int>.Instance, autoLoad: true),
			_ => new StreamMappedMerkleRecyclableList<int>(stream, CHF.SHA2_256, 32, PrimitiveSerializer<int>.Instance, autoLoad: true)
		};
		using var ownedCollection = (IDisposable)collection;
		Assert.That(((IStreamMappedCollection)collection).ObjectStream, Is.SameAs(collection.ObjectStream));
	}

	[Test]
	public void StreamDictionaries_AdoptTheirActualStreamItemContract() {
		using var pairStream = new MemoryStream();
		using var valueStream = new MemoryStream();
		using var pairs = StreamMappedFactory.CreateDictionaryKvp<int, int>(pairStream, 32, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, autoLoad: true);
		using var values = StreamMappedFactory.CreateDictionaryClk<int, int>(valueStream, 32, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, autoLoad: true);
		Assert.That(((IStreamMappedCollection<KeyValuePair<int, int>>)pairs).ObjectStream, Is.SameAs(pairs.ObjectStream));
		Assert.That(((IStreamMappedCollection)pairs).ObjectStream, Is.SameAs(pairs.ObjectStream));
		Assert.That(((IStreamMappedCollection<int>)values).ObjectStream, Is.SameAs(values.ObjectStream));
		Assert.That(((IStreamMappedCollection)values).ObjectStream, Is.SameAs(values.ObjectStream));
	}

	[Test]
	public void StreamDictionaryPairCollectionPreservesOperationsAndStorage(
		[Values(false, true)] bool useConstantLengthKeys,
		[Values("Collection", "Dictionary", "StandardCollection", "UntypedCollection")] string clearView
	) {
		using var stream = new MemoryStream();
		using IStreamMappedDictionary<int, int> dictionary = useConstantLengthKeys
			? StreamMappedFactory.CreateDictionaryClk<int, int>(stream, 32, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, autoLoad: true)
			: StreamMappedFactory.CreateDictionaryKvp<int, int>(stream, 32, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, autoLoad: true);
		IStreamMappedCollection<int, int> collection = dictionary;
		var entry = new KeyValuePair<int, int>(1, 10);

		collection.Add(entry);
		var copy = new KeyValuePair<int, int>[1];
		collection.CopyTo(copy, 0);
		Assert.That(collection.Count, Is.EqualTo(1));
		Assert.That(copy, Is.EqualTo(new[] { entry }));
		Assert.That(collection.Contains(entry), Is.True);
		Assert.That(collection.Remove(entry), Is.True);
		collection.Add(entry);
		Assert.That(collection.ObjectStream, Is.SameAs(dictionary.ObjectStream));
		Assert.That(collection.ObjectStream.ItemType, Is.EqualTo(useConstantLengthKeys ? typeof(int) : typeof(KeyValuePair<int, int>)));

		switch (clearView) {
			case "Collection":
				collection.Clear();
				break;
			case "Dictionary":
				dictionary.Clear();
				break;
			case "StandardCollection":
				((ICollection<KeyValuePair<int, int>>)collection).Clear();
				break;
			default:
				((IStreamMappedCollection)collection).Clear();
				break;
		}

		Assert.That(collection, Is.Empty);
		Assert.That(dictionary.ContainsKey(entry.Key), Is.False);
		collection.Add(entry);
		Assert.That(dictionary[entry.Key], Is.EqualTo(entry.Value));
	}

	[TestCase("Lookup")]
	[TestCase("InMemory")]
	[TestCase("StreamMapped")]
	public void BTreeLookup_LinqAdaptersPreserveGroupsAndDistinctCount(string implementation) {
		using var stream = new MemoryStream();
		IExtendedBTreeLookup<int, int> lookup = implementation switch {
			"Lookup" => new BTreeLookup<int, int>(
				new InMemoryBTreePlus<CompoundKey<int, int>, int>(5, new CompoundKeyComparer<int, int>(Comparer<int>.Default, Comparer<int>.Default)),
				Comparer<int>.Default
			),
			"InMemory" => new InMemoryBTreeLookup<int, int>(5, Comparer<int>.Default),
			_ => new StreamMappedBTreeLookup<int, int>(5, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<int>.Instance, Comparer<int>.Default)
		};
		using var ownedLookup = lookup as IDisposable;
		lookup.Add(1, 10);
		lookup.Add(1, 11);
		lookup.Add(2, 20);
		var legacy = (ILookup<int, int>)lookup;
		Assert.That(legacy.Count, Is.EqualTo(2));
		Assert.That(lookup.TotalCount, Is.EqualTo(3));
		Assert.That(legacy.Contains(1), Is.True);
		Assert.That(legacy.Contains(3), Is.False);
		Assert.That(legacy[1], Is.EqualTo(new[] { 10, 11 }));
		Assert.That(legacy[3], Is.Empty);
		Assert.That(legacy.Select(group => group.Key), Is.EqualTo(new[] { 1, 2 }));
		var enumerator = ((IEnumerable)lookup).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(((IGrouping<int, int>)enumerator.Current).Key, Is.EqualTo(1));
		}
	}

	[Test]
	public void DirectDictionaryList_DefaultValueViewUsesDictionaryContract() {
		var dictionary = new DirectDictionaryList();
		var values = (IReadOnlyList<int>)dictionary;
		Assert.That(values.Count, Is.EqualTo(2));
		Assert.That(((IEnumerable<int>)dictionary).ToArray(), Is.EqualTo(new[] { 10, 20 }));
	}

	[Test]
	public void DictionaryList_StateSpecificValueViewSurvivesDivergentSubclassProperties() {
		var dictionary = new DivergentDictionaryList();
		dictionary.Add("key", 17);
		Assert.That(((IReadOnlyCollection<int>)dictionary).Count, Is.EqualTo(1));
		Assert.That(((IReadOnlyCollection<KeyValuePair<string, int>>)dictionary).Count, Is.EqualTo(99));
		Assert.That(((IEnumerable<int>)dictionary).ToArray(), Is.EqualTo(new[] { 17 }));
		Assert.That(dictionary.Values, Is.EqualTo(new[] { 999 }));
		using (var typedEnumerator = ((IEnumerable<KeyValuePair<string, int>>)dictionary).GetEnumerator()) {
			Assert.That(typedEnumerator.MoveNext(), Is.True);
			Assert.That(typedEnumerator.Current, Is.EqualTo(new KeyValuePair<string, int>("redirect", 999)));
		}
		var enumerator = ((IEnumerable)dictionary).GetEnumerator();
		using ((IDisposable)enumerator) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(new KeyValuePair<string, int>("key", 17)));
		}
	}

	private sealed class PrimitiveExtendedList : IExtendedList<int> {
		private readonly List<int> _items = new();

		public long? CountOverride { get; set; }

		public long? IndexOverride { get; set; }

		public int EnumerationDisposals { get; private set; }

		public bool IsReadOnly => false;

		long IExtendedCollection<int>.Count => CountOverride ?? _items.Count;

		void IExtendedCollection<int>.Add(int item) => _items.Add(item);

		void IExtendedCollection<int>.Clear() => _items.Clear();

		bool IExtendedCollection<int>.Contains(int item) => _items.Contains(item);

		bool IExtendedCollection<int>.Remove(int item) => _items.Remove(item);

		void IExtendedCollection<int>.CopyTo(int[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		void IExtendedList<int>.Insert(long index, int item) => _items.Insert(checked((int)index), item);

		void IExtendedList<int>.RemoveAt(long index) => _items.RemoveAt(checked((int)index));

		public void AddRange(IEnumerable<int> items) => _items.AddRange(items);

		public bool[] RemoveRange(IEnumerable<int> items) => items.Select(_items.Remove).ToArray();

		public IEnumerable<bool> ContainsRange(IEnumerable<int> items) => items.Select(_items.Contains);

		public long IndexOfL(int item) => IndexOverride ?? _items.IndexOf(item);

		public long[] IndexOfRange(IEnumerable<int> items) => items.Select(item => (long)_items.IndexOf(item)).ToArray();

		public int Read(long index) => _items[checked((int)index)];

		public IEnumerable<int> ReadRange(long index, long count) => _items.SkipL(index).TakeL(count);

		public void Update(long index, int item) => _items[checked((int)index)] = item;

		public void UpdateRange(long index, IEnumerable<int> items) {
			foreach (var item in items)
				Update(index++, item);
		}

		public void InsertRange(long index, IEnumerable<int> items) => _items.InsertRange(checked((int)index), items);

		public void RemoveRange(long index, long count) => _items.RemoveRange(checked((int)index), checked((int)count));

		public IEnumerator<int> GetEnumerator() => _items.GetEnumerator().OnDispose(() => EnumerationDisposals++);
	}

	private sealed class DispatchListDecorator : ExtendedListDecorator<int> {
		private readonly List<string> _operations = new();

		public DispatchListDecorator()
			: base(new ExtendedList<int>()) {
		}

		public string[] Operations => _operations.ToArray();

		public override long IndexOfL(int item) {
			_operations.Add("IndexOfL");
			return base.IndexOfL(item);
		}

		public override int Read(long index) {
			_operations.Add("Read");
			return base.Read(index);
		}

		public override void Update(long index, int item) {
			_operations.Add("Update");
			base.Update(index, item);
		}

		public override void Insert(long index, int item) {
			_operations.Add("Insert");
			base.Insert(index, item);
		}

		public override void RemoveAt(long index) {
			_operations.Add("RemoveAt");
			base.RemoveAt(index);
		}
	}

	private sealed class PrimitiveReadOnlyList : IReadOnlyExtendedList<int> {
		private readonly int[] _items = { 10, 20 };

		public int Count => _items.Length;

		public bool Contains(int item) => _items.Contains(item);

		public IEnumerable<bool> ContainsRange(IEnumerable<int> items) => items.Select(Contains);

		public void CopyTo(int[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		public long IndexOfL(int item) => ((IList<int>)_items).IndexOf(item);

		public long[] IndexOfRange(IEnumerable<int> items) => items.Select(IndexOfL).ToArray();

		public int Read(long index) => _items[checked((int)index)];

		public IEnumerable<int> ReadRange(long index, long count) => _items.SkipL(index).TakeL(count);

		public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_items).GetEnumerator();
	}

	private sealed class PrimitiveWriteOnlyList : IWriteOnlyExtendedList<object> {
		public long LastIndex { get; private set; }

		public object LastItem { get; private set; }

		public void Update(long index, object item) {
			LastIndex = index;
			LastItem = item;
		}

		public void Add(object item) => Update(0, item);

		public void AddRange(IEnumerable<object> items) => UpdateRange(0, items);

		public bool Remove(object item) => false;

		public bool[] RemoveRange(IEnumerable<object> items) => items.Select(Remove).ToArray();

		public void Clear() => LastItem = null;

		public void UpdateRange(long index, IEnumerable<object> items) {
			foreach (var item in items)
				Update(index++, item);
		}

		public void Insert(long index, object item) => Update(index, item);

		public void InsertRange(long index, IEnumerable<object> items) => UpdateRange(index, items);

		public void RemoveAt(long index) => LastIndex = index;

		public void RemoveRange(long index, long count) => LastIndex = index;
	}

	private sealed class RemappedExtendedList : ExtendedList<int>, IReadOnlyExtendedList<int>, IWriteOnlyExtendedList<int>, IEnumerable<int> {
		long IReadOnlyExtendedList<int>.IndexOfL(int item) => 999;

		int IReadOnlyExtendedList<int>.Read(long index) => 999;

		int IReadOnlyExtendedList<int>.this[long index] => 888;

		int IWriteOnlyExtendedList<int>.this[int index] {
			set => throw new InvalidOperationException();
		}

		int IWriteOnlyExtendedList<int>.this[long index] {
			set => throw new InvalidOperationException();
		}

		void IWriteOnlyExtendedList<int>.Update(long index, int item) => throw new InvalidOperationException();

		void IWriteOnlyExtendedList<int>.Insert(long index, int item) => throw new InvalidOperationException();

		void IWriteOnlyExtendedList<int>.RemoveAt(long index) => throw new InvalidOperationException();

		IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 999 }).GetEnumerator();
	}

	private sealed class RemappedListDecorator : ExtendedListDecorator<int>, IReadOnlyExtendedList<int>, IWriteOnlyExtendedList<int> {
		public RemappedListDecorator()
			: base(new ExtendedList<int>()) {
		}

		long IReadOnlyExtendedList<int>.IndexOfL(int item) => 999;

		int IReadOnlyExtendedList<int>.Read(long index) => 999;

		int IReadOnlyExtendedList<int>.this[long index] => 888;

		int IWriteOnlyExtendedList<int>.this[int index] {
			set => throw new InvalidOperationException();
		}

		int IWriteOnlyExtendedList<int>.this[long index] {
			set => throw new InvalidOperationException();
		}

		void IWriteOnlyExtendedList<int>.Update(long index, int item) => throw new InvalidOperationException();

		void IWriteOnlyExtendedList<int>.Insert(long index, int item) => throw new InvalidOperationException();

		void IWriteOnlyExtendedList<int>.RemoveAt(long index) => throw new InvalidOperationException();
	}

	private sealed class RemappedObservableList : ObservableExtendedList<int>, IReadOnlyExtendedList<int>, IWriteOnlyExtendedList<int> {
		long IReadOnlyExtendedList<int>.IndexOfL(int item) => 999;

		int IReadOnlyExtendedList<int>.Read(long index) => 999;

		int IReadOnlyExtendedList<int>.this[long index] => 888;

		int IWriteOnlyExtendedList<int>.this[int index] {
			set => throw new InvalidOperationException();
		}

		int IWriteOnlyExtendedList<int>.this[long index] {
			set => throw new InvalidOperationException();
		}

		void IWriteOnlyExtendedList<int>.Update(long index, int item) => throw new InvalidOperationException();

		void IWriteOnlyExtendedList<int>.Insert(long index, int item) => throw new InvalidOperationException();

		void IWriteOnlyExtendedList<int>.RemoveAt(long index) => throw new InvalidOperationException();
	}

	private sealed class StackExtendedList : ExtendedList<int>, IStack<int> {
		public void Push(int item) => Add(item);

		public bool TryPeek(out int value) {
			value = Count > 0 ? Read(Count - 1) : default;
			return Count > 0;
		}

		public bool TryPop(out int value) {
			if (!TryPeek(out value))
				return false;
			RemoveAt(Count - 1);
			return true;
		}
	}

	private sealed class CollectionPage : PageBase<int>, IReadOnlyExtendedCollection<int> {
		int IReadOnlyCollection<int>.Count => 1;

		public bool Contains(int item) => item == 17;

		public IEnumerable<bool> ContainsRange(IEnumerable<int> items) => items.Select(Contains);

		public void CopyTo(int[] array, int arrayIndex) => array[arrayIndex] = 17;

		public override IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)new[] { 17 }).GetEnumerator();

		protected override IEnumerable<int> ReadInternal(long index, long count) => new[] { 17 };

		protected override long AppendInternal(int[] items, out long newItemsSize) {
			newItemsSize = items.Length;
			return items.Length;
		}

		protected override void UpdateInternal(long index, int[] items, out long oldItemsSize, out long newItemsSize) {
			oldItemsSize = items.Length;
			newItemsSize = items.Length;
		}

		protected override void EraseFromEndInternal(long count, out long oldItemsSize) => oldItemsSize = count;
	}

	private sealed class RemappedStackAdapter : StackAdapter<int>, IStack<int> {
		public RemappedStackAdapter()
			: base(new ExtendedList<int>()) {
		}

		long IStack<int>.Count => 999;

		void IStack<int>.Push(int item) => throw new InvalidOperationException();

		IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 999 }).GetEnumerator();
	}

	private sealed class RemappedQueueAdapter : QueueAdapter<int>, IQueue<int> {
		void IQueue<int>.Enqueue(int item) => throw new InvalidOperationException();

		IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 999 }).GetEnumerator();
	}

	private sealed class RemappedQueueListAdapter : QueueListAdapter<int>, IQueue<int> {
		public RemappedQueueListAdapter()
			: base(new List<int>()) {
		}

		void IQueue<int>.Enqueue(int item) => throw new InvalidOperationException();

		IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 999 }).GetEnumerator();
	}

	private sealed class RemappedHeap : BinaryHeap<int>, IEnumerable<int> {
		IEnumerator<int> IEnumerable<int>.GetEnumerator() => ((IEnumerable<int>)new[] { 999 }).GetEnumerator();
	}

	private sealed class OverriddenSortedList : SortedList<int>, IReadOnlyExtendedList<int> {
		public override int this[long index] => 42;

		int IReadOnlyExtendedList<int>.this[long index] => 888;

		int IReadOnlyExtendedList<int>.Read(long index) => 999;
	}

	private sealed class OverriddenDecoratorCount : ExtendedCollectionDecorator<int> {
		public OverriddenDecoratorCount()
			: base(new ExtendedList<int>()) {
		}

		public override long Count => 99;
	}

	private sealed class OverriddenObservableCount : ObservableExtendedCollection<int> {
		public override long Count => 99;
	}

	private sealed class RemappedDecoratorCount : ExtendedCollectionDecorator<int>, IExtendedCollection<int> {
		public RemappedDecoratorCount()
			: base(new ExtendedList<int>()) {
		}

		public override long Count => 99;

		long IExtendedCollection<int>.Count => 999;
	}

	private sealed class RemappedObservableCount : ObservableExtendedCollection<int>, IExtendedCollection<int> {
		public override long Count => 99;

		long IExtendedCollection<int>.Count => 999;
	}

	private sealed class PrimitiveStack : IStack<int> {
		private readonly Stack<int> _items = new();

		public long? CountOverride { get; set; }

		public long Count => CountOverride ?? _items.Count;

		public bool IsReadOnly => false;

		public bool TryPeek(out int value) => _items.TryPeek(out value);

		public bool TryPop(out int value) => _items.TryPop(out value);

		public void Push(int item) => _items.Push(item);

		public void Clear() => _items.Clear();

		public bool Contains(int item) => _items.Contains(item);

		public void CopyTo(int[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		public bool Remove(int item) => false;

		public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
	}

	private sealed class PrimitiveQueue : IQueue<int> {
		private readonly Queue<int> _items = new();

		public int Count => _items.Count;

		public bool IsReadOnly => false;

		public bool TryPeek(out int value) => _items.TryPeek(out value);

		public bool TryDequeue(out int value) => _items.TryDequeue(out value);

		public void Enqueue(int item) => _items.Enqueue(item);

		public void Clear() => _items.Clear();

		public bool Contains(int item) => _items.Contains(item);

		public void CopyTo(int[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

		public bool Remove(int item) => false;

		public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
	}

	private sealed class DirectDictionaryList : IReadOnlyDictionaryList<string, int> {
		private readonly Dictionary<string, int> _items = new() { ["a"] = 10, ["b"] = 20 };

		public IEnumerable<string> Keys => _items.Keys;

		public IEnumerable<int> Values => _items.Values;

		public int this[string key] => _items[key];

		public int this[int index] => _items.Values.ElementAt(index);

		int IReadOnlyCollection<KeyValuePair<string, int>>.Count => _items.Count;

		public int IndexOf(string key) => _items.Keys.ToList().IndexOf(key);

		public bool ContainsKey(string key) => _items.ContainsKey(key);

		public bool TryGetValue(string key, out int value) => _items.TryGetValue(key, out value);

		public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	private sealed class DivergentDictionaryList : DictionaryList<string, int>, IEnumerable<KeyValuePair<string, int>> {
		public override long Count => 99;

		public override ICollection<int> Values => new[] { 999 };

		IEnumerator<KeyValuePair<string, int>> IEnumerable<KeyValuePair<string, int>>.GetEnumerator()
			=> ((IEnumerable<KeyValuePair<string, int>>)new[] { new KeyValuePair<string, int>("redirect", 999) }).GetEnumerator();
	}
}
