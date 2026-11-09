# Blockchain

Generic contracts and implementations for reversible ledger operations, blocks, a linear chain, a graph of competing branches, finalization and reorganization.

A ledger supplies an `IBlockchainState` and reversible `IBlockchainOperation<TState, TOperationID>` types, usually derived from `BlockchainStateBase` and `BlockchainOperationBase<TState, TOperationID>`. `BlockchainBlock<TState, TBlockID, TWeight, TOperationID>` applies its operations in order and undoes them in reverse, and derived blocks override `ID` and `Weight`. `Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID>` is the in-memory linear chain. `ApplyBlock` and `UndoBlock` run inside the state's update scope, keep `LinkedBlock` entries with aggregated weights from an `IWeightAggregator<TWeight>`, and raise `BlockApplied` and `BlockUndone`.

`UnfinalizedBlockGraph<...>` indexes candidate blocks above a finalized root by ID and answers path, ancestry and common-ancestor queries without applying any block. `FinalizedBlockchain<...>` decorates a concrete `Blockchain<...>` with such a graph. `FinalizePath` applies a chosen branch and re-roots the graph, and `CreateReorgPlan` and `ExecuteReorg` undo back to a common ancestor and apply another branch. The plan type is the nested `FinalizedBlockchain<...>.ReorgPlan<...>`. The library does not choose a winning branch, validate blocks or make a multi-block change atomic. `BlockchainDecorator<...>` is the base for other chain decorators. `MerkleHelper` computes Merkle roots over operation digests. The [project README](../README.md) has complete examples, and the proof-of-work types live in the project folder itself.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID>`](Blockchain.cs) | class | In-memory linear chain that applies and undoes blocks in update scopes and tracks aggregated weight |
| [`BlockchainBase<TBlock, TState, TBlockID, TWeight, TOperationID>`](BlockchainBase.cs) | abstract class | Base chain with the `BlockApplied` and `BlockUndone` events and abstract apply, undo and state members |
| [`BlockchainBlock<TState, TBlockID, TWeight, TOperationID>`](BlockchainBlock.cs) | class | Block over an ordered operation list that applies forward and undoes in reverse, with overridable `ID` and `Weight` |
| [`BlockchainDecorator<TBlock, TState, TBlockID, TWeight, TOperationID>`](BlockchainDecorator.cs) | abstract class | Chain decorator whose inner chain is typed as `IBlockchain<...>` |
| [`BlockchainDecorator<TBlock, TState, TBlockID, TWeight, TOperationID, TConcrete>`](BlockchainDecorator.cs) | abstract class | Chain decorator that forwards every member and event to an inner chain of type `TConcrete` |
| [`BlockchainOperationBase<TState, TOperationID>`](BlockchainOperationBase.cs) | abstract class | Operation base that rejects a null state and calls `ApplyInternal` or `UndoInternal` |
| [`BlockchainStateBase`](BlockchainStateBase.cs) | abstract class | State base whose virtual `EnterUpdateScope` returns a scope that does nothing |
| [`FinalizedBlockchain<TBlock, TState, TBlockID, TWeight, TOperationID>`](FinalizedBlockchain.cs) | class | Decorator over `Blockchain<...>` with an unfinalized graph, path finalization and reorganization plans |
| [`IBlockchain<TBlock, TState, TBlockID, TWeight, TOperationID>`](IBlockchain.cs) | interface | Linear chain contract with state, weight aggregator, height, blocks, head, aggregated weight, apply, undo and events |
| [`IBlockchainBlock<TState, TBlockID, TWeight, TOperationID>`](IBlockchainBlock.cs) | interface | Block contract with ID, weight, operations, `Apply` and `Undo` |
| [`IBlockchainOperation<TState, TOperationID>`](IBlockchainOperation.cs) | interface | Reversible operation contract with ID, `Apply` and `Undo` |
| [`IBlockchainState`](IBlockchainState.cs) | interface | Ledger state contract that opens an update scope around each block change |
| [`IWeightAggregator<TWeight>`](IWeightAggregator.cs) | interface | Combines a parent's aggregated weight with a block's own weight |
| [`LinkedBlock<TBlock, TState, TBlockID, TWeight, TOperationID>`](LinkedBlock.cs) | class | Immutable block entry with its parent ID, parent flag and aggregated weight |
| [`MerkleHelper`](MerkleHelper.cs) | static class | Computes Merkle roots of operation digests, or of a block's operation hash codes, with a chosen hash function |
| [`UnfinalizedBlockGraph<TBlock, TState, TBlockID, TWeight, TOperationID>`](UnfinalizedBlockGraph.cs) | class | Graph of candidate blocks above a finalized root with potential heads, paths, ancestry, pruning and clearing |
