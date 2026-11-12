// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.ObjectModel;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>Serializes a <see cref="ReadOnlyCollection{T}"/> as an array of its items and reads it back as a read-only view of that array.</summary>
public class ReadOnlyCollectionSerializer<T> : ProjectedSerializer<T[], ReadOnlyCollection<T>> {

	public ReadOnlyCollectionSerializer(IItemSerializer<T> itemSerializer, SizeDescriptorStrategy sizeDescriptorStrategy = SizeDescriptorStrategy.UseCVarInt)
		: base(new ArraySerializer<T>(itemSerializer, sizeDescriptorStrategy), items => new ReadOnlyCollection<T>(items), collection => collection.ToArray()) {
	}
}
