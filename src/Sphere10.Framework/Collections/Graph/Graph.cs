// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

public class Graph<TNode, TEdge, TWeight> : IGraph<TNode, TEdge, TWeight>
	where TNode : IGraph<TNode, TEdge, TWeight>
	where TEdge : IGraphEdge<TNode, TEdge, TWeight>, new() {
	public virtual IEnumerable<TEdge> Edges { get; set; }

	public ISimpleGraph ToSimpleGraph(Func<ISimpleGraph, TEdge> edgeCreator = null) {
		return new SimpleGraphAdapter<TNode, TEdge, TWeight>((TNode)(object)this, edgeCreator);
	}
}

public class Graph<TNode> : Graph<TNode, GraphEdge<TNode>, int>, IGraph<TNode>
	where TNode : IGraph<TNode> {
}

public class Graph : Graph<Graph>, IGraph {
}
