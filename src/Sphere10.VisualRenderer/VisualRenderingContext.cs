// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

/// <summary>The traversal state for one render invocation; contains only visual data.</summary>
public sealed class VisualRenderingContext {

	private readonly VisualTraversal _traversal = new();

	private readonly Stack<int> _indices = new();

	private readonly Dictionary<VisualNode, string> _anchors = new(ReferenceEqualityComparer.Instance);

	private readonly HashSet<VisualValue> _values = new(ReferenceEqualityComparer.Instance);

	private int _nextAnchor;

	public VisualRenderingContext(DocumentBlock document, RenderOptions options) {
		this.Document = document;
		this.Options = options;
	}

	public DocumentBlock Document { get; }

	public RenderOptions Options { get; }

	public IReadOnlyList<VisualNode> RenderingStack => _traversal.Ancestors;

	public VisualNode CurrentRenderingNode => RenderingStack[^1];

	public int CurrentIndex => _indices.Peek();

	public VisualNode GetParentRenderingNode()
		=> RenderingStack.Count > 1 ? RenderingStack[^2] : null;

	public string GetAnchor(VisualNode node) {
		if (_anchors.TryGetValue(node, out var anchor))
			return anchor;
		anchor = node.Metadata.Anchor ?? node.Metadata.SourceId ?? "visual_" + ++_nextAnchor;
		_anchors.Add(node, anchor);
		return anchor;
	}

	internal void Enter(VisualNode node, int index) {
		_traversal.Enter(node);
		_indices.Push(index);
	}

	internal void Leave(VisualNode node) {
		_indices.Pop();
		_traversal.Leave(node);
	}

	internal void EnterValue(VisualValue value) {
		if (_values.Count >= 256 || !_values.Add(value))
			throw new InvalidOperationException("A visual property value contains a cycle or exceeds the maximum nesting depth.");
	}

	internal void LeaveValue(VisualValue value) => _values.Remove(value);
}
