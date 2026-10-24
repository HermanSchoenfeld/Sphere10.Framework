// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public interface IGraphEdge<TNode, TEdge, TWeight>
	where TNode : IGraph<TNode, TEdge, TWeight>
	where TEdge : IGraphEdge<TNode, TEdge, TWeight>, new() {
	TWeight Weight { get; set; }
	TNode Source { get; set; }
	TNode Destination { get; set; }
}

public interface IGraphEdge<TNode> : IGraphEdge<TNode, GraphEdge<TNode>, int>
	where TNode : IGraph<TNode> {
}

public interface IGraphEdge : IGraphEdge<Graph> {
}
