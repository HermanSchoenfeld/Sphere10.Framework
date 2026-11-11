// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

/// <summary>One deterministic local capture for exercising the browser grid without a native collector or external service.</summary>
public sealed class GridDemoDataSource : ListDataSource<GridDemoDataSource.Row> {

	public GridDemoDataSource(IEnumerable<Row> rows)
		: base(rows) {
		DefaultSortProperty = nameof(Row.Id);
	}

	/// <summary>The rows of the first capture.</summary>
	public static IEnumerable<Row> InitialRows() => Enumerable.Range(1, 96).Select(id => new Row(
		id,
		$"Sample {id:000}",
		id % 3 == 0 ? "Network" : id % 3 == 1 ? "Processor" : "Memory",
		0,
		id % 100,
		id > 40 ? $"A longer description from a later page for sample {id:000}; established column widths should remain unchanged." : $"Sample reading {id:000}"
	));

	/// <summary>Replaces every row instance while retaining stable IDs and collection membership.</summary>
	public static Row NextRevision(Row row) => row with { Revision = row.Revision + 1, Usage = (row.Usage + row.Id % 7 + 1) % 100 };

	public Row[] Rows => List.ToArray();

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	public override async Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		// Deliberately visible latency exercises the actual grid refresh, not only a collector preceding it.
		await Task.Delay(350).ConfigureAwait(false);
		return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ConfigureAwait(false);
	}

	protected override Row RefreshMethod(Row entity) => NextRevision(entity);

	public sealed record Row(int Id, string Name, string Group, int Revision, double Usage, string Details);
}
