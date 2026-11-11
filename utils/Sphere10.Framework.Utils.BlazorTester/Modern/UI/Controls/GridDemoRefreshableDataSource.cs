// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

/// <summary>Refreshes the demo capture by publishing the next revision of every row as a fresh source; a polling decorator supplies the live cadence.</summary>
public sealed class GridDemoRefreshableDataSource : RefreshableDataSourceBase<GridDemoDataSource.Row, GridDemoDataSource> {

	public GridDemoRefreshableDataSource()
		: base(new GridDemoDataSource(GridDemoDataSource.InitialRows())) {
	}

	/// <summary>The rows of the served capture.</summary>
	public GridDemoDataSource.Row[] Rows => Current.Rows;

	/// <summary>Publishes the next revision of every row now, retaining stable IDs and membership.</summary>
	public void PublishNextRevision() => Publish(CreateDataSource());

	/// <summary>Removes a local sample from the served capture and every later one; the grid publishes that removal separately.</summary>
	public void Remove(int id) => Publish(new GridDemoDataSource(Current.Rows.Where(row => row.Id != id)));

	protected override GridDemoDataSource CreateDataSource() => new GridDemoDataSource(Current.Rows.Select(GridDemoDataSource.NextRevision));
}
