# Misc

Small shared policies, sort options, authentication results, a cycle detector, a decaying gauge, recursive data, UI target references and obfuscated URL identifiers.

These types have no common subsystem. `AlreadyExistsPolicy` and `SortDirection` are general-purpose enums that APIs across the Framework accept, for example `Tools.Web.Mvc.ToSelectList` sorts by `SortDirection`. `CycleDetector<T>` reports when a stream of items starts repeating, and its own summary marks it as not tested. `DecayGauge` models a level that decays as `((DecayOffset + elapsedSeconds) * DecayCoefficient) ^ DecayExponent` and is topped up with `RegisterEvent`. `RecursiveDataType<TState>` is a tree of states that flattens to parallel arrays of states and child counts and rebuilds from them. `UrlID.Generate` turns a sequential `uint` into a short non-sequential identifier with a three-round Feistel permutation and Base62 or hex encoding.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AlreadyExistsPolicy`](AlreadyExistsPolicy.cs) | enum | Whether an existing target is skipped, reported as an error or overwritten. |
| [`AuthenticationErrorCode`](AuthenticationErrorCode.cs) | enum | Outcome of a logon attempt, such as success, abort, invalid credentials or an unavailable server. |
| [`AuthenticationResult`](AuthenticationResult.cs) | class | Pairs an `AuthenticationErrorCode` with the authenticated user object. |
| [`CycleDetector<T>`](CycleDetector.cs) | class | Detects when added items begin repeating an earlier sequence and captures the last cycle. |
| [`DecayGauge`](DecayGauge.cs) | class | Sealed gauge whose level decays over time within bounds, with preset RSI and screen-mouse gauges. |
| [`RecursiveDataType<TState>`](RecursiveDataType.cs) | class | Tree of states with sub-states that flattens to and rebuilds from state and child-count sequences. |
| [`SortDirection`](SortDirection.cs) | enum | No sort, ascending or descending. |
| [`SortOption`](SortOption.cs) | class | Sort column name with its `SortDirection`. |
| [`UITargetReference`](UITargetReference.cs) | class | Cross-platform reference to a UI target and, where a platform needs it, the target's owner. |
| [`UrlID`](UrlID.cs) | class | Generates short obfuscated URL identifiers from `uint` values, with a nested `Format` enum for Base62 or hex output. |
