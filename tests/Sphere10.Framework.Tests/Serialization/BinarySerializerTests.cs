// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>Verifies the .NET collections, value types, structs and object graphs that <see cref="BinarySerializer"/> round-trips.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BinarySerializerTests {

	#region Collections

	[Test]
	public void Queue_RoundTrips() => AssertCollectionRoundTrips(count => new Queue<int>(Enumerable.Range(1, count)));

	[Test]
	public void Stack_RoundTrips() => AssertCollectionRoundTrips(count => new Stack<int>(Enumerable.Range(1, count)));

	[Test]
	public void LinkedList_RoundTrips() => AssertCollectionRoundTrips(count => new LinkedList<int>(Enumerable.Range(1, count)));

	[Test]
	public void HashSet_RoundTrips() => AssertCollectionRoundTrips(count => new HashSet<int>(Enumerable.Range(1, count)), false);

	[Test]
	public void SortedSet_RoundTrips() => AssertCollectionRoundTrips(count => new SortedSet<int>(Enumerable.Range(1, count)));

	[Test]
	public void List_RoundTrips() => AssertCollectionRoundTrips(count => new List<int>(Enumerable.Range(1, count)));

	[Test]
	public void Collection_RoundTrips() => AssertCollectionRoundTrips(count => new Collection<int>(Enumerable.Range(1, count).ToList()));

	[Test]
	public void ObservableCollection_RoundTrips() => AssertCollectionRoundTrips(count => new System.Collections.ObjectModel.ObservableCollection<int>(Enumerable.Range(1, count)));

	[Test]
	public void ReadOnlyCollection_RoundTrips() => AssertCollectionRoundTrips(count => new ReadOnlyCollection<int>(Enumerable.Range(1, count).ToList()));

	[Test]
	public void Dictionary_RoundTrips() => AssertCollectionRoundTrips(count => Enumerable.Range(1, count).ToDictionary(x => $"key{x}", x => x), false);

	[Test]
	public void SortedList_RoundTrips() => AssertCollectionRoundTrips(count => new SortedList<string, int>(Enumerable.Range(1, count).ToDictionary(x => $"key{x}", x => x)));

	[Test]
	public void SortedDictionary_RoundTrips() => AssertCollectionRoundTrips(count => new SortedDictionary<string, int>(Enumerable.Range(1, count).ToDictionary(x => $"key{x}", x => x)));

	[Test]
	public void ConcurrentDictionary_RoundTrips() => AssertCollectionRoundTrips(count => new ConcurrentDictionary<string, int>(Enumerable.Range(1, count).ToDictionary(x => $"key{x}", x => x)), false);

	[Test]
	public void ConcurrentQueue_RoundTrips() => AssertCollectionRoundTrips(count => new ConcurrentQueue<int>(Enumerable.Range(1, count)));

	[Test]
	public void ConcurrentStack_RoundTrips() => AssertCollectionRoundTrips(count => new ConcurrentStack<int>(Enumerable.Range(1, count)));

	[Test]
	public void ConcurrentBag_RoundTrips() => AssertCollectionRoundTrips(count => new ConcurrentBag<int>(Enumerable.Range(1, count)), false);

	[Test]
	public void Stack_PreservesPopOrder() {
		var stack = new Stack<string>();
		stack.Push("bottom");
		stack.Push("middle");
		stack.Push("top");
		var copy = RoundTrip(stack);
		Assert.That(copy.Pop(), Is.EqualTo("top"));
		Assert.That(copy.Pop(), Is.EqualTo("middle"));
		Assert.That(copy.Pop(), Is.EqualTo("bottom"));
		Assert.That(copy, Is.Empty);
	}

	[Test]
	public void ConcurrentStack_PreservesPopOrder() {
		var stack = new ConcurrentStack<string>();
		stack.Push("bottom");
		stack.Push("top");
		var copy = RoundTrip(stack);
		Assert.That(copy.TryPop(out var first), Is.True);
		Assert.That(first, Is.EqualTo("top"));
		Assert.That(copy.TryPop(out var second), Is.True);
		Assert.That(second, Is.EqualTo("bottom"));
	}

	[Test]
	public void Queue_PreservesDequeueOrderAndNullItems() {
		var queue = new Queue<string>(new[] { "first", null, "last" });
		var copy = RoundTrip(queue);
		Assert.That(copy.Dequeue(), Is.EqualTo("first"));
		Assert.That(copy.Dequeue(), Is.Null);
		Assert.That(copy.Dequeue(), Is.EqualTo("last"));
	}

	[Test]
	public void LinkedList_PreservesNodeOrder() {
		var copy = RoundTrip(new LinkedList<string>(new[] { "first", "second", "third" }));
		Assert.That(copy.First.Value, Is.EqualTo("first"));
		Assert.That(copy.First.Next.Value, Is.EqualTo("second"));
		Assert.That(copy.Last.Value, Is.EqualTo("third"));
		Assert.That(copy.Last.Previous.Value, Is.EqualTo("second"));
	}

	[Test]
	public void KeyedCollections_SupportLookupsAfterRoundTrip() {
		Assert.That(RoundTrip(new HashSet<string> { "alpha", "beta" }).Contains("beta"), Is.True);
		Assert.That(RoundTrip(new Dictionary<string, int> { ["alpha"] = 1, ["beta"] = 2 })["beta"], Is.EqualTo(2));
		Assert.That(RoundTrip(new SortedSet<string> { "beta", "alpha" }).Min, Is.EqualTo("alpha"));
		Assert.That(RoundTrip(new SortedList<string, int> { ["beta"] = 2, ["alpha"] = 1 }).Keys, Is.EqualTo(new[] { "alpha", "beta" }));
		Assert.That(RoundTrip(new SortedDictionary<string, int> { ["beta"] = 2, ["alpha"] = 1 }).First().Key, Is.EqualTo("alpha"));
		Assert.That(RoundTrip(new ConcurrentDictionary<string, int>(new[] { KeyValuePair.Create("alpha", 1) })).TryGetValue("alpha", out var value), Is.True);
		Assert.That(value, Is.EqualTo(1));
	}

	[Test]
	public void NestedCollections_RoundTrip() {
		var graph = new Dictionary<string, Queue<Stack<int>>> {
			["first"] = new(new[] { new Stack<int>(new[] { 1, 2 }), new Stack<int>() }),
			["second"] = new()
		};
		var copy = RoundTrip(graph);
		Assert.That(copy.Keys, Is.EquivalentTo(graph.Keys));
		Assert.That(copy["first"].Select(stack => stack.ToArray()), Is.EqualTo(graph["first"].Select(stack => stack.ToArray())));
		Assert.That(copy["second"], Is.Empty);
	}

	[Test]
	public void JaggedArray_RoundTrips() {
		var array = new[] { new[] { 1, 2 }, Array.Empty<int>(), new[] { 3 } };
		Assert.That(RoundTrip(array), Is.EqualTo(array));
		Assert.That(RoundTrip(new Holder<int[][]> { Value = array, Tail = 77 }).Value, Is.EqualTo(array));
	}

	#endregion

	#region Values

	[Test]
	[TestCaseSource(nameof(Values))]
	public void Value_RoundTrips(object value) {
		Assert.That(RoundTrip(value), Is.EqualTo(value));
		var copy = RoundTrip(new Holder<object> { Value = value, Tail = 77 });
		Assert.That(copy.Value, Is.TypeOf(value.GetType()));
		Assert.That(copy.Value, Is.EqualTo(value));
		Assert.That(copy.Tail, Is.EqualTo(77));
	}

	[Test]
	public void NullableMembers_RoundTrip() {
		var copy = RoundTrip(new NullableOwner { Number = 5, Colour = Colour.Green, Point = new PlainPoint { X = 1, Y = 2 } });
		Assert.That(copy.Number, Is.EqualTo(5));
		Assert.That(copy.Missing, Is.Null);
		Assert.That(copy.Colour, Is.EqualTo(Colour.Green));
		Assert.That(copy.Point, Is.EqualTo(new PlainPoint { X = 1, Y = 2 }));
	}

	[Test]
	public void DateTimeOffset_PreservesInstantAndOffset([Values(-8, 0, 3, 10)] int offsetHours) {
		var value = new DateTimeOffset(2020, 6, 15, 12, 30, 45, TimeSpan.FromHours(offsetHours));
		var copy = RoundTrip(value);
		Assert.That(copy, Is.EqualTo(value));
		Assert.That(copy.Offset, Is.EqualTo(value.Offset));
	}

	#endregion

	#region Structs

	[Test]
	public void Struct_WithoutConstructor_RoundTrips() {
		var value = new PlainPoint { X = 3, Y = 4, Label = "corner" };
		Assert.That(RoundTrip(value), Is.EqualTo(value));
	}

	[Test]
	public void Struct_WithConstructor_RoundTrips() {
		var value = new ConstructedPoint { X = 3, Y = 4, Label = "corner" };
		Assert.That(RoundTrip(value), Is.EqualTo(value));
	}

	[Test]
	public void Struct_Default_RoundTrips()
		=> Assert.That(RoundTrip(default(PlainPoint)), Is.EqualTo(default(PlainPoint)));

	[Test]
	public void ReadOnlyRecordStruct_RoundTrips() {
		var value = new Measurement(12.5, "kg");
		Assert.That(RoundTrip(value), Is.EqualTo(value));
	}

	[Test]
	public void Struct_InsideClassArrayAndCollections_RoundTrips() {
		var point = new PlainPoint { X = 3, Y = 4, Label = "corner" };
		var owner = new PointOwner {
			Point = point,
			Boxed = point,
			Array = new[] { new PlainPoint { X = 5, Y = 6, Label = "array" }, default },
			List = new List<PlainPoint> { point },
			Map = new Dictionary<string, PlainPoint> { ["corner"] = point },
			Nested = new Segment { From = point, To = new PlainPoint { X = 9 } }
		};
		var copy = RoundTrip(owner);
		Assert.That(copy.Point, Is.EqualTo(point));
		Assert.That(copy.Boxed, Is.EqualTo(point));
		Assert.That(copy.Array, Is.EqualTo(owner.Array));
		Assert.That(copy.List, Is.EqualTo(owner.List));
		Assert.That(copy.Map["corner"], Is.EqualTo(point));
		Assert.That(copy.Nested.From, Is.EqualTo(point));
		Assert.That(copy.Nested.To.X, Is.EqualTo(9));
	}

	[Test]
	public void Struct_WithoutConstructorOrMembers_IsRejected()
		=> Assert.That(() => RoundTrip(new Opaque(5)), Throws.InvalidOperationException.With.Message.Contains("public parameterless constructor"));

	#endregion

	#region Object graphs

	[Test]
	public void SharedReference_InsideCollection_IsPreserved() {
		var shared = new Node { Name = "shared" };
		var copy = RoundTrip(new Queue<Node>(new[] { shared, shared }));
		Assert.That(copy.Count, Is.EqualTo(2));
		Assert.That(copy.Dequeue(), Is.SameAs(copy.Dequeue()));
	}

	[Test]
	public void Cycle_ThroughCollection_IsPreserved() {
		var node = new Node { Name = "root", Children = new LinkedList<Node>() };
		node.Children.AddLast(node);
		node.Pending = new Stack<object>();
		node.Pending.Push(node.Pending);
		var copy = RoundTrip(node);
		Assert.That(copy.Children.First.Value, Is.SameAs(copy));
		Assert.That(copy.Pending.Peek(), Is.SameAs(copy.Pending));
	}

	[Test]
	public void BaseTypedMember_HoldingCollection_RoundTrips() {
		var copy = RoundTrip(new Holder<IEnumerable> { Value = new Queue<int>(new[] { 1, 2, 3 }), Tail = 77 });
		Assert.That(copy.Value, Is.TypeOf<Queue<int>>());
		Assert.That(copy.Value, Is.EqualTo(new[] { 1, 2, 3 }));
		Assert.That(copy.Tail, Is.EqualTo(77));
	}

	[Test]
	public void LinkedChain_RoundTrips() {
		var copy = RoundTrip(CreateChain(500));
		var length = 0;
		for (var link = copy; link is not null; link = link.Next)
			length++;
		Assert.That(length, Is.EqualTo(500));
	}

	[Test]
	public void LinkedChain_TooDeepForTheStack_ThrowsInsteadOfOverflowing() {
		var chain = CreateChain(100_000);
		Assert.That(() => new BinarySerializer().SerializeBytesLE(chain), Throws.TypeOf<InsufficientExecutionStackException>());
		Assert.That(() => new BinarySerializer().CalculateSize(chain), Throws.TypeOf<InsufficientExecutionStackException>());
	}

	#endregion

	#region Type codes

	[Test]
	public void CollectionSerializers_AreDefaultRegistrations() {
		Assert.That(SerializerFactory.Default.ContainsSerializer(typeof(Queue<int>)), Is.True);
		Assert.That(SerializerFactory.Default.ContainsSerializer(typeof(Stack<int>)), Is.True);
		Assert.That(SerializerFactory.Default.ContainsSerializer(typeof(LinkedList<int>)), Is.True);
		Assert.That(SerializerFactory.Default.ContainsSerializer(typeof(SortedList<string, int>)), Is.True);
	}

	[Test]
	public void UserTypes_TakeCodesAboveTheReservedBand() {
		// Default registrations, the collection serializers included, stay below the band's end, so adding one never moves a user type's code
		Assert.That(SerializerFactory.Default.MaxGeneratedTypeCode, Is.LessThan(SerializerFactory.UserTypeCodeStart));
		Assert.That(SerializerFactory.Default.ContainsSerializer(typeof(Queue<>)), Is.True);
		Assert.That(SerializerFactory.Default.ContainsSerializer(typeof(Stack<>)), Is.True);
		var factory = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<QueueOwner>();
		Assert.That(factory.FindRegistration(typeof(QueueOwner)).TypeCode, Is.EqualTo(SerializerFactory.UserTypeCodeStart));
		Assert.That(factory.FindRegistration(typeof(Trailer)).TypeCode, Is.EqualTo(SerializerFactory.UserTypeCodeStart + 1));
		Assert.That(factory.GenerateTypeCode(), Is.EqualTo(SerializerFactory.UserTypeCodeStart + 2));
	}

	[Test]
	public void CollectionSerializer_IsResolvedFromTheDefaultFactory() {
		var serializer = SerializerFactory.Default.GetSerializer<Queue<string>>();
		var queue = new Queue<string>(new[] { "first", "last" });
		Assert.That(serializer.DeserializeBytesLE(serializer.SerializeBytesLE(queue)), Is.EqualTo(queue));
	}

	[Test]
	public void AutoBuiltFactory_RoundTripsCollectionMembers([Values] Endianness endianness) {
		var factory = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<QueueOwner>();
		var serializer = factory.GetSerializer<QueueOwner>();
		var owner = new QueueOwner { Items = new Queue<int>(new[] { 1, 2, 3 }), Trailer = new Trailer { Value = 9 }, Trailers = new Stack<Trailer>(new[] { new Trailer { Value = 4 } }) };
		var bytes = serializer.SerializeToBytes(owner, endianness);
		Assert.That(serializer.CalculateSize(owner), Is.EqualTo(bytes.Length));
		var copy = serializer.DeserializeBytes(bytes, endianness);
		Assert.That(copy.Items, Is.EqualTo(owner.Items));
		Assert.That(copy.Trailer.Value, Is.EqualTo(9));
		Assert.That(copy.Trailers.Single().Value, Is.EqualTo(4));
	}

	[Test]
	public void SerializedFactory_RebuildsCollectionAndStructRegistrations([Values] Endianness endianness) {
		// The collection member is assembled last: a factory cannot yet be captured once a registration follows a collection member's
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<Segment>().RegisterAutoBuild<TrailingQueueOwner>();
		var factorySerializer = new SerializerFactorySerializer();
		var restored = factorySerializer.DeserializeBytes(factorySerializer.SerializeToBytes(original, endianness), endianness);
		foreach (var type in original.RegisteredTypes)
			Assert.That(restored.FindRegistration(type).TypeCode, Is.EqualTo(original.FindRegistration(type).TypeCode));
		Assert.That(restored.GetSerializer<Queue<int>>(), Is.Not.Null);

		// Bytes written through the original factory are read through the rebuilt one
		var owner = new TrailingQueueOwner { Items = new Queue<int>(new[] { 1, 2, 3 }), Trailer = new Trailer { Value = 9 } };
		var ownerCopy = restored.GetSerializer<TrailingQueueOwner>().DeserializeBytes(original.GetSerializer<TrailingQueueOwner>().SerializeToBytes(owner, endianness), endianness);
		Assert.That(ownerCopy.Items, Is.EqualTo(owner.Items));
		Assert.That(ownerCopy.Trailer.Value, Is.EqualTo(9));
		var segment = new Segment { From = new PlainPoint { X = 1, Label = "from" }, To = new PlainPoint { Y = 2 } };
		var segmentCopy = restored.GetSerializer<Segment>().DeserializeBytes(original.GetSerializer<Segment>().SerializeToBytes(segment, endianness), endianness);
		Assert.That(segmentCopy.From, Is.EqualTo(segment.From));
		Assert.That(segmentCopy.To, Is.EqualTo(segment.To));
	}

	#endregion

	#region Auxiliary

	private static IEnumerable<TestCaseData> Values() {
		yield return new TestCaseData(5).SetName("Value_RoundTrips(int)");
		yield return new TestCaseData("text").SetName("Value_RoundTrips(string)");
		yield return new TestCaseData(1.5m).SetName("Value_RoundTrips(decimal)");
		yield return new TestCaseData(Colour.Green).SetName("Value_RoundTrips(enum)");
		yield return new TestCaseData(Guid.Parse("11111111-2222-3333-4444-555555555555")).SetName("Value_RoundTrips(Guid)");
		yield return new TestCaseData(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc)).SetName("Value_RoundTrips(DateTime)");
		yield return new TestCaseData(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(3))).SetName("Value_RoundTrips(DateTimeOffset)");
		yield return new TestCaseData(TimeSpan.FromSeconds(90)).SetName("Value_RoundTrips(TimeSpan)");
		yield return new TestCaseData(new byte[] { 1, 2, 3 }).SetName("Value_RoundTrips(byte[])");
		yield return new TestCaseData((object)new[] { "a", null, "c" }).SetName("Value_RoundTrips(string[])");
		yield return new TestCaseData(new KeyValuePair<string, int>("a", 1)).SetName("Value_RoundTrips(KeyValuePair)");
		yield return new TestCaseData(Tuple.Create(1, "a")).SetName("Value_RoundTrips(Tuple)");
		yield return new TestCaseData((1, "a")).SetName("Value_RoundTrips(ValueTuple)");
		yield return new TestCaseData(new ArrayList { 1, "a" }).SetName("Value_RoundTrips(ArrayList)");
		yield return new TestCaseData(new PlainPoint { X = 1, Y = 2, Label = "point" }).SetName("Value_RoundTrips(struct)");
	}

	private static T RoundTrip<T>(T value) {
		var serializer = new BinarySerializer();
		var bytes = serializer.SerializeBytesLE(value);
		Assert.That(serializer.CalculateSize(value), Is.EqualTo(bytes.Length));
		return (T)serializer.DeserializeBytesLE(bytes);
	}

	// Round-trips an empty, single-item and several-item collection on its own, as a typed member, as an object member and as a null member
	private static void AssertCollectionRoundTrips<TCollection>(Func<int, TCollection> create, bool ordered = true) where TCollection : class, IEnumerable {
		foreach (var count in new[] { 0, 1, 5 }) {
			var collection = create(count);
			AssertSameItems(RoundTrip(collection), collection);

			var typed = RoundTrip(new Holder<TCollection> { Value = collection, Tail = 77 });
			AssertSameItems(typed.Value, collection);
			Assert.That(typed.Tail, Is.EqualTo(77));

			var untyped = RoundTrip(new Holder<object> { Value = collection, Tail = 77 });
			AssertSameItems(untyped.Value, collection);
			Assert.That(untyped.Tail, Is.EqualTo(77));
		}

		var empty = RoundTrip(new Holder<TCollection> { Tail = 77 });
		Assert.That(empty.Value, Is.Null);
		Assert.That(empty.Tail, Is.EqualTo(77));

		void AssertSameItems(object actual, TCollection expected) {
			Assert.That(actual, Is.TypeOf<TCollection>());
			if (ordered)
				Assert.That(((IEnumerable)actual).Cast<object>(), Is.EqualTo(expected.Cast<object>()));
			else
				Assert.That(((IEnumerable)actual).Cast<object>(), Is.EquivalentTo(expected.Cast<object>()));
		}
	}

	private static Link CreateChain(int length) {
		var head = new Link();
		var tail = head;
		for (var i = 1; i < length; i++)
			tail = tail.Next = new Link();
		return head;
	}

	#endregion

	#region Inner Types

	public enum Colour {
		Red,
		Green = 5
	}

	public class Holder<T> {
		public T Value { get; set; }

		public int Tail { get; set; }
	}

	public class Node {
		public string Name { get; set; }

		public LinkedList<Node> Children { get; set; }

		public Stack<object> Pending { get; set; }
	}

	public class Link {
		public Link Next { get; set; }
	}

	public class Trailer {
		public int Value { get; set; }
	}

	public class QueueOwner {
		public Queue<int> Items { get; set; }

		public Trailer Trailer { get; set; }

		public Stack<Trailer> Trailers { get; set; }
	}

	public class TrailingQueueOwner {
		public Trailer Trailer { get; set; }

		public Queue<int> Items { get; set; }
	}

	public class NullableOwner {
		public int? Number { get; set; }

		public int? Missing { get; set; }

		public Colour? Colour { get; set; }

		public PlainPoint? Point { get; set; }
	}

	public class PointOwner {
		public PlainPoint Point { get; set; }

		public object Boxed { get; set; }

		public PlainPoint[] Array { get; set; }

		public List<PlainPoint> List { get; set; }

		public Dictionary<string, PlainPoint> Map { get; set; }

		public Segment Nested { get; set; }
	}

	public struct PlainPoint {
		public int X;

		public int Y { get; set; }

		public string Label { get; set; }
	}

	public struct ConstructedPoint {
		public int X;

		public ConstructedPoint() {
			X = 0;
			Y = 0;
			Label = null;
		}

		public int Y { get; set; }

		public string Label { get; set; }
	}

	public struct Segment {
		public PlainPoint From { get; set; }

		public PlainPoint To { get; set; }
	}

	public readonly record struct Measurement(double Amount, string Unit);

	public readonly struct Opaque {
		private readonly int _value;

		public Opaque(int value) => _value = value;

		public int Value => _value;
	}

	#endregion
}
