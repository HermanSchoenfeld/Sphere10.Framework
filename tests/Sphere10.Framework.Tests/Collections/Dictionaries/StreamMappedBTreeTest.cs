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
public class StreamMappedBTreeTests : BTreeTests {

	protected override BTreeBase<K, V> CreateInstance<K, V>(int order, IComparer<K> comparer = null) {
		var KeySerializer = ItemSerializer<K>.Default;
		var ValueSerializer = ItemSerializer<V>.Default;
		if (!KeySerializer.IsConstantSize || !ValueSerializer.IsConstantSize)
			Assert.Ignore("StreamMappedBTree requires constant-size serializers; skipping for this type combination.");
		var Stream = new MemoryStream();
		return new StreamMappedBTree<K, V>(order, Stream, KeySerializer, ValueSerializer, comparer);
	}
}
