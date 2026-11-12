// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>Serializes the .NET <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>, which is named in full because this namespace has its own ObservableCollection.</summary>
public class ObservableCollectionSerializer<T> : CollectionSerializerBase<System.Collections.ObjectModel.ObservableCollection<T>, T> {

	public ObservableCollectionSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(itemSerializer, sizeDescriptorStrategy) {
	}

	protected override long GetLength(System.Collections.ObjectModel.ObservableCollection<T> collection) => collection.Count;

	protected override System.Collections.ObjectModel.ObservableCollection<T> Activate(long capacity) => new();

	protected override void SetItem(System.Collections.ObjectModel.ObservableCollection<T> collection, long index, T item) {
		Guard.Ensure(collection.Count == index, "Unexpected index");
		collection.Add(item);
	}
}
