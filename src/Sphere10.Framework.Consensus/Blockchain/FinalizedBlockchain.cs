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
/// A decorator over <see cref="Blockchain{TBlock, TState, TBlockID, TWeight, TOperationID}"/> that tracks
/// finalization. Maintains an <see cref="UnfinalizedBlockGraph{TBlock, TState, TBlockID, TWeight, TOperationID}"/>
/// whose root is always synced to the finalized head.
/// </summary>
public class FinalizedBlockchain<TBlock, TState, TBlockID, TWeight, TOperationID> : BlockchainDecorator<TBlock, TState, TBlockID, TWeight, TOperationID, Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID>>
	where TBlock : IBlockchainBlock<TState, TBlockID, TWeight, TOperationID>
	where TState : IBlockchainState {
	public FinalizedBlockchain(Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID> blockchain)
		: this(blockchain, new UnfinalizedBlockGraph<TBlock, TState, TBlockID, TWeight, TOperationID>(blockchain.WeightAggregator)) {
	}

	public FinalizedBlockchain(Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID> blockchain, UnfinalizedBlockGraph<TBlock, TState, TBlockID, TWeight, TOperationID> unfinalizedSector)
		: base(blockchain) {
		Guard.ArgumentNotNull(unfinalizedSector, nameof(unfinalizedSector));
		UnfinalizedSector = unfinalizedSector;
	}

	public UnfinalizedBlockGraph<TBlock, TState, TBlockID, TWeight, TOperationID> UnfinalizedSector { get; }

	/// <summary>
	/// Adds a block to the unfinalized sector as the new root. This is the genesis of the unfinalized frontier.
	/// </summary>
	public LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID> AddUnfinalizedRoot(TBlock block) {
		UnfinalizedSector.SetFinalizedBlock(block);
		return UnfinalizedSector.Nodes[block.ID];
	}

	/// <summary>
	/// Adds a block to the unfinalized sector as a child of the block identified by <paramref name="previousBlockID"/>.
	/// </summary>
	public LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID> AddUnfinalizedBlock(TBlock block, TBlockID previousBlockID)
		=> UnfinalizedSector.Add(block, previousBlockID);

	/// <summary>
	/// Finalizes a path from the unfinalized sector into the finalized chain.
	/// Applies each block in order, then syncs the unfinalized root to the new head.
	/// </summary>
	public IReadOnlyList<TBlock> FinalizePath(TBlockID winningHeadID) {
		Guard.ArgumentNotNull(winningHeadID, nameof(winningHeadID));

		var path = UnfinalizedSector.GetPathToRoot(winningHeadID);
		foreach (var node in path)
			ApplyBlock(node.Block);

		// Sync unfinalized sector root with the new finalized head
		var head = Head;
		UnfinalizedSector.SetFinalizedBlock(head.Block);
		return path.Select(x => x.Block).ToArray();
	}

	/// <summary>
	/// Creates a re-org plan: undo finalized blocks back to the common ancestor,
	/// then apply the winning branch from the unfinalized sector.
	/// </summary>
	public ReorgPlan<TBlock, TState, TBlockID, TWeight, TOperationID> CreateReorgPlan(TBlockID fromHeadID, TBlockID toHeadID) {
		Guard.ArgumentNotNull(fromHeadID, nameof(fromHeadID));
		Guard.ArgumentNotNull(toHeadID, nameof(toHeadID));

		var ancestor = UnfinalizedSector.FindCommonAncestor(fromHeadID, toHeadID);
		Guard.Ensure(ancestor != null, "Heads do not share a common ancestor.");

		var undoNodes = UnfinalizedSector.GetPathFromAncestor(ancestor!.Block.ID, fromHeadID).Reverse().ToArray();
		var applyNodes = UnfinalizedSector.GetPathFromAncestor(ancestor!.Block.ID, toHeadID).ToArray();
		return new ReorgPlan<TBlock, TState, TBlockID, TWeight, TOperationID>(ancestor!, undoNodes, applyNodes);
	}

	/// <summary>
	/// Executes a re-org by undoing finalized blocks back to the common ancestor,
	/// then applying the winning branch from the unfinalized sector.
	/// </summary>
	public void ExecuteReorg(ReorgPlan<TBlock, TState, TBlockID, TWeight, TOperationID> plan) {
		Guard.ArgumentNotNull(plan, nameof(plan));

		foreach (var _ in plan.UndoPath)
			UndoBlock();

		foreach (var node in plan.ApplyPath)
			ApplyBlock(node.Block);

		// Sync unfinalized sector root with the new finalized head
		var head = Head;
		UnfinalizedSector.SetFinalizedBlock(head.Block);
	}

	public sealed class ReorgPlan<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID>
		where TReorgBlock : IBlockchainBlock<TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID>
		where TReorgState : IBlockchainState {

		public ReorgPlan(LinkedBlock<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID> commonAncestor, IReadOnlyList<LinkedBlock<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID>> undoPath, IReadOnlyList<LinkedBlock<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID>> applyPath) {
			CommonAncestor = commonAncestor;
			UndoPath = undoPath ?? throw new ArgumentNullException(nameof(undoPath));
			ApplyPath = applyPath ?? throw new ArgumentNullException(nameof(applyPath));
		}

		public LinkedBlock<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID> CommonAncestor { get; }

		public IReadOnlyList<LinkedBlock<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID>> UndoPath { get; }

		public IReadOnlyList<LinkedBlock<TReorgBlock, TReorgState, TReorgBlockID, TReorgWeight, TReorgOperationID>> ApplyPath { get; }
	}
}
