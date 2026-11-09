// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
using Sphere10.Framework.Consensus;
using Sphere10.Framework.Consensus.Tests.ReferenceChain;
using System.Linq;

namespace Sphere10.Framework.Consensus.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FinalizedBlockchainTests {

	[Test]
	public void FinalizePath_AdvancesFinalizedSector() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		var block1 = new AccountBlock(new AccountOperation("alice", 100));
		var block2 = new AccountBlock(new AccountOperation("alice", 50));

		var head1 = manager.AddUnfinalizedRoot(block1);
		var head2 = manager.AddUnfinalizedBlock(block2, head1.Block.ID);

		var finalizedBlocks = manager.FinalizePath(head2.Block.ID);

		Assert.That(chain.Height, Is.EqualTo(2L));
		Assert.That(state.GetBalance("alice"), Is.EqualTo(150L));
		Assert.That(finalizedBlocks, Has.Length.EqualTo(2));
	}

	[Test]
	public void FinalizePath_DoesNotReapplySyncedRoot() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		var root = manager.AddUnfinalizedRoot(new AccountBlock(new AccountOperation("alice", 100)));
		var head1 = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("alice", 50)), root.Block.ID);
		manager.FinalizePath(head1.Block.ID);

		// The root is now the finalized head, so only the new block is applied
		var head2 = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("alice", 25)), chain.Head.Block.ID);
		var finalizedBlocks = manager.FinalizePath(head2.Block.ID);

		Assert.That(chain.Height, Is.EqualTo(3L));
		Assert.That(state.GetBalance("alice"), Is.EqualTo(175L));
		Assert.That(finalizedBlocks, Has.Count.EqualTo(1));
		Assert.That(finalizedBlocks[0], Is.SameAs(head2.Block));
	}

	[Test]
	public void FinalizePath_DoesNotReapplyRootSetToAppliedHead() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		manager.ApplyBlock(new AccountBlock(new AccountOperation("alice", 100)));
		var root = manager.AddUnfinalizedRoot(chain.Head.Block);
		var head = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("alice", 50)), root.Block.ID);
		manager.FinalizePath(head.Block.ID);

		Assert.That(chain.Height, Is.EqualTo(2L));
		Assert.That(state.GetBalance("alice"), Is.EqualTo(150L));
	}

	[Test]
	public void CreateReorgPlan_IdentifiesCorrectPaths() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		var root = manager.AddUnfinalizedRoot(new AccountBlock(new AccountOperation("alice", 100)));
		var branchA = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("bob", 50)), root.Block.ID);
		var branchB = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("carol", 25)), root.Block.ID);

		var plan = manager.CreateReorgPlan(branchA.Block.ID, branchB.Block.ID);

		Assert.That(plan.CommonAncestor, Is.SameAs(root));
		Assert.That(plan.UndoPath, Has.Count.EqualTo(1));
		Assert.That(plan.UndoPath[0], Is.SameAs(branchA));
		Assert.That(plan.ApplyPath, Has.Count.EqualTo(1));
		Assert.That(plan.ApplyPath[0], Is.SameAs(branchB));
	}

	[Test]
	public void ExecuteReorg_SwitchesBranch() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		// Build two competing branches from a common root
		var root = manager.AddUnfinalizedRoot(new AccountBlock(new AccountOperation("alice", 100)));
		var branchA = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("bob", 50)), root.Block.ID);
		var branchB = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("carol", 25)), root.Block.ID);

		// Re-org from branch A to branch B (both are in the graph, no finalization yet)
		var plan = manager.CreateReorgPlan(branchA.Block.ID, branchB.Block.ID);

		Assert.That(plan.CommonAncestor, Is.SameAs(root));
		Assert.That(plan.UndoPath, Has.Count.EqualTo(1));
		Assert.That(plan.UndoPath[0], Is.SameAs(branchA));
		Assert.That(plan.ApplyPath, Has.Count.EqualTo(1));
		Assert.That(plan.ApplyPath[0], Is.SameAs(branchB));
	}

	[Test]
	public void FinalizePath_SyncsUnfinalizedRootToHead() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		var block1 = new AccountBlock(new AccountOperation("alice", 100));
		var block2 = new AccountBlock(new AccountOperation("alice", 50));

		var head1 = manager.AddUnfinalizedRoot(block1);
		var head2 = manager.AddUnfinalizedBlock(block2, head1.Block.ID);

		manager.FinalizePath(head2.Block.ID);

		// After finalization, the unfinalized sector root should be the new head
		var unfinalizedRoot = manager.UnfinalizedSector.Nodes[chain.Head.Block.ID];
		Assert.That(unfinalizedRoot, Is.Not.Null);
		Assert.That(unfinalizedRoot.IsRoot, Is.True);
		Assert.That(unfinalizedRoot.Block.ID, Is.EqualTo(chain.Head.Block.ID));
	}

	[Test]
	public void ExecuteReorg_SyncsUnfinalizedRootToHead() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		// Build two competing branches
		var root = manager.AddUnfinalizedRoot(new AccountBlock(new AccountOperation("alice", 100)));
		var branchA = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("bob", 50)), root.Block.ID);
		var branchB = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("carol", 25)), root.Block.ID);

		// Finalize branch A
		manager.FinalizePath(branchA.Block.ID);
		Assert.That(chain.Height, Is.EqualTo(2L));
		Assert.That(state.GetBalance("bob"), Is.EqualTo(50L));

		// Rebuild branch B from the new root
		var newRoot = manager.UnfinalizedSector.Nodes[chain.Head.Block.ID];
		var newBranchB = manager.AddUnfinalizedBlock(new AccountBlock(new AccountOperation("carol", 25)), newRoot.Block.ID);

		// Re-org to branch B
		var plan = manager.CreateReorgPlan(branchA.Block.ID, newBranchB.Block.ID);
		manager.ExecuteReorg(plan);

		// After re-org, the unfinalized sector root should be the new head
		var unfinalizedRoot = manager.UnfinalizedSector.Nodes[chain.Head.Block.ID];
		Assert.That(unfinalizedRoot, Is.Not.Null);
		Assert.That(unfinalizedRoot.IsRoot, Is.True);
		Assert.That(unfinalizedRoot.Block.ID, Is.EqualTo(chain.Head.Block.ID));
	}

	[Test]
	public void Decorator_DelegatesToInternalBlockchain() {
		var state = new AccountState();
		var chain = new Blockchain<AccountBlock, AccountState, int, long, int>(state, new LongWeightAggregator());
		var manager = new FinalizedBlockchain<AccountBlock, AccountState, int, long, int>(chain);

		var block = new AccountBlock(new AccountOperation("alice", 100));
		manager.ApplyBlock(block);

		Assert.That(manager.Height, Is.EqualTo(1L));
		Assert.That(manager.Head.Block, Is.SameAs(block));
		Assert.That(manager.State, Is.SameAs(state));
		Assert.That(manager.WeightAggregator, Is.SameAs(chain.WeightAggregator));
	}
}
