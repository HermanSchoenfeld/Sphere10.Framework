// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

public sealed class EnumerableSerializer<T> : CollectionSerializerBase<IEnumerable<T>, T> {

	public EnumerableSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy) 
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(IEnumerable<T> collection) => collection.LongCount();

	protected override IEnumerable<T> Activate(long capacity) => new T[capacity];

	protected override void SetItem(IEnumerable<T> collection, long index, T item) => ((T[])collection)[index] = item;
}

