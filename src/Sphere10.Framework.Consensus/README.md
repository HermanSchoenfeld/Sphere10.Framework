# ⛓️ Sphere10.Framework.Consensus

<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**Blockchain consensus primitives** providing difficulty adjustment algorithms, compact target encoding, and real-time target calculation for proof-of-work systems.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Consensus
```

## 🏗️ Core Architecture

### IDAAlgorithm - Difficulty Adjustment

The `IDAAlgorithm` interface defines the contract for difficulty adjustment:

```csharp
public interface IDAAlgorithm {
    bool RealTime { get; }
    
    uint CalculateNextBlockTarget(
        IEnumerable<DateTime> previousBlockTimestamps, 
        uint previousCompactTarget, 
        uint blockNumber);
}
```

**Implementations:**

| Class | Description |
|-------|-------------|
| `ASERT_RTT` | ASERT algorithm using real-time (head time to current time) |
| `ASERT2` | ASERT algorithm using block-to-block time (head-1 to head) |

### ICompactTargetAlgorithm - Target Encoding

The `ICompactTargetAlgorithm` interface handles conversion between full targets and compact representations:

```csharp
public interface ICompactTargetAlgorithm {
    uint MinCompactTarget { get; }
    uint MaxCompactTarget { get; }
    
    uint FromTarget(BigInteger target);
    uint FromDigest(ReadOnlySpan<byte> digest);
    BigInteger ToTarget(uint compactTarget);
    void ToDigest(uint compactTarget, Span<byte> digest);
    uint AggregateWork(uint compactAggregation, uint newBlockCompactWork);
}
```

## 🔧 MolinaTargetAlgorithm

The `MolinaTargetAlgorithm` implements compact target encoding invented by Albert Molina (PascalCoin):

```csharp
using Sphere10.Framework.Consensus;

var targetAlg = new MolinaTargetAlgorithm();

// Convert 256-bit target to compact 32-bit representation
BigInteger fullTarget = BigInteger.Parse("00000FFFFFFFFFFFFFFFFFFFFFFFFFFF...");
uint compactTarget = targetAlg.FromTarget(fullTarget);

// Convert compact back to full target
BigInteger reconstructed = targetAlg.ToTarget(compactTarget);

// Convert hash digest to compact target
byte[] digest = ComputeBlockHash(blockData);
uint digestTarget = targetAlg.FromDigest(digest);

// Get target as byte array
byte[] targetBytes = targetAlg.ToDigest(compactTarget);
```

**Key Properties:**
- `MinCompactTarget`: 134217728 (easiest difficulty)
- `MaxCompactTarget`: 3892314111 (hardest difficulty)
- **Orderable**: Higher compact values = harder difficulty (unlike Bitcoin's nBits)

## 🔧 ASERT Difficulty Adjustment

### Configuration

```csharp
using Sphere10.Framework.Consensus;

var config = new ASERTConfiguration {
    BlockTime = TimeSpan.FromSeconds(60),      // Target 60 seconds per block
    RelaxationTime = TimeSpan.FromHours(1)     // Smoothing half-life
};
```

### ASERT_RTT (Real-Time Target)

Calculates difficulty based on time since last block (real-time):

```csharp
var targetAlg = new MolinaTargetAlgorithm();
var config = new ASERTConfiguration {
    BlockTime = TimeSpan.FromSeconds(60),
    RelaxationTime = TimeSpan.FromHours(1)
};

IDAAlgorithm da = new ASERT_RTT(targetAlg, config);

// Calculate next block's target
var previousTimestamps = new[] { lastBlockTime };
uint currentTarget = lastBlockCompactTarget;

uint nextTarget = da.CalculateNextBlockTarget(
    previousTimestamps, 
    currentTarget, 
    blockNumber
);
```

### ASERT2 (Block-to-Block Time)

Calculates difficulty based on time between consecutive blocks:

```csharp
IDAAlgorithm da = new ASERT2(targetAlg, config);

// Requires at least 2 timestamps (head and head-1)
var previousTimestamps = new[] { headTime, headMinus1Time };

uint nextTarget = da.CalculateNextBlockTarget(
    previousTimestamps,
    currentTarget,
    blockNumber
);
```

## 📊 Target Calculation Formula

The ASERT algorithm uses exponential adjustment:

```
nextTarget = previousTarget × exp((timeDelta - blockTime) / relaxationTime)
```

Where:
- `timeDelta`: Time since last block (ASERT_RTT) or between blocks (ASERT2)
- `blockTime`: Target block time (e.g., 60 seconds)
- `relaxationTime`: Smoothing parameter (e.g., 1 hour)

**Behavior:**
- Block found too quickly → difficulty increases (higher compact target)
- Block found too slowly → difficulty decreases (lower compact target)
- Exponential smoothing prevents oscillation

## 🔗 Blockchain Primitives

The library also provides generic state-transition and chain-management primitives for building full blockchains.

### State Transition

```csharp
using Sphere10.Framework.Consensus;

// Define your ledger state
public class MyState : BlockchainStateBase {
    // implement your ledger data
}

// Define invertible operations
public class MyOperation : BlockchainOperationBase<MyState> {
    protected override void ApplyInternal(MyState state) { /* mutate */ }
    protected override void UndoInternal(MyState state) { /* reverse */ }
}

// Define blocks
public class MyBlock : BlockchainBlock<MyState> {
    public MyBlock(IEnumerable<IBlockchainOperation<MyState>> ops) : base(ops) { }
}
```

### Chain Management

```csharp
using Sphere10.Framework.Consensus;

var state = new MyState();
var chain = new Blockchain<MyBlock, MyState>(state);

// Apply blocks
chain.ApplyBlock(block1);
chain.ApplyBlock(block2);

// Undo last block
chain.UndoBlock();

// Branching frontier
var unfinalized = new FinalizedBlockchain<MyBlock, MyState>(chain);
var head = unfinalized.AddUnfinalizedRoot(candidateBlock, candidateState);
unfinalized.FinalizePath(head);
```

## 📁 Project Contents

| File | Description |
|------|-------------|
| `IDAAlgorithm.cs` | Difficulty adjustment algorithm interface |
| `ICompactTargetAlgorithm.cs` | Compact target encoding interface |
| `MolinaTargetAlgorithm.cs` | PascalCoin-style compact target implementation |
| `ASERT_RTT.cs` | ASERT with real-time calculation |
| `ASERT2.cs` | ASERT with block-to-block time |
| `ASERTConfiguration.cs` | ASERT configuration (block time, relaxation) |
| `HashStats.cs` | Periodic statistics tracking |
| `Blockchain\IBlockchainState.cs` | State interface |
| `Blockchain\IBlockchainOperation.cs` | Operation interface |
| `Blockchain\IBlockchainBlock.cs` | Block interface |
| `Blockchain\BlockchainBlock.cs` | Block base implementation |
| `Blockchain\BlockchainOperationBase.cs` | Operation base implementation |
| `Blockchain\BlockchainStateBase.cs` | State base implementation |
| `Blockchain\Blockchain.cs` | Linear finalized chain with Apply/Undo |
| `Blockchain\UnfinalizedBlockGraph.cs` | Branching unfinalized frontier |
| `Blockchain\FinalizedBlockchain.cs` | Two-sector chain with re-org support |
| `CryptoTool.cs` | Secure checksum and hierarchical digest derivation |
| `Blockchain\MerkleHelper.cs` | Merkle root computation for blocks/operations |

## ✅ Best Practices

- **Use ASERT_RTT** for real-time difficulty adjustment during mining
- **Use ASERT2** for block validation (deterministic, uses block timestamps only)
- **Configure RelaxationTime** appropriately for network hash rate volatility
- **Use MolinaTargetAlgorithm** for orderable compact targets

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

`PeriodicStatistics` requires a positive period. `Start()` records the UTC start time, and events can then be registered against initialized statistics.
