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

namespace Sphere10.Framework;

/// <summary>A detached bounded sampled call tree. Percentages use all supplied sample observations, including explicitly omitted/missing stacks.</summary>
public sealed class SampledCallTree<TFrame> {
	private SampledCallTree() {
	}

	public SampledCallTreeNode<TFrame>[] Nodes { get; private init; } = Array.Empty<SampledCallTreeNode<TFrame>>();
	public long TotalSamples { get; private init; }
	public long CountedSamples { get; private init; }
	public long MissingStackSamples { get; private init; }
	public long DepthLimitedSamples { get; private init; }
	public long NodeLimitedSamples { get; private init; }

	/// <summary>Each array is one root-to-leaf observation with weight one. Keys/comparer must be stable and include any actor/module context needed to distinguish addresses.</summary>
	/// <remarks>Empty/null stacks contribute to the denominator. A stack exceeding a depth/node bound is wholly omitted; its prefix is never mislabeled as an exclusive leaf.</remarks>
	public static SampledCallTree<TFrame> Build(IEnumerable<TFrame[]> samples, int maximumNodes = 100000, int maximumDepth = 128, IEqualityComparer<TFrame> frameComparer = null, Action checkpoint = null) {
		Guard.ArgumentNotNull(samples, nameof(samples));
		Guard.ArgumentInRange(maximumNodes, 1, 1000000, nameof(maximumNodes));
		Guard.ArgumentInRange(maximumDepth, 1, 1024, nameof(maximumDepth));
		frameComparer ??= ComparerFactory.Default.GetEqualityComparer<TFrame>();
		var roots = new Dictionary<TFrame, Node>(frameComparer);
		var nodes = new List<Node>();
		long total = 0, counted = 0, missing = 0, depthLimited = 0, nodeLimited = 0;
		foreach (var frames in samples) {
			checkpoint?.Invoke();
			total++;
			if (frames == null || frames.Length == 0) {
				missing++;
				continue;
			}
			if (frames.Length > maximumDepth) {
				depthLimited++;
				continue;
			}
			var known = roots;
			var required = 0;
			for (var index = 0; index < frames.Length; index++) {
				Guard.Argument(frames[index] is not null, nameof(samples), "A frame key must not be null; use an explicit unknown-frame key or an empty stack.");
				if (known == null || !known.TryGetValue(frames[index], out var existing)) {
					required = frames.Length - index;
					break;
				}
				known = existing.Children;
			}
			if (nodes.Count + required > maximumNodes) {
				nodeLimited++;
				continue;
			}
			Node parent = null;
			for (var index = 0; index < frames.Length; index++) {
				var siblings = parent == null ? roots : parent.Children ??= new Dictionary<TFrame, Node>(frameComparer);
				if (!siblings.TryGetValue(frames[index], out var node)) {
					node = new Node(nodes.Count + 1, parent?.Id, index, frames[index]);
					siblings.Add(frames[index], node);
					nodes.Add(node);
				}
				node.Inclusive++;
				parent = node;
			}
			parent.Exclusive++;
			counted++;
		}
		return new SampledCallTree<TFrame> {
			TotalSamples = total, CountedSamples = counted, MissingStackSamples = missing, DepthLimitedSamples = depthLimited, NodeLimitedSamples = nodeLimited,
			Nodes = nodes.Select(node => new SampledCallTreeNode<TFrame>(node.Id, node.ParentId, node.Depth, node.Frame, node.Inclusive, node.Exclusive,
				total == 0 ? 0 : 100.0 * node.Inclusive / total, total == 0 ? 0 : 100.0 * node.Exclusive / total)).ToArray()
		};
	}

	#region Inner Types

	private sealed class Node {
		public Node(int id, int? parentId, int depth, TFrame frame) {
			Id = id;
			ParentId = parentId;
			Depth = depth;
			Frame = frame;
		}

		public int Id { get; }
		public int? ParentId { get; }
		public int Depth { get; }
		public TFrame Frame { get; }
		public long Inclusive { get; set; }
		public long Exclusive { get; set; }
		public Dictionary<TFrame, Node> Children { get; set; }
	}

	#endregion
}
