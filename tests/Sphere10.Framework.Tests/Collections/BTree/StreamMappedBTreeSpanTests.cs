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

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamMappedBTreeSpanTests {

	[Test]
	public void ShortSpanReadsPreserveMutationsRecyclingAndReopen(
		[Values] BTreeType type,
		[Values] Endianness endianness,
		[Values(49, 50, 51, 256)] int order) {
		using var stream = new ShortReadMemoryStream();
		var expected = new SortedDictionary<int, long>();
		var count = order * 3 + 17;
		var tree = CreateTree(type, order, stream, endianness);
		using ((IDisposable)tree) {
			// Shuffle insertion so record shifts, splits and both key directions are exercised.
			foreach (var key in Enumerable.Range(0, count).OrderBy(key => unchecked((uint)key * 2654435761U))) {
				var value = 10000000000L + key * 17L;
				tree.Add(key, value);
				expected.Add(key, value);
			}
			AssertMatchesReference(tree, expected);
			foreach (var key in expected.Keys.Where(key => key % 5 == 0).ToArray()) {
				expected[key] = -expected[key];
				tree[key] = expected[key];
			}

			// Merge deleted ranges, then grow again through recycled node storage.
			foreach (var key in expected.Keys.Where(key => key % 3 != 0).OrderByDescending(key => key).ToArray()) {
				Assert.That(tree.Remove(key), Is.True);
				expected.Remove(key);
			}
			AssertMatchesReference(tree, expected);
			foreach (var key in Enumerable.Range(count, count / 2)) {
				var value = 20000000000L + key * 31L;
				tree.Add(key, value);
				expected.Add(key, value);
			}
			AssertMatchesReference(tree, expected);
			Assert.That(tree.Remove(-1), Is.False);
			Assert.That(tree.TryGetValue(-1, out _), Is.False);
		}
		Assert.That(stream.CanRead && stream.CanWrite && stream.CanSeek, Is.True, "Disposing a tree must leave its caller-owned stream open.");
		Assert.That(stream.ShortSpanReads, Is.GreaterThan(0), "The backing stream must exercise the full-read loop through virtual span reads.");

		stream.Position = 7;
		var reopened = CreateTree(type, order, stream, endianness);
		using ((IDisposable)reopened) {
			AssertMatchesReference(reopened, expected);
			var key = expected.Keys.First();
			expected[key] = long.MinValue + key;
			reopened[key] = expected[key];
			var removed = expected.Keys.Last();
			Assert.That(reopened.Remove(removed), Is.True);
			expected.Remove(removed);
			reopened.Add(-7, long.MaxValue);
			expected.Add(-7, long.MaxValue);
			AssertMatchesReference(reopened, expected);
		}
		Assert.That(stream.CanRead && stream.CanWrite && stream.CanSeek, Is.True);
		var final = CreateTree(type, order, stream, endianness);
		using ((IDisposable)final)
			AssertMatchesReference(final, expected);
	}

	[Test]
	public void SerializerFailureDuringReadOrExistingValueReplacementCanBeRetried(
		[Values] BTreeType type,
		[Values] Endianness endianness,
		[Values(49, 51)] int order,
		[Values(false, true)] bool writing) {
		using var stream = new ShortReadMemoryStream();
		var serializer = new FailingValueSerializer();
		var tree = CreateTree(type, order, stream, endianness, serializer);
		var expected = new SortedDictionary<int, long> { [1] = 10000000001L, [3] = 10000000003L };
		using ((IDisposable)tree) {
			foreach (var entry in expected)
				tree.Add(entry.Key, entry.Value);
			var original = stream.ToArray();
			if (writing)
				serializer.FailNextSerialize = true;
			else
				serializer.FailNextDeserialize = true;

			// These operations have no structural mutation: a failed read or value serialization can be retried.
			Assert.That(() => {
				if (writing)
					tree[3] = 333;
				else
					tree.TryGetValue(3, out _);
			}, Throws.TypeOf<IOException>().With.Message.EqualTo("Injected value serializer failure"));
			Assert.That(serializer.FailNextSerialize || serializer.FailNextDeserialize, Is.False);
			Assert.That(stream.ToArray(), Is.EqualTo(original));
			AssertMatchesReference(tree, expected);
			tree[3] = 333;
			expected[3] = 333;
			AssertMatchesReference(tree, expected);
		}
		var reopened = CreateTree(type, order, stream, endianness);
		using ((IDisposable)reopened)
			AssertMatchesReference(reopened, expected);
		Assert.That(stream.CanRead && stream.CanWrite && stream.CanSeek, Is.True);
	}

	[Test]
	public void TruncatedNodeCannotBePartiallyRewrittenAndCanBeRetriedAfterRestoration(
		[Values] BTreeType type,
		[Values] Endianness endianness,
		[Values(49, 51)] int order) {
		using var stream = new ShortReadMemoryStream();
		var tree = CreateTree(type, order, stream, endianness);
		var expected = new SortedDictionary<int, long> { [1] = 11, [5] = 55 };
		using ((IDisposable)tree) {
			foreach (var entry in expected)
				tree.Add(entry.Key, entry.Value);
			var original = stream.ToArray();
			stream.SetLength(original.Length - 1);
			var truncated = stream.ToArray();

			// The missing byte is unused node padding; insertion still requires the complete record before modifying it.
			Assert.That(() => tree.Add(3, 33), Throws.TypeOf<EndOfStreamException>());
			Assert.That(tree.Count, Is.EqualTo(expected.Count));
			Assert.That(stream.ToArray(), Is.EqualTo(truncated));
			stream.SetLength(original.Length);
			stream.Position = 0;
			stream.Write(original.AsSpan());
			Assert.That(stream.ToArray(), Is.EqualTo(original));
			tree.Add(3, 33);
			expected.Add(3, 33);
			AssertMatchesReference(tree, expected);
		}
		var reopened = CreateTree(type, order, stream, endianness);
		using ((IDisposable)reopened)
			AssertMatchesReference(reopened, expected);
	}

	private static BTreeBase<int, long> CreateTree(BTreeType type, int order, Stream stream, Endianness endianness, IItemSerializer<long> valueSerializer = null)
		=> type == BTreeType.BTree
			? new StreamMappedBTree<int, long>(order, stream, PrimitiveSerializer<int>.Instance, valueSerializer ?? PrimitiveSerializer<long>.Instance, Comparer<int>.Default, endianness)
			: new StreamMappedBTreePlus<int, long>(order, stream, PrimitiveSerializer<int>.Instance, valueSerializer ?? PrimitiveSerializer<long>.Instance, Comparer<int>.Default, endianness);

	private static void AssertMatchesReference(BTreeBase<int, long> tree, SortedDictionary<int, long> expected) {
		Assert.That(tree.Validate(out var error), Is.True, error);
		Assert.That(tree.Count, Is.EqualTo(expected.Count));
		Assert.That(tree.ToArray(), Is.EqualTo(expected.ToArray()));
		foreach (var entry in expected) {
			Assert.That(tree.TryGetValue(entry.Key, out var value), Is.True);
			Assert.That(value, Is.EqualTo(entry.Value));
		}
	}

	private sealed class ShortReadMemoryStream : MemoryStream {
		public int ShortSpanReads { get; private set; }

		public override int Read(Span<byte> destination) {
			var read = base.Read(destination[..Math.Min(destination.Length, 3)]);
			if (read > 0 && read < destination.Length)
				ShortSpanReads++;
			return read;
		}

		public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 3));
	}

	private sealed class FailingValueSerializer : ItemSerializerDecorator<long> {
		public FailingValueSerializer()
			: base(PrimitiveSerializer<long>.Instance) {
		}

		public bool FailNextSerialize { get; set; }

		public bool FailNextDeserialize { get; set; }

		public override void Serialize(long item, EndianBinaryWriter writer, SerializationContext context) {
			if (FailNextSerialize) {
				FailNextSerialize = false;
				throw new IOException("Injected value serializer failure");
			}
			base.Serialize(item, writer, context);
		}

		public override long Deserialize(EndianBinaryReader reader, SerializationContext context) {
			if (FailNextDeserialize) {
				FailNextDeserialize = false;
				throw new IOException("Injected value serializer failure");
			}
			return base.Deserialize(reader, context);
		}
	}
}
