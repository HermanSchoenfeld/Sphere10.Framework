// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The sole entity-to-physical-row plan, including rows occupied by owned content rather than entities.</summary>
internal sealed class VisualGridDetailLayout {
	public VisualGridDetailLayout(VisualGridDetailRow[] rows, ISet<object> expanded) {
		Rows = rows;
		EntityRows = new int[rows.Length];
		var physical = 1;
		for (var index = 0; index < rows.Length; index++) {
			EntityRows[index] = physical;
			Entities.Add(physical++, rows[index].Entity);
			if (rows[index].Expandable && expanded.Contains(rows[index].Identity))
				Details.Add(physical++, rows[index].Identity);
		}
		RowCount = physical;
	}

	public VisualGridDetailRow[] Rows { get; }
	public int[] EntityRows { get; }
	public Dictionary<int, object> Entities { get; } = new();
	public Dictionary<int, object> Details { get; } = new();
	public int RowCount { get; }

	public bool SameShape(VisualGridDetailLayout other) {
		return other != null && RowCount == other.RowCount && EntityRows.AsSpan().SequenceEqual(other.EntityRows)
			&& Details.Count == other.Details.Count && Details.All(pair => other.Details.TryGetValue(pair.Key, out var identity) && Equals(pair.Value, identity));
	}
}
