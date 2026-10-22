// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Globally unique object identifier within an ObjectSpace.
/// Composed of a <see cref="DimensionIndex"/> (ordinal position in <see cref="ObjectSpaceDefinition.Dimensions"/>)
/// and an <see cref="ObjectIndex"/> (row position within that dimension's <see cref="StreamMappedRecyclableList{T}"/>).
/// Together these form a lightweight cross-dimension pointer used for external reference serialization and GC tracking.
/// </summary>
/// <remarks>
/// Constant serialized size: 10 bytes (2-byte short + 8-byte long).
/// Negative <see cref="ObjectIndex"/> values represent provisional IDs for newly created objects not yet persisted.
/// </remarks>
public readonly record struct ObjectSpaceObjectReference(short DimensionIndex, long ObjectIndex)
	: IEquatable<ObjectSpaceObjectReference>, IComparable<ObjectSpaceObjectReference> {

	/// <summary>
	/// Compares two references by dimension index first, then by object index within the dimension.
	/// This ordering groups references by dimension and sorts them by row position.
	/// </summary>
	public int CompareTo(ObjectSpaceObjectReference other) {
		// Primary sort: by dimension index so references in the same dimension are adjacent
		var dimensionComparison = DimensionIndex.CompareTo(other.DimensionIndex);
		if (dimensionComparison != 0)
			return dimensionComparison;

		// Secondary sort: by object (row) index within the dimension
		return ObjectIndex.CompareTo(other.ObjectIndex);
	}

	/// <summary>
	/// Human-readable representation for debugging, e.g. "(Dim:2, Row:17)".
	/// </summary>
	public override string ToString() => $"(Dim:{DimensionIndex}, Row:{ObjectIndex})";
}
