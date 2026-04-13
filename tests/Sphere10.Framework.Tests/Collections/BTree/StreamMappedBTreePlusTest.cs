// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamMappedBTreePlusTests : BTreePlusTests {

	protected override BTreeBase<K, V> CreateInstance<K, V>(int order, IComparer<K> comparer = null) {
		var KeySerializer = ItemSerializer<K>.Default;
		if (KeySerializer is ReferenceSerializer<string>)
			KeySerializer = KeySerializer.AsConstantSize(MaxStringValueLength*sizeof(char));

		var ValueSerializer = ItemSerializer<V>.Default;
		if (ValueSerializer is ReferenceSerializer<string>)
			ValueSerializer = ValueSerializer.AsConstantSize(MaxStringValueLength*sizeof(char));
		if (!KeySerializer.IsConstantSize || !ValueSerializer.IsConstantSize)
			Assert.Ignore("StreamMappedBTreePlus requires constant-size serializers; skipping for this type combination.");
		var Stream = new MemoryStream();
		return new StreamMappedBTreePlus<K, V>(order, Stream, KeySerializer, ValueSerializer, comparer);
	}

	
	// Integration test will run this, per iteration. It will duplicate stream and recreate the current tree from the stream,
	// then run the same test on the new tree to ensure that the stream data is correct and can be read back into a new instance
	// of the tree.
	protected override void IntergrationPerIterationTest<K, V>(BTreeBase<K, V> tree) {
		base.IntergrationPerIterationTest(tree);
		// Test that a new instance of the tree can read the data from the stream
		var streamMappedTree = (StreamMappedBTreePlus<K, V>)tree;
		byte[] streamData;
		using (streamMappedTree.Stream.EnterRestoreSeekPositionScope()) { 
			streamData = streamMappedTree.Stream.ToArray();
		}
		var newTree = new StreamMappedBTreePlus<K, V>(streamMappedTree.Order, new MemoryStream(streamData), streamMappedTree.KeySerializer, streamMappedTree.ValueSerializer, streamMappedTree.Comparer);

		base.IntergrationPerIterationTest(newTree);

		Assert.That(newTree.Count, Is.EqualTo(tree.Count));
		Assert.That(newTree, Is.EqualTo(tree).Using(new KeyValuePairEqualityComparer<K,V>(streamMappedTree.Comparer.ToEqualityComparer(), EqualityComparer<V>.Default )));

	}
}
