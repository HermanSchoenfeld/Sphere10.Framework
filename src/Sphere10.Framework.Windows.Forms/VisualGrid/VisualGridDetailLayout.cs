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

/// <summary>Maps displayed entities and expanded detail hosts to physical rows in SourceGrid.</summary>
/// <remarks>
/// Each expanded entity receives a run of physical rows for its detail control, spanned by one host cell. SourceGrid scrolls
/// by whole rows, so a detail taller than the viewport is split into row-high slices that the outer grid scrolls through.
/// Binding, hit testing and reflow use these mappings to keep entity rows aligned with their native cells as details open,
/// close and change height.
/// </remarks>
internal sealed class VisualGridDetailLayout {
	public VisualGridDetailLayout(VisualGridDetailRow[] rows, ISet<object> expanded, Func<object, int> detailRowCount = null) {
		Rows = rows;
		EntityRows = new int[rows.Length];
		var physical = 1;
		for (var index = 0; index < rows.Length; index++) {
			EntityRows[index] = physical;
			Entities.Add(physical++, rows[index].Entity);
			if (rows[index].Expandable && expanded.Contains(rows[index].Identity)) {
				var count = Math.Max(1, detailRowCount?.Invoke(rows[index].Identity) ?? 1);
				Details.Add(physical, rows[index].Identity);
				DetailRowCounts.Add(physical, count);
				for (var slice = 0; slice < count; slice++)
					DetailRows.Add(physical + slice);
				physical += count;
			}
		}
		RowCount = physical;
	}

	public VisualGridDetailRow[] Rows { get; }
	public int[] EntityRows { get; }
	public Dictionary<int, object> Entities { get; } = new();

	/// <summary>The first physical row of each detail host, mapped to the expanded row's identity.</summary>
	public Dictionary<int, object> Details { get; } = new();

	/// <summary>The number of physical rows each detail host spans, keyed by its first physical row.</summary>
	public Dictionary<int, int> DetailRowCounts { get; } = new();

	/// <summary>Every physical row covered by a detail host.</summary>
	public HashSet<int> DetailRows { get; } = new();

	public int RowCount { get; }

	public bool SameShape(VisualGridDetailLayout other)
		=> other != null && RowCount == other.RowCount && EntityRows.AsSpan().SequenceEqual(other.EntityRows)
			&& Details.Count == other.Details.Count && Details.All(pair => other.Details.TryGetValue(pair.Key, out var identity) && Equals(pair.Value, identity))
			&& DetailRowCounts.All(pair => other.DetailRowCounts.TryGetValue(pair.Key, out var count) && count == pair.Value);
}
