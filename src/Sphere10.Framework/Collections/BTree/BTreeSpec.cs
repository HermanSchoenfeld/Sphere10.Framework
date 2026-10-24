// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>Configures a concrete B-tree type and an order of at least three.</summary>
public sealed record BTreeSpec {
	public BTreeSpec(BTreeType type, int order) {
		if (!Enum.IsDefined(type))
			throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown B-tree type.");
		if (order < 3)
			throw new ArgumentOutOfRangeException(nameof(order), order, "B-tree order must be at least 3.");
		Type = type;
		Order = order;
	}

	public BTreeType Type { get; }

	public int Order { get; }
}
