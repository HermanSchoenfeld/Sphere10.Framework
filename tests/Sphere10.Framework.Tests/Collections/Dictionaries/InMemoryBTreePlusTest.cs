// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class InMemoryBTreePlusTests : BTreePlusTests {

	protected override BTreeBase<K, V> CreateInstance<K, V>(int order, IComparer<K> comparer = null)
		=> new InMemoryBTreePlus<K, V>(order, comparer);
}
