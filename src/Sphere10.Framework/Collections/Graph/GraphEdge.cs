// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public class GraphEdge<TNode, TEdge, TWeight> : IGraphEdge<TNode, TEdge, TWeight>
	where TNode : IGraph<TNode, TEdge, TWeight>
	where TEdge : IGraphEdge<TNode, TEdge, TWeight>, new() {
	public virtual TWeight Weight { get; set; }
	public virtual TNode Source { get; set; }
	public virtual TNode Destination { get; set; }
}

public class GraphEdge<TNode> : GraphEdge<TNode, GraphEdge<TNode>, int>, IGraphEdge<TNode>
	where TNode : IGraph<TNode> {
}

public class GraphEdge : GraphEdge<Graph>, IGraphEdge {
}
