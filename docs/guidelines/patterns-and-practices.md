# Patterns and Practices

This is the task index for Sphere10's design and implementation practices. Read
the topics that apply to the work before choosing an implementation; read any
additional references selected by those topics. Re-evaluate the selection when
the task expands. A task can require several topics.

Use the [coding guide](code-styling.md) for formatting, naming, compiler conventions
and source headers. Follow the [agent workflow](../../agents/instructions.md) and
the matching [repository skills](../../agents/skills/README.md). The user's explicit
instructions take precedence; these topic files hold the implementation rules.

| Work being done | Read |
|---|---|
| Any framework implementation or change to its consumers | [Framework reuse and adoption](patterns/framework-reuse.md) |
| Choosing interfaces, abstract bases, concrete products or decorators | [Type hierarchies](patterns/type-hierarchies.md) |
| Adapting typed serializers, collections, data sources, caches or comparers to another representation | [Projection adapters](patterns/projections.md) |
| Paired synchronous/asynchronous APIs, adapters or disposal | [Sync/async implementation hierarchies](patterns/sync-async.md) |
| Specialized interfaces projecting base properties | [Inline interface bridges](patterns/interface-bridges.md) |
| Application/plugin registration, ownership graphs, screens or contextual commands | [Application ownership and platform extensions](patterns/application-ownership.md) |
| Fluent configuration and nested builders | [Fluent builders](patterns/fluent-builders.md) |
| Selecting utilities, guards or cryptographic infrastructure | [Framework utilities](patterns/framework-utilities.md) |
| Serialization, binary layouts or endian-aware I/O | [Serialization](patterns/serialization.md) |
| Selecting extended lists, dictionaries, bidirectional maps, sets or collection decorators | [Collection selection and composition](patterns/collections.md) |
| Shared state, read/write locks, compound operations or synchronized enumeration | [Synchronization with read/write scopes](patterns/synchronization.md) |
| Repository CRUD, loading/saving, persistable state or transactional backends | [Repositories, persistence and transactions](patterns/persistence-and-transactions.md) |
| Graph storage, caches, clustered streams or storage comparers | [Collections and storage](patterns/collections-and-storage.md) |
| ObjectSpace schemas, identity, indexed CRUD, file transactions or configuration profiling | [Object spaces](patterns/object-spaces.md) |
| Data-source CRUD, paging or batch/single operations | [Data sources](patterns/data-sources.md) |
| Application logging, sinks or logger decorators | [Logging](patterns/logging.md) |
| Acquisition, cleanup, locking, transactions or disposal scopes | [Resource scopes](patterns/resource-scopes.md) |
| Background jobs or schedules | [Scheduling](patterns/scheduling.md) |
| Adding, changing or running NUnit fixtures | [Testing](patterns/testing.md) |
| Agent instruction discovery, shared skills or local aliases | [Agent guidance structure](patterns/agent-guidance.md) |
| Architectural layer boundaries | [3-Tier Architecture](3-tier-architecture.md) |

Open the affected [project documentation](../README.md) for the concrete API and
its current consumer path. The topic files describe the practice; project guides
and selected skills supply the relevant application details.

Before adding, changing, renaming or retiring a topic, follow the
[guidance maintenance workflow](../../agents/instructions.md#maintain-skills-and-patterns).
