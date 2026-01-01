<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Consensus

This project contains consensus-related primitives that are currently focused on **difficulty / target** algorithms.

## Contents

- `ICompactTargetAlgorithm` + `MolinaTargetAlgorithm`: Compact target encoding/decoding utilities.
- `IDAAlgorithm`: Interface for difficulty adjustment algorithms.
- `ASERT_RTT`, `ASERT2`, `ASERTConfiguration`: ASERT-style real-time difficulty adjustment.
- `PeriodicStatistics` (in `HashStats.cs`): small helper for periodic stats tracking.

## Quick usage

```csharp
using Sphere10.Framework.Consensus;

ICompactTargetAlgorithm targetAlg = new MolinaTargetAlgorithm();
var cfg = new ASERTConfiguration {
	BlockTime = TimeSpan.FromSeconds(60),
	RelaxationTime = TimeSpan.FromHours(1)
};

IDAAlgorithm da = new ASERT_RTT(targetAlg, cfg);
```

## License

Distributed under the **MIT NON-AI License**.


