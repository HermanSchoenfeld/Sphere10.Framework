# ⛓️ Sphere10.Framework.Consensus

<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**Blockchain state transitions, chain management, and proof-of-work primitives** for building a ledger with your own block format, state model, and consensus rules.

The project combines reversible operations, an in-memory linear chain, a graph of competing branches, finalization/reorganization helpers, compact target encoding, and ASERT-style difficulty adjustment. These components can be used independently: a target calculator does not need a blockchain, and a blockchain does not have to use proof of work.

Your application supplies block validation, identity rules, fork selection, persistence, and networking. The examples below demonstrate the current APIs and describe the integration boundaries of the implementation.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Consensus
```

The package targets **.NET 10** and references [Sphere10.Framework](../Sphere10.Framework/README.md). Public types in this project use the `Sphere10.Framework.Consensus` namespace, including types in the `Blockchain/` directory.

## 🏗️ Core Architecture

| Area | Main types | Responsibility |
|---|---|---|
| Ledger state | `IBlockchainState`, `BlockchainStateBase` | Supply the state and its update scope |
| Reversible operations | `IBlockchainOperation<TState, TOperationID>`, `BlockchainOperationBase<TState, TOperationID>` | Apply a mutation and reverse it |
| Blocks | `IBlockchainBlock<TState, TBlockID, TWeight, TOperationID>`, `BlockchainBlock<...>` | Group ordered operations with an ID and weight |
| Linear chain | `IBlockchain<...>`, `BlockchainBase<...>`, `Blockchain<...>`, `LinkedBlock<...>` | Apply/undo blocks, track parents and aggregate weights |
| Branches and finalization | `UnfinalizedBlockGraph<...>`, `FinalizedBlockchain<...>` | Index candidate branches, find paths, and execute caller-selected paths |
| Proof of work | `ICompactTargetAlgorithm`, `MolinaTargetAlgorithm`, `IDAAlgorithm`, `ASERT_RTT`, `ASERT2` | Encode targets and calculate difficulty adjustments |
| Supporting helpers | `MerkleHelper`, `CryptoTool`, `PeriodicStatistics` | Compute roots/digests and provide basic statistics lifecycle support |

The chain and graph types use the same five generic parameters:

```text
Blockchain<TBlock, TState, TBlockID, TWeight, TOperationID>
UnfinalizedBlockGraph<TBlock, TState, TBlockID, TWeight, TOperationID>
FinalizedBlockchain<TBlock, TState, TBlockID, TWeight, TOperationID>
```

| Parameter | Meaning | Example below |
|---|---|---|
| `TBlock` | Implements `IBlockchainBlock<TState, TBlockID, TWeight, TOperationID>` | `CounterBlock` |
| `TState` | Implements `IBlockchainState` | `CounterState` |
| `TBlockID` | Block identity and graph lookup key | `int` |
| `TWeight` | Per-block and aggregated weight representation | `long` |
| `TOperationID` | Operation identity | `int` |

## ⚡ Quick Start: Calculate a Target

This example calculates a target from two block timestamps and compares a candidate digest with the expanded numeric threshold:

```csharp
using System;
using System.Numerics;
using Sphere10.Framework;
using Sphere10.Framework.Consensus;

var targetAlgorithm = new MolinaTargetAlgorithm();
var configuration = new ASERTConfiguration {
	BlockTime = TimeSpan.FromSeconds(60),
	RelaxationTime = TimeSpan.FromHours(1)
};
var difficulty = new ASERT2(targetAlgorithm, configuration);
var headTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
var timestamps = new[] { headTime, headTime.AddSeconds(-60) };
var previousCompactTarget = 0x10000000u;

var nextCompactTarget = difficulty.CalculateNextBlockTarget(timestamps, previousCompactTarget, 100);
var threshold = targetAlgorithm.ToTarget(nextCompactTarget);
var digest = Hashers.Hash(CHF.SHA2_256, "candidate block header"u8);
var hashValue = new BigInteger(digest, isUnsigned: true, isBigEndian: true);

Console.WriteLine(nextCompactTarget == previousCompactTarget); // True: exactly the desired interval.
Console.WriteLine(hashValue <= threshold); // Whether this candidate satisfies this numeric target.
```

A protocol must specify the actual header bytes, hash function, byte order, timestamp rules, and comparison rule. Calling `ApplyBlock` does not perform this proof-of-work check automatically.

## 🎯 Compact Targets

[MolinaTargetAlgorithm](MolinaTargetAlgorithm.cs) implements Albert Molina's PascalCoin-style target representation. It encodes the leading-zero count in the high eight bits and an inverted mantissa in the remaining 24 bits.

**Higher compact values mean harder difficulty; lower full numeric thresholds mean harder difficulty.** This compact format differs from Bitcoin's `nBits` encoding.

| Member | Behavior |
|---|---|
| `MinCompactTarget` | `134217728` / `0x08000000`, the easiest advertised target |
| `MaxCompactTarget` | `3892314111` / `0xE7FFFFFF`, the hardest advertised target |
| `FromTarget(BigInteger)` | Compress a numeric target, losing lower-bit precision |
| `ToTarget(uint)` | Expand a compact target to a `BigInteger` |
| `FromDigest(ReadOnlySpan<byte>)` | Interpret up to 32 bytes as an unsigned big-endian value; shorter inputs are left-padded with zeros |
| `ToDigest(uint, Span<byte>)` | Write an expanded target into an exactly 32-byte destination |
| `ToDigest(BigInteger, Span<byte>)` | Write an unsigned numeric target into an exactly 32-byte destination |
| `ToDigest(...)` extensions | Allocate and return the 32-byte destination |
| `AggregateWork(uint, uint)` | Decode both compact values, add their full numeric targets, and compact the result |

```csharp
using System;
using System.Numerics;
using Sphere10.Framework.Consensus;

var targetAlgorithm = new MolinaTargetAlgorithm();
var fullTarget = (BigInteger.One << 232) - BigInteger.One;
var compactTarget = targetAlgorithm.FromTarget(fullTarget);
var expandedTarget = targetAlgorithm.ToTarget(compactTarget);

var targetBytes = targetAlgorithm.ToDigest(compactTarget);
var compactFromBytes = targetAlgorithm.FromDigest(targetBytes);

Console.WriteLine(targetBytes.Length); // 32
Console.WriteLine(compactFromBytes == compactTarget); // True
Console.WriteLine(expandedTarget <= fullTarget); // True: compaction discards precision.
```

Use the **expanded target** for exact digest comparisons. Comparing compacted digests alone discards information near the threshold. Supply valid nonnegative targets within 256 bits; these conversion methods do not constitute protocol validation.

`AggregateWork` specifically computes `FromTarget(ToTarget(a) + ToTarget(b))`. It does not calculate inverse-target proof-of-work totals. Define the meaning of `TWeight` and `IWeightAggregator<TWeight>` for your chain rather than treating compact target values as directly additive chain work.

## ⏱️ Difficulty Adjustment

[IDAAlgorithm](IDAAlgorithm.cs) exposes `CalculateNextBlockTarget(previousBlockTimestamps, previousCompactTarget, blockNumber)`. Timestamp sequences are **newest first**.

| Implementation | Observed interval | Insufficient history |
|---|---|---|
| `ASERT_RTT` | `DateTime.UtcNow - timestamps[0]` | No timestamps → `MinCompactTarget` |
| `ASERT2` | `timestamps[0] - timestamps[1]` | Fewer than two timestamps → `MinCompactTarget` |
| `ASERT_RTT.CalculateNextBlockTarget(uint, int, int, int)` | Explicit interval, block time, and relaxation time in seconds | Caller supplies all inputs |

The implemented adjustment is:

```text
nextFullTarget = previousFullTarget × exp((observedSeconds - blockTimeSeconds) / relaxationSeconds)
nextCompactTarget = targetAlgorithm.FromTarget(nextFullTarget)
```

A shorter interval decreases the full target and increases difficulty; a longer interval increases the full target and decreases difficulty. `RelaxationTime` is the exponential time constant in this formula, **not a base-2 half-life**. The implementation uses `FixedPoint.Exp` and truncates the multiplier to six decimal places before scaling the integer target.

```csharp
using System;
using Sphere10.Framework.Consensus;

var targetAlgorithm = new MolinaTargetAlgorithm();
var configuration = new ASERTConfiguration {
	BlockTime = TimeSpan.FromSeconds(60),
	RelaxationTime = TimeSpan.FromHours(1)
};
var calculator = new ASERT_RTT(targetAlgorithm, configuration);

// Explicit elapsed time avoids a dependency on the wall clock.
var previousCompactTarget = 0x10000000u;
var slowerBlockTarget = calculator.CalculateNextBlockTarget(
	previousCompactTarget,
	timestampDelta: 120,
	blockTimeSec: 60,
	relaxationTime: 3600
);

Console.WriteLine(slowerBlockTarget < previousCompactTarget); // True: an easier target.
```

For calculations during mining, the timestamp overload on `ASERT_RTT` uses the current UTC time. `ASERT2` uses only the two supplied timestamps. Both currently report `RealTime == true` because `ASERT2` inherits the base property; choose the implementation explicitly instead of using that flag to distinguish them.

Set both configuration values: their defaults are zero. Use a positive relaxation time of at least one whole second and protocol-appropriate timestamp bounds. The timestamp overloads truncate `TimeSpan.TotalSeconds` to `int`, and the current implementations do not use `blockNumber` or validate a network's timestamp policy.

## 🧱 Define a Ledger

The following complete model uses a single counter so the apply/undo behavior is visible. Put these types in a file named `CounterModel.cs`. Subsequent chain examples use this model.

```csharp
using Sphere10.Framework.Consensus;

namespace CounterLedger;

public sealed class CounterState : BlockchainStateBase {
	public long Value { get; set; }
}

public sealed class CounterOperation : BlockchainOperationBase<CounterState, int> {
	public CounterOperation(int id, long delta) {
		ID = id;
		Delta = delta;
	}

	public override int ID { get; }

	public long Delta { get; }

	protected override void ApplyInternal(CounterState state)
		=> state.Value = checked(state.Value + Delta);

	protected override void UndoInternal(CounterState state)
		=> state.Value = checked(state.Value - Delta);
}

public sealed class CounterBlock : BlockchainBlock<CounterState, int, long, int> {
	public CounterBlock(int id, params IBlockchainOperation<CounterState, int>[] operations)
		: base(operations) {
		ID = id;
	}

	public override int ID { get; }

	public override long Weight => 1L;
}

public sealed class AdditiveWeight : IWeightAggregator<long> {
	public long Aggregate(long parentAggregatedWeight, long blockWeight)
		=> checked(parentAggregatedWeight + blockWeight);
}
```

`BlockchainBlock` applies operations in their listed order and undoes them in reverse order. Each operation must restore the original state when undone in the appropriate sequence. The base block returns `default` for both `ID` and `Weight`, so override them for your protocol as shown above.

The integer IDs and unit weights here are demonstration choices. A real ledger should define stable block/operation identities, validate operations before application, and specify its own weight calculation. Keep block identities, weights, and operation lists unchanged after insertion.

### State Update Scopes

`IBlockchainState.EnterUpdateScope()` returns `IDisposable`. The linear chain opens this scope around each apply or undo operation. `BlockchainStateBase` supplies a **no-op scope**.

The interface has no `Commit` method, and `BlockchainBlock.Apply` does not automatically undo earlier operations when a later operation throws. Persistence, synchronization, and recovery from partially applied blocks therefore need an application-defined design; deriving from `BlockchainStateBase` alone does not provide transactions. Direct `block.Apply(state)` calls also do not open a state update scope.

## 🔗 Apply and Undo Blocks

```csharp
using System;
using CounterLedger;
using Sphere10.Framework.Consensus;

var state = new CounterState();
var chain = new Blockchain<CounterBlock, CounterState, int, long, int>(state, new AdditiveWeight());

chain.ApplyBlock(new CounterBlock(1, new CounterOperation(101, 10)));
chain.ApplyBlock(new CounterBlock(2, new CounterOperation(102, -3)));

Console.WriteLine(state.Value); // 7
Console.WriteLine(chain.Height); // 2: number of applied blocks
Console.WriteLine(chain.AggregatedWeight); // 2: this example assigns weight 1 to each block

var undone = chain.UndoBlock();

Console.WriteLine(undone.Block.ID); // 2
Console.WriteLine(state.Value); // 10
Console.WriteLine(chain.Head.Block.ID); // 1
```

| Member | Behavior |
|---|---|
| `State` | The state instance supplied to the constructor |
| `Blocks` | Ordered, read-only view of the in-memory linked-block list |
| `Head` | Most recently applied linked block; `null` for an empty chain |
| `Height` | Applied block count, starting at zero |
| `AggregatedWeight` | Head's aggregated weight; `default(TWeight)` for an empty chain |
| `ApplyBlock(block)` | Aggregate weight, apply operations in an update scope, append the linked block |
| `UndoBlock()` | Undo the head in an update scope, remove and return it; rejects an empty chain |
| `BlockApplied` / `BlockUndone` | Single-argument events raised after the corresponding mutation and scope disposal |

The first block's aggregated weight is its own weight. Later blocks use `WeightAggregator.Aggregate(parentAggregatedWeight, block.Weight)`. The constructor overload accepting an initial block sequence applies those blocks to the supplied state immediately.

A `LinkedBlock` contains `Block`, `ParentID`, `HasParent`, `IsRoot`, and `AggregatedWeight`. The linear chain derives parent links from insertion order; it does not independently check a serialized header's parent hash, reject duplicate IDs, or select a winning branch.

## 🌿 Track Competing Branches

`UnfinalizedBlockGraph` stores nodes and candidate heads in dictionaries keyed by block ID. Adding a candidate records relationships and weights without applying its operations to a ledger state.

```csharp
using System;
using System.Linq;
using CounterLedger;
using Sphere10.Framework.Consensus;

var graph = new UnfinalizedBlockGraph<CounterBlock, CounterState, int, long, int>(new AdditiveWeight());
var root = new CounterBlock(1);
var branchA = new CounterBlock(2, new CounterOperation(201, 5));
var branchB = new CounterBlock(3, new CounterOperation(301, 9));
var branchAChild = new CounterBlock(4, new CounterOperation(401, 2));

graph.SetFinalizedBlock(root);
graph.Add(branchA, root.ID);
graph.Add(branchB, root.ID);
graph.Add(branchAChild, branchA.ID);

// With unit weights, this application's selection rule prefers the longer branch.
var winner = graph.PotentialHeads.Values.OrderByDescending(node => node.AggregatedWeight).First();
var path = graph.GetPathToRoot(winner.Block.ID);

Console.WriteLine(winner.Block.ID); // 4
Console.WriteLine(path.Count); // 3: root -> branch A -> branch A child
Console.WriteLine(graph.FindCommonAncestor(branchAChild.ID, branchB.ID).Block.ID); // 1
```

A real fork-choice rule also needs a defined tie-breaker. The graph itself does not choose the winning head.

| Operation | Current behavior |
|---|---|
| `SetFinalizedBlock(block)` | Clear all nodes/heads and insert a root whose aggregated weight is its own weight |
| `Add(block, previousBlockID)` | Require an existing parent; add the child and replace the parent in `PotentialHeads` if present |
| `GetPathToRoot(headID)` / `GetAncestorPath(headID)` | Return root-to-head order, **including the root** |
| `GetPathFromAncestor(ancestorID, descendantID)` | Return ancestor-to-descendant order, **excluding the ancestor** |
| `FindCommonAncestor(xID, yID)` | Return a shared linked ancestor, or `null` when none is found |
| `IsDescendantOf(descendantID, ancestorID)` | Test strict ancestry; a node is not its own descendant |
| `RemoveHead(headID)` | Remove only the candidate-head entry; retain its node |
| `PruneToPath(retainedIDs)` | Remove unlisted nodes and heads; the caller must retain coherent ancestry |
| `Clear()` | Remove all nodes and heads |

Constructors accept block-ID and operation-ID equality comparers. Use an appropriate block-ID comparer for your identity representation. The operation-ID comparer is currently stored but not used for deduplication.

Require unique block IDs before calling `Add`: it assigns into the node dictionary and does not reject replacement of an existing ID. Root weight is local to the graph; re-rooting does not import the full accumulated weight of the finalized chain.

## ✅ Finalize a Candidate Path

`FinalizedBlockchain` decorates the concrete linear `Blockchain` and exposes `UnfinalizedSector`. The following example starts with an **empty linear chain and an unapplied graph root**, because `FinalizePath` applies the entire root-to-head path.

```csharp
using System;
using CounterLedger;
using Sphere10.Framework.Consensus;

var state = new CounterState();
var linearChain = new Blockchain<CounterBlock, CounterState, int, long, int>(state, new AdditiveWeight());
var chain = new FinalizedBlockchain<CounterBlock, CounterState, int, long, int>(linearChain);
var root = new CounterBlock(10, new CounterOperation(1001, 10));
var candidate = new CounterBlock(11, new CounterOperation(1101, 4));

chain.AddUnfinalizedRoot(root);
chain.AddUnfinalizedBlock(candidate, root.ID);

Console.WriteLine(state.Value); // 0: adding candidates has not applied them

var applied = chain.FinalizePath(candidate.ID);

Console.WriteLine(applied.Count); // 2: root and candidate
Console.WriteLine(state.Value); // 14
Console.WriteLine(chain.Height); // 2
Console.WriteLine(chain.UnfinalizedSector.Nodes.Count); // 1: the new root
Console.WriteLine(chain.UnfinalizedSector.PotentialHeads.Count); // 0
```

**Current finalization boundaries:** the constructor and direct `ApplyBlock`/`UndoBlock` calls do not synchronize the graph with the linear head. `AddUnfinalizedRoot` resets the graph and does not apply the block. `FinalizePath` includes the root, so passing a graph rooted at an already-applied block would apply that block again. After finalization, re-rooting clears all other candidates.

These details matter when integrating repeated finalization into a node. Do not assume that a root synchronized to the current linear head will automatically be skipped by a later `FinalizePath` call.

## 🔄 Plan and Execute a Reorganization

`CreateReorgPlan(fromHeadID, toHeadID)` finds a common ancestor in the candidate graph. Its result contains:

- `CommonAncestor`: shared linked block.
- `UndoPath`: old branch from head back toward the ancestor, excluding the ancestor.
- `ApplyPath`: new branch from just after the ancestor to the new head.

This example retains both branches in the graph while applying the old branch to the linear chain:

```csharp
using System;
using CounterLedger;
using Sphere10.Framework.Consensus;

var state = new CounterState();
var linearChain = new Blockchain<CounterBlock, CounterState, int, long, int>(state, new AdditiveWeight());
var graph = new UnfinalizedBlockGraph<CounterBlock, CounterState, int, long, int>(new AdditiveWeight());
var root = new CounterBlock(20, new CounterOperation(2001, 10));
var oldHead = new CounterBlock(21, new CounterOperation(2101, 5));
var newHead = new CounterBlock(22, new CounterOperation(2201, -2));

graph.SetFinalizedBlock(root);
graph.Add(oldHead, root.ID);
graph.Add(newHead, root.ID);
linearChain.ApplyBlock(root);
linearChain.ApplyBlock(oldHead);

var chain = new FinalizedBlockchain<CounterBlock, CounterState, int, long, int>(linearChain, graph);
var plan = chain.CreateReorgPlan(oldHead.ID, newHead.ID);

Console.WriteLine(state.Value); // 15
Console.WriteLine(plan.UndoPath.Count); // 1
Console.WriteLine(plan.ApplyPath.Count); // 1

chain.ExecuteReorg(plan);

Console.WriteLine(state.Value); // 8: undo +5, then apply -2
Console.WriteLine(chain.Head.Block.ID); // 22
```

`ExecuteReorg` performs one `UndoBlock` per undo-path entry, then applies the new branch and re-roots the graph. It does not verify that the current linear head matches the plan's starting head. Build and execute plans against the matching chain state, serialize competing mutations, and provide recovery if a multi-block operation fails. The implementation does not wrap the whole reorganization in one atomic transaction.

## 🌳 Merkle Roots

For a portable commitment, define canonical operation bytes in your protocol, hash them, and pass the resulting digests to `ComputeOperationsMerkleRoot`:

```csharp
using System;
using Sphere10.Framework;
using Sphere10.Framework.Consensus;

var operationDigests = new[] {
	Hashers.Hash(CHF.SHA2_256, "operation-one"u8),
	Hashers.Hash(CHF.SHA2_256, "operation-two"u8)
};
var root = MerkleHelper.ComputeOperationsMerkleRoot(operationDigests);

Console.WriteLine(root.Length); // 32 with SHA2-256
```

The literal strings above are example bytes; an actual protocol must define complete operation serialization. SHA2-256 is the default, and overloads accept another `CHF`. Empty input returns `null`; a single digest returns that digest as the root.

`ComputeBlockMerkleRoot(block)` currently passes each operation's `GetHashCode()` to the underlying Merkle helper. It does **not** hash canonical operation serialization or `operation.ID`. Use the pre-hashed digest overload for protocol commitments rather than relying on object hash codes.

## 🔐 Digest Helpers

[CryptoTool](CryptoTool.cs) provides two standalone helpers:

| Method | Behavior |
|---|---|
| `DeriveChildDigest(byte[] digest, ulong index)` | Double SHA2-256 of the eight-byte little-endian index followed by the parent bytes |
| `DeriveSecureChecksum(byte[] secret)` | Double SHA2-256 of `secret || secret`, followed by a four-byte little-endian read at offset 27 of the second hash |

```csharp
using System;
using Sphere10.Framework;
using Sphere10.Framework.Consensus;

var parentDigest = Hashers.Hash(CHF.SHA2_256, "example parent material"u8);
var firstChild = CryptoTool.DeriveChildDigest(parentDigest, 0UL);
var secondChild = CryptoTool.DeriveChildDigest(parentDigest, 1UL);
var checksum = CryptoTool.DeriveSecureChecksum(parentDigest);

Console.WriteLine(firstChild.Length); // 32
Console.WriteLine(secondChild.Length); // 32
Console.WriteLine(checksum); // A 32-bit lookup checksum
```

The checksum's method name does not make its 32-bit output an authentication tag or a unique identity. Preserve the current byte selection when interoperating with existing values. Child digest derivation defines a particular byte-level scheme; it does not implement a complete wallet/key-management protocol.

## 📊 Periodic Statistics

`PeriodicStatistics` lives in [HashStats.cs](HashStats.cs). Its currently implemented lifecycle is:

```csharp
using System;
using Sphere10.Framework.Consensus;

var statistics = new PeriodicStatistics(TimeSpan.FromMinutes(1), historyLength: 60);
statistics.Start();
statistics.RegisterEvent(5.0);

Console.WriteLine(statistics.StartedOn.Kind); // Utc
```

The constructor requires a positive period and a non-null backing store when supplied. `Start` may be called once; registering events or reading `StartedOn`/`PeriodsAvailable` before starting throws. `PeriodsAvailable` is derived from elapsed UTC time.

**History aggregation is incomplete:** `historyLength` and the `occurances` argument are not used, period rollover is unfinished, and no public statistics/history accessor is exposed. This class should not be described as a complete rolling hash-rate monitor.

## 🧩 Extending the Project

| Requirement | Extension point |
|---|---|
| A different ledger model | Implement `IBlockchainState` and reversible `IBlockchainOperation<TState, TOperationID>` types |
| Custom block IDs and weights | Implement `IBlockchainBlock<...>` or override `BlockchainBlock<...>.ID` and `Weight` |
| Different cumulative scoring | Implement `IWeightAggregator<TWeight>` |
| A different chain store | Derive from `BlockchainBase<...>` or implement `IBlockchain<...>` |
| Additional chain behavior | Derive from `BlockchainDecorator<...>`; the six-parameter form exposes a typed inner chain |
| Another target representation | Implement `ICompactTargetAlgorithm` |
| Another difficulty rule | Implement `IDAAlgorithm` |

`FinalizedBlockchain` currently wraps the concrete `Blockchain<...>`, rather than an arbitrary `IBlockchain<...>` implementation. Account for that constructor constraint when introducing a different chain store.

The [reference account ledger](../../tests/Sphere10.Framework.Consensus.Tests/ReferenceChain/) demonstrates an account-model state, balance-adjustment operations, blocks, and additive weights. It is test/sample code, not a type exported by the NuGet package.

## 🧪 Build and Test

Run these commands from the repository root:

```powershell
dotnet build ./src/Sphere10.Framework.Consensus/Sphere10.Framework.Consensus.csproj --configuration Release
dotnet test ./tests/Sphere10.Framework.Consensus.Tests/Sphere10.Framework.Consensus.Tests.csproj --configuration Release
```

The [test project](../../tests/Sphere10.Framework.Consensus.Tests/) covers operation/block apply and undo, linear chain behavior, graph ancestry and heads, finalization/reorganization helpers, Merkle/digest helpers, and statistics lifecycle guards.

The library is included in both main solutions. Its test project is currently included in the Windows solution; it is not among the assemblies run by the default cross-platform GitHub test job. Run the explicit test-project command above when working on Consensus.

## 📁 Project Structure

| Path | Contents |
|---|---|
| [Blockchain/](Blockchain/) | State/operation/block contracts, base classes, linear chain, candidate graph, finalization, weights, and Merkle helpers |
| [MolinaTargetAlgorithm.cs](MolinaTargetAlgorithm.cs) | PascalCoin-style compact targets and digest conversion |
| [ICompactTargetAlgorithm.cs](ICompactTargetAlgorithm.cs) / [extensions](ICompactTargetAlgorithmExtensions.cs) | Target representation contract and allocating digest helpers |
| [IDAAlgorithm.cs](IDAAlgorithm.cs) | Difficulty adjustment contract |
| [ASERT_RTT.cs](ASERT_RTT.cs) / [ASERT2.cs](ASERT2.cs) | Wall-clock and block-to-block adjustment variants |
| [ASERTConfiguration.cs](ASERTConfiguration.cs) | Block interval and relaxation configuration |
| [CryptoTool.cs](CryptoTool.cs) | Checksum and indexed child-digest derivation |
| [HashStats.cs](HashStats.cs) | `PeriodicStatistics` lifecycle and unfinished history support |
| [DESIGN.md](DESIGN.md) | Broader design background and intended architecture |
| [Consensus tests](../../tests/Sphere10.Framework.Consensus.Tests/) | NUnit fixtures and the reference account ledger |

## ⚖️ License

Distributed under the **MIT License**.

See [LICENSE](../../LICENSE) for full details. More information: [MIT License](https://opensource.org/license/mit).

## 👤 Author

**Herman Schoenfeld** — Sphere 10 Software
