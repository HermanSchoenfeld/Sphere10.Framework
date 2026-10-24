// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SerializationContextAllocationTests {
	[Test]
	public void ScalarDeserializationAvoidsReferenceMapAllocations([Values] bool activateEarly) {
		var items = new object[1000];
		for (var index = 0; index < items.Length; index++)
			items[index] = new object();
		using (var warmup = new SerializationContext())
			Register(warmup, items[0], activateEarly);
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var index = 0; index < items.Length; index++) {
			using var context = new SerializationContext();
			var reference = Register(context, items[index], activateEarly);
			if (reference != 0 || !ReferenceEquals(context.GetDeserializedObject(reference), items[index]))
				throw new InvalidOperationException("The scalar context did not retain its object identity.");
		}
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.WriteLine($"1000 scalar deserialization contexts (early activation={activateEarly}): {allocated:N0} allocated bytes.");
		Assert.That(allocated, Is.LessThan(512_000));
	}

	[Test]
	public void PromotionPreservesDistinctValueEqualObjects([Values] bool activateEarly) {
		using var context = new SerializationContext();
		var first = new ValueEqualObject();
		var second = new ValueEqualObject();
		Assert.That(Register(context, first, activateEarly), Is.Zero);
		Assert.That(Register(context, second, activateEarly), Is.EqualTo(1));
		Assert.That(context.GetDeserializedObject(0), Is.SameAs(first));
		Assert.That(context.GetDeserializedObject(1), Is.SameAs(second));
		context.NotifySerializingObject(first, out var firstIndex);
		context.NotifySerializingObject(second, out var secondIndex);
		Assert.That(firstIndex, Is.Zero);
		Assert.That(secondIndex, Is.EqualTo(1));
	}

	[Test]
	public void SizingSerializationAndDeserializationReuseTheFirstIndex() {
		using var context = new SerializationContext();
		var original = new object();
		context.NotifySizing(original, out var sizingIndex);
		Assert.That(context.IsSizingOrSerializingObject(original, out _), Is.True);
		context.NotifySized(sizingIndex);
		Assert.That(context.GetSizedOrSerializedObject(sizingIndex), Is.SameAs(original));
		context.NotifySerializingObject(original, out var serializedIndex);
		Assert.That(serializedIndex, Is.EqualTo(sizingIndex));
		context.NotifySerializedObject(serializedIndex);
		Assert.That(context.GetSerializedObject(serializedIndex), Is.SameAs(original));
		var rehydrated = new object();
		Assert.That(Register(context, rehydrated, true), Is.EqualTo(serializedIndex));
		Assert.That(context.GetDeserializedObject(serializedIndex), Is.SameAs(rehydrated));
		Assert.That(context.HasSizedOrSerializedObject(original, out _), Is.False);
		context.NotifySerializingObject(rehydrated, out var reusedIndex);
		Assert.That(reusedIndex, Is.EqualTo(serializedIndex));
	}

	[Test]
	public void NullSlotsAndFollowingObjectsKeepTheirOwnIndexes() {
		using var context = new SerializationContext();
		Assert.That(Register(context, null, false), Is.Zero);
		Assert.That(Register(context, null, false), Is.EqualTo(1));
		var item = new object();
		Assert.That(Register(context, item, false), Is.EqualTo(2));
		Assert.That(context.GetDeserializedObject(0), Is.Null);
		Assert.That(context.GetDeserializedObject(1), Is.Null);
		Assert.That(context.GetDeserializedObject(2), Is.SameAs(item));
	}

	[Test]
	public void SparseStatusUpdatesDoNotDisplaceTheFirstObject() {
		using var context = new SerializationContext();
		context.NotifySized(7);
		var item = new object();
		context.NotifySerializingObject(item, out var index);
		context.NotifySerializedObject(index);
		Assert.That(index, Is.Zero);
		Assert.That(context.GetSerializedObject(0), Is.SameAs(item));
		Assert.That(() => context.GetSizedOrSerializedObject(7), Throws.TypeOf<KeyNotFoundException>());
	}

	[Test]
	public void FinalizationCanReadObjectsBeforeTheyAreReleased() {
		var context = new SerializationContext();
		var item = new object();
		Register(context, item, false);
		var calls = new List<int>();
		context.RegisterFinalizationAction(() => {
			Assert.That(context.GetDeserializedObject(0), Is.SameAs(item));
			calls.Add(1);
		});
		context.RegisterFinalizationAction(() => calls.Add(2));
		context.Dispose();
		Assert.That(calls, Is.EqualTo(new[] { 1, 2 }));
		Assert.That(() => context.GetDeserializedObject(0), Throws.TypeOf<KeyNotFoundException>());
	}

	[Test]
	public void ReferenceRoundTripRetainsAliasingAndWireSize([Values] bool mixedContext) {
		var serializer = SerializerBuilder.For<ReferenceTriple>()
			.Serialize(item => item.First, new StringSerializer().AsReferenceSerializer())
			.Serialize(item => item.Second, new StringSerializer().AsReferenceSerializer())
			.Serialize(item => item.Third, new StringSerializer().AsReferenceSerializer())
			.Build().AsReferenceSerializer();
		var shared = new string("shared".ToCharArray());
		var item = new ReferenceTriple { First = shared, Second = shared, Third = new string("shared".ToCharArray()) };
		using var sizing = new SerializationContext();
		using var writing = new SerializationContext();
		using var reading = new SerializationContext();
		var size = serializer.CalculateSize(sizing, item);
		var bytes = serializer.SerializeBytesLE(item, mixedContext ? sizing : writing);
		var result = serializer.DeserializeBytesLE(bytes, mixedContext ? sizing : reading);
		Assert.That(bytes.LongLength, Is.EqualTo(size));
		Assert.That(result.First, Is.EqualTo(shared));
		Assert.That(result.First, Is.SameAs(result.Second));
		Assert.That(result.First, Is.Not.SameAs(result.Third));
		Assert.That(result.Third, Is.EqualTo(shared));
		TestContext.WriteLine($"Reference graph bytes (mixed context={mixedContext}): {Convert.ToHexString(bytes)}");
	}

	private static long Register(SerializationContext context, object item, bool activateEarly) {
		context.NotifyDeserializingObject(out var index);
		if (activateEarly)
			context.SetDeserializingItem(item);
		context.NotifyDeserializedObject(item, index);
		return index;
	}

	public sealed class ReferenceTriple {
		public string First { get; set; }
		public string Second { get; set; }
		public string Third { get; set; }
	}

	private sealed class ValueEqualObject {
		public override bool Equals(object obj) => obj is ValueEqualObject;
		public override int GetHashCode() => throw new InvalidOperationException("Reference tracking must not call value hashing.");
	}
}
