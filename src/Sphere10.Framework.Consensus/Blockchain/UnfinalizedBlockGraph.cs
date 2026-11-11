// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework;

namespace Sphere10.Framework.Consensus;

/// <summary>
/// Models the unfinalized frontier of a blockchain as an upside-down tree.
/// The root (finalized head) is at the bottom; potential blocks grow upward as competing branches.
/// All nodes are indexed by block ID for O(1) lookup.
/// </summary>
public class UnfinalizedBlockGraph<TBlock, TState, TBlockID, TWeight, TOperationID>
	where TBlock : IBlockchainBlock<TState, TBlockID, TWeight, TOperationID>
	where TState : IBlockchainState {

	private readonly Dictionary<TBlockID, LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> _allNodes;
	private readonly Dictionary<TBlockID, LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> _potentialHeads;
	private readonly IEqualityComparer<TBlockID> _blockIDComparer;
	private readonly IEqualityComparer<TOperationID> _operationIDComparer;
	private readonly IWeightAggregator<TWeight> _weightAggregator;

	public UnfinalizedBlockGraph(IWeightAggregator<TWeight> weightAggregator)
		: this(EqualityComparer<TBlockID>.Default, EqualityComparer<TOperationID>.Default, weightAggregator) {
	}

	public UnfinalizedBlockGraph(IEqualityComparer<TBlockID> blockIDComparer, IWeightAggregator<TWeight> weightAggregator)
		: this(blockIDComparer, EqualityComparer<TOperationID>.Default, weightAggregator) {
	}

	public UnfinalizedBlockGraph(IEqualityComparer<TBlockID> blockIDComparer, IEqualityComparer<TOperationID> operationIDComparer, IWeightAggregator<TWeight> weightAggregator) {
		Guard.ArgumentNotNull(blockIDComparer, nameof(blockIDComparer));
		Guard.ArgumentNotNull(operationIDComparer, nameof(operationIDComparer));
		Guard.ArgumentNotNull(weightAggregator, nameof(weightAggregator));

		_blockIDComparer = blockIDComparer;
		_operationIDComparer = operationIDComparer;
		_weightAggregator = weightAggregator;
		_allNodes = new Dictionary<TBlockID, LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>>(_blockIDComparer);
		_potentialHeads = new Dictionary<TBlockID, LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>>(_blockIDComparer);
	}

	public IReadOnlyDictionary<TBlockID, LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> PotentialHeads => _potentialHeads;

	public IReadOnlyDictionary<TBlockID, LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> Nodes => _allNodes;

	public IEqualityComparer<TBlockID> BlockIDComparer => _blockIDComparer;

	/// <summary>
	/// Sets the finalized (root) block. Clears all existing potential blocks.
	/// The root's aggregated weight is set to the block's own weight (no parent to aggregate with).
	/// </summary>
	public void SetFinalizedBlock(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));

		var root = new LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>(block, default!, false, block.Weight);
		_allNodes.Clear();
		_potentialHeads.Clear();
		_allNodes[block.ID] = root;
	}

	/// <summary>
	/// Adds a new block as a child of the block identified by <paramref name="previousBlockID"/>.
	/// The parent must exist in the graph (i.e., be the finalized block or a descendant of it).
	/// If the parent is currently a potential block, the new node replaces it.
	/// If the parent is not a potential block, the new node becomes a new potential block.
	/// The new block's aggregated weight is computed by aggregating the parent's aggregated weight
	/// with the new block's own weight.
	/// </summary>
	public LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID> Add(TBlock block, TBlockID previousBlockID) {
		Guard.ArgumentNotNull(block, nameof(block));

		if (!_allNodes.TryGetValue(previousBlockID, out var parent))
			throw new InvalidOperationException($"Cannot add block: previous block '{previousBlockID}' does not exist or is before the current finalized block.");

		var aggregatedWeight = _weightAggregator.Aggregate(parent.AggregatedWeight, block.Weight);
		var node = new LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>(block, previousBlockID, true, aggregatedWeight);
		_allNodes[block.ID] = node;
		_potentialHeads.Remove(previousBlockID);
		_potentialHeads[block.ID] = node;
		return node;
	}

	public bool RemoveHead(TBlockID headID) => _potentialHeads.Remove(headID);

	public IReadOnlyList<LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> GetPathToRoot(TBlockID headID) {
		if (!_allNodes.TryGetValue(headID, out var head))
			throw new InvalidOperationException($"Block '{headID}' not found in graph.");

		var path = new List<LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>>();
		var cursor = head;
		while (cursor != null) {
			path.Add(cursor);
			cursor = ResolveParent(cursor)!;
		}
		path.Reverse();
		return path;
	}

	public IReadOnlyList<LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> GetPathFromAncestor(TBlockID ancestorID, TBlockID descendantID) {
		if (!_allNodes.TryGetValue(ancestorID, out var ancestor))
			throw new InvalidOperationException($"Ancestor block '{ancestorID}' not found in graph.");
		if (!_allNodes.TryGetValue(descendantID, out var descendant))
			throw new InvalidOperationException($"Descendant block '{descendantID}' not found in graph.");

		var result = new List<LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>>();
		var cursor = descendant;
		while (!ReferenceEquals(cursor, ancestor)) {
			if (cursor == null)
				throw new InvalidOperationException("Ancestor is not in descendant lineage.");

			result.Add(cursor);
			cursor = ResolveParent(cursor)!;
		}
		result.Reverse();
		return result;
	}

	public LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>? FindCommonAncestor(TBlockID xID, TBlockID yID) {
		if (!_allNodes.TryGetValue(xID, out var x) || !_allNodes.TryGetValue(yID, out var y))
			return null;

		var lineage = new HashSet<TBlockID>(_blockIDComparer);
		for (var cursor = x; cursor != null; cursor = ResolveParent(cursor)!)
			lineage.Add(cursor.Block.ID);

		for (var cursor = y; cursor != null; cursor = ResolveParent(cursor)!)
			if (lineage.Contains(cursor.Block.ID))
				return cursor;

		return null;
	}

	public IReadOnlyList<LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>> GetAncestorPath(TBlockID headID) {
		if (!_allNodes.TryGetValue(headID, out var head))
			throw new InvalidOperationException($"Block '{headID}' not found in graph.");

		var path = new List<LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>>();
		for (var cursor = head; cursor != null; cursor = ResolveParent(cursor)!)
			path.Add(cursor);

		path.Reverse();
		return path;
	}

	public bool IsDescendantOf(TBlockID descendantID, TBlockID ancestorID) {
		if (!_allNodes.TryGetValue(descendantID, out var descendant))
			return false;

		for (var cursor = ResolveParent(descendant); cursor != null; cursor = ResolveParent(cursor))
			if (_blockIDComparer.Equals(cursor!.Block.ID, ancestorID))
				return true;

		return false;
	}

	public void Clear() {
		_potentialHeads.Clear();
		_allNodes.Clear();
	}

	public void PruneToPath(IEnumerable<TBlockID> retainedPathIDs) {
		Guard.ArgumentNotNull(retainedPathIDs, nameof(retainedPathIDs));

		var retainedIDs = retainedPathIDs.ToHashSet(_blockIDComparer);

		var allKeys = _allNodes.Keys.ToArray();
		foreach (var key in allKeys)
			if (!retainedIDs.Contains(key))
				_allNodes.Remove(key);

		var headKeys = _potentialHeads.Keys.ToArray();
		foreach (var key in headKeys)
			if (!retainedIDs.Contains(key))
				_potentialHeads.Remove(key);
	}

	private LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>? ResolveParent(LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID> node) {
		if (!node.HasParent)
			return null;
		return _allNodes.TryGetValue(node.ParentID, out var parent) ? parent : null;
	}
}
