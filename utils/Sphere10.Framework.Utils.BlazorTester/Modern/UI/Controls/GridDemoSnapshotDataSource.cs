// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

/// <summary>Deterministic local snapshots for exercising the browser grid without a native collector or external service.</summary>
public sealed class GridDemoSnapshotDataSource : RefreshableSnapshotDataSource<GridDemoSnapshotDataSource.Row, int> {

	public GridDemoSnapshotDataSource()
		: base(row => row.Id, NextRevision) {
		DefaultSortProperty = nameof(Row.Id);
		ReplaceSnapshot(Enumerable.Range(1, 96).Select(id => new Row(
			id,
			$"Sample {id:000}",
			id % 3 == 0 ? "Network" : id % 3 == 1 ? "Processor" : "Memory",
			0,
			id % 100,
			id > 40 ? $"A longer description from a later page for sample {id:000}; established column widths should remain unchanged." : $"Sample reading {id:000}"
		)).ToArray());
	}

	/// <summary>Replaces every row instance while retaining stable IDs and collection membership.</summary>
	public bool PublishNextSnapshot() => TryTransformSnapshot(rows => rows.Select(NextRevision).ToArray());

	/// <summary>Removes a local sample from subsequent source queries; the grid publishes that removal separately.</summary>
	public bool Remove(int id) => TryTransformSnapshot(rows => rows.Where(row => row.Id != id).ToArray());

	public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		// Deliberately visible latency exercises the actual grid refresh, not only a collector preceding it.
		await Task.Delay(350).ConfigureAwait(false);
		return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ConfigureAwait(false);
	}

	private static Row NextRevision(Row row) => row with { Revision = row.Revision + 1, Usage = (row.Usage + row.Id % 7 + 1) % 100 };

	public sealed record Row(int Id, string Name, string Group, int Revision, double Usage, string Details);
}
