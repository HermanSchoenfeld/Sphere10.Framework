# ObjectSpace profiling lab

This subfolder supplies deterministic sample models, declared schemas and storage configuration to the [ObjectSpace benchmarks](../README.md) in the [general profiler](../../README.md). The sources compile into that project; there is no separate lab library or UI dependency.

`ObjectSpaceLabRecord` has a fixed-size identifier (`Id`), variable-size unique code, nonunique category, balance, byte payload and notes. Its transient `Dirty` flag supports comparing explicit save with dirty-flag autosave. `Clone()` copies the payload so detached editors cannot mutate the stored entity through an array alias.

`ObjectSpaceLabSchemaProfile.PayloadOnly` is the benchmark default and retains the original Record dimension. The existing versioned `DeveloperLab` profile is retained for compatibility tests and declares three dimensions in fixed order:

| Dimension | Persisted model | Full secondary indexes |
| --- | --- | --- |
| Records | Long Id, Code, Category, Balance, binary Payload, Notes | Unique string Code; nonunique int Category |
| Groups | Int Id, Code, Name | Unique string Code |
| Events | Long Id, RecordId, int Kind, TimestampTicks, Message | Nonunique long RecordId and int Kind |

Each dimension always has its identifier and recyclable-slot store. `IdentifierOnly` omits secondary indexes from every dimension; `Full` installs those in the table. The explicit schema ignores annotations so minimal profiles do not acquire inferred indexes. Every model has a transient dirty flag; Record cloning deep-copies its payload. Category/RecordId are scalar sample relationships, with no automatic foreign-key or graph-GC guarantee. GC is disabled.

## Use the factory

- `Open(configuration, filePath, pagesDirectory, memoryStream)` returns a loaded space. Memory streams supplied by the caller remain caller-owned.
- `CreateRecord(id, payloadBytes)` creates an untracked deterministic record. `AcceptNew(record)` before saving it.
- `CreateGroup(id)` and `CreateEvent(id, recordId)` create untracked deterministic objects for DeveloperLab. Event timestamps derive from the ID rather than the wall clock.
- `Seed(space, count, payloadBytes)` explicitly saves into an empty Record dimension. It does not seed Groups/Events or commit/flush automatically.
- `Complete(space)` commits a file space or flushes a memory space.
- `ReadConfiguration(filePath)` loads the configuration recorded alongside a lab file.

File workspaces have a versioned `<file>.objectspace.json` sidecar. The factory rejects missing sidecars on nonempty files and mismatched configurations before loading the store. Copy the sidecar with a committed baseline, and retain the same schema and configuration when reopening. An existing arbitrary ObjectSpace file is not necessarily a compatible lab file. File staging defaults to `<file>.pages` unless supplied explicitly.

New sidecars use `SchemaVersion = 4` and require the configuration fields, including `SchemaProfile`. A selected type is stored as a complete `IndexBTreeSpec` with concrete `Type` and valid `Order`; an omitted specification uses a bare `IndexBTreeOrder` and retains historical mixed indexes. Version 3 maps a concrete recorded `IndexBTreeImplementation` into that specification and retains the bare order when no type was recorded. Versions 1 and 2 require the original order, map missing tree type to the historical mixed layout and cannot declare an explicit type. Version 1 also maps explicitly to PayloadOnly; it cannot declare DeveloperLab. Conflicting legacy/current tree fields and unsupported versions are rejected; repeated order fields must agree. This compatibility path preserves the original Record layout and does not reinterpret a one-dimension file as three dimensions. These sidecars belong to the lab application; the Framework passes tree specifications through its builders and constructors without storing them in tree headers.

Configuration includes storage backend, schema profile, an optional immutable B-tree specification (type and order) or the bare order, outer cluster size, inner cluster-size hint, file page/cache sizes, clustered-stream policy, endianness, indexes, autosave, Merkle traits and the Merkle hash algorithm. The default hash is SHA2_256; algorithms need a registered fixed-size digest and must match on reopen. Current member-checksum indexes use their own default digest independently. `Validate()` returns the framework `Result`; `Clone()` detaches settings, with immutable specifications safely shared until replaced. `EffectiveDimensionClusterSize` reports the clipped hint. File page/cache settings have no memory-backend effect, and cache memory is not total process memory.

The current ObjectSpace benchmark domain rejects DeveloperLab because its measured workloads operate on Records only. Add explicit multi-dimension workloads before claiming performance results for that schema.

The [ObjectSpace guide](../../../../src/Sphere10.Framework/ObjectSpaces/README.md) describes the actual core APIs and persistence limitations. [Profiler tests](../../../../tests/Sphere10.Framework.Utils.Profiler.Tests/README.md) cover configuration safety, indexes, autosave, transactions, reopen and detached payload ownership.

The current ObjectSpace retains dimension-stream locks on its creation thread. Keep factory creation, operations and disposal on one owner thread; benchmark fixtures use their synchronous execution thread. Per-operation access scopes do not permit migrating an existing store across arbitrary workers.
