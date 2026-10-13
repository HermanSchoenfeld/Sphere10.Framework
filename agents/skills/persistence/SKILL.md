---
name: persistence
description: Consume or implement Sphere10 IRepository CRUD, ILoadable, ISaveable, IPersistable, transactional scopes and transaction-backed collections. Use for loading/saving state, repository execution direction, commit/rollback ownership or persistent backend lifecycle work.
---

# Persistence Skill

1. Read [repositories, persistence and transactions](../../../docs/guidelines/patterns/persistence-and-transactions.md). Identify whether the task needs entity CRUD, load/save lifecycle, a transaction owner, or a combination; inspect the existing concrete backend and its consumers.
2. Read [sync/async hierarchies](../../../docs/guidelines/patterns/sync-async.md) when implementing paired operations. Select the existing directional base, and verify pending work, errors and disposal complete through the chosen direction.
3. Trace ownership of the repository, transaction, stream and any nested scopes. Verify where loading becomes required, where mutations mark state dirty, and what commit, rollback and disposal actually do. Select only operations supported by that backend.
4. Read [resource scopes](../../../docs/guidelines/patterns/resource-scopes.md) for cleanup, and [synchronization](../../../docs/guidelines/patterns/synchronization.md) if locks or synchronized repository wrappers participate. Read [collection selection](../../../docs/guidelines/patterns/collections.md) for storage collections and [ObjectSpace](../object-spaces/SKILL.md) for indexed object stores.
5. For database-specific work, read the affected project README and [NHibernate DAL skill](../nhibernate-dal/SKILL.md) when its application scaffolding applies. Distinguish that scaffold's `DataScope` from the framework's concrete `DACScope` and `DataAccessScope` APIs.
6. Validate the changed backend's lifecycle with meaningful failure, nested ownership or persisted reopen checks. Verify successful work is durable only where its concrete commit contract promises durability, and preserve the original error if cleanup also fails.

Keep reusable findings in the central guides through the [guidance maintenance workflow](../../../agents/instructions.md#maintain-skills-and-patterns).
