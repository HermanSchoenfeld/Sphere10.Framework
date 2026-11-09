# Sphere10.Framework.Consensus.Tests

NUnit tests for [Sphere10.Framework.Consensus](../../src/Sphere10.Framework.Consensus/README.md). They cover the compact-target and ASERT difficulty algorithms, periodic statistics and the generic blockchain primitives, including the finalized chain and the unfinalized block graph. A small account-model chain in `ReferenceChain` supplies concrete blocks, operations and state.

## Run the tests

Run these commands from the repository root. `dotnet test` restores and builds the referenced projects first. `BuildRevision=0` supplies an explicit local build revision.

```powershell
dotnet test tests/Sphere10.Framework.Consensus.Tests/Sphere10.Framework.Consensus.Tests.csproj -c Release -p:BuildRevision=0
```

Run the blockchain fixtures only:

```powershell
dotnet test tests/Sphere10.Framework.Consensus.Tests/Sphere10.Framework.Consensus.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~Blockchain"
```

The suite needs no network, database or desktop session.

## Contents

### Project root

Difficulty-algorithm and statistics fixtures.

| Fixture | Purpose |
| --- | --- |
| [PeriodicStatisticsTests](PeriodicStatisticsTests.cs) | Rejection of non-positive periods and a missing store, `Start` initialization, and event registration only after starting. |
| [TargetAlgorithmTests](TargetAlgorithmTests.cs) | `MolinaTargetAlgorithm` compact-target round trips and ordering, the real-time flags of `ASERT_RTT` and `ASERT2`, the minimum target without enough timestamps, and target easing or hardening for slow, fast and stale blocks. |

### Blockchain

Fixtures for the generic blockchain, block graph and Merkle helpers, run against the reference chain.

| Fixture | Purpose |
| --- | --- |
| [BlockchainTests](Blockchain/BlockchainTests.cs) | `Blockchain<...>` block application and undo, height and state changes, applied and undone events, undo on an empty chain and construction from existing blocks. |
| [CryptoToolTests](Blockchain/CryptoToolTests.cs) | Deterministic `CryptoTool.DeriveSecureChecksum` and `DeriveChildDigest` results, and distinct results for different secrets, indexes and parents. |
| [FinalizedBlockchainTests](Blockchain/FinalizedBlockchainTests.cs) | `FinalizedBlockchain` path finalization without reapplying a synced root, reorganization plans and execution, syncing the unfinalized root to the head and decorator delegation. |
| [MerkleHelperTests](Blockchain/MerkleHelperTests.cs) | Deterministic block Merkle roots, a null root for no operations and a single digest returned unchanged. |
| [PrimitivesTests](Blockchain/PrimitivesTests.cs) | Operation apply and undo as inverses, in-order block application, reverse-order undo, apply-undo-apply state equality and null-state rejection. |
| [UnfinalizedBlockGraphTests](Blockchain/UnfinalizedBlockGraphTests.cs) | `UnfinalizedBlockGraph` root setting, potential-head replacement, sibling blocks, missing parents, head removal, paths to the root, common ancestors, pruning, descendant checks and aggregated weight. |

### ReferenceChain

Public support types for an in-memory account-model chain. They contain no tests.

| Fixture | Purpose |
| --- | --- |
| [AccountBlock](ReferenceChain/AccountBlock.cs) | Block of account operations with a sequential ID and a weight of one. |
| [AccountOperation](ReferenceChain/AccountOperation.cs) | Invertible balance adjustment for one account. |
| [AccountState](ReferenceChain/AccountState.cs) | In-memory ledger of account balances. |
| [LongWeightAggregator](ReferenceChain/LongWeightAggregator.cs) | Additive `IWeightAggregator<long>` used by the chain and the block graph. |
