// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

/// <summary>Supplies the grid demo with two root nodes, shared descendants and a cycle.</summary>
/// <remarks>
/// Shared services appears under both teams and links back to Alpha team. Independently paged child views let the demo exercise
/// sibling paging, shared nodes and cycle detection along the expanded branch.
/// </remarks>
public sealed class GridDemoHierarchyDataSource : GraphDataSourceBase<GraphNode<GridDemoHierarchyDataSource.Row>, GridDemoHierarchyDataSource.Row> {
	public GridDemoHierarchyDataSource()
		: base(CreateRoots()) {
	}

	public override object GetNodeKey(GraphNode<Row> node) => node.Entity.Id;

	protected override Row ResolveEntity(GraphNode<Row> node) => node.Entity;

	private static GraphNode<Row>[] CreateRoots() {
		var alpha = new GraphNode<Row>(new Row(1, "Alpha team"));
		var beta = new GraphNode<Row>(new Row(2, "Beta team"));
		var shared = new GraphNode<Row>(new Row(3, "Shared services"));
		alpha.Connections = new[] { shared }.Concat(Enumerable.Range(10, 8).Select(id => new GraphNode<Row>(new Row(id, $"Alpha member {id - 9}")))).ToArray();
		beta.Connections = new[] { shared };
		shared.Connections = new[] { alpha };
		return new[] { alpha, beta };
	}

	/// <summary>The node payload displayed by the hierarchy demo, with a stable identity and visible name.</summary>
	/// <param name="Id">The stable identity returned by the source's node-key selector.</param>
	/// <param name="Name">The name displayed and sorted in the demo grid.</param>
	public sealed record Row(int Id, string Name);
}
