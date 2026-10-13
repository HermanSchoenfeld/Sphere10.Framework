---
name: collections
description: Select, consume or compose Sphere10 extended lists, dictionaries, bidirectional maps, sets, synchronized wrappers, projected views, paged storage and persistent collections. Use when choosing a collection family or implementing its contracts and decorators.
---

# Collections Skill

1. Read [collection selection and composition](../../../docs/guidelines/patterns/collections.md). Identify the required lookup, ordering, uniqueness, count/index width, range operations and storage lifetime, then select an existing family from that guide.
2. Inspect the selected interface, concrete implementation and a real consumer or fixture. Check whether returned ranges, keys, values and enumerators are borrowed or materialized, and whether logical positions differ from storage slots.
3. Read [synchronization](../../../docs/guidelines/patterns/synchronization.md) when access is shared. Identify self-locking members and the outer scope needed for compound operations or deferred results; configure a shared parent before use.
4. Read [projection adapters](../../../docs/guidelines/patterns/projections.md) when exposing another representation. Read [collections and storage](../../../docs/guidelines/patterns/collections-and-storage.md) for cache, stream layout or comparer work, and [persistence and transactions](../../../docs/guidelines/patterns/persistence-and-transactions.md) for transactional collections. Use the [ObjectSpace skill](../object-spaces/SKILL.md) when the task is an object store rather than a collection alone.
5. Preserve the selected contract's count, equality, ordering, mutation and ownership semantics. Validate those behaviors at the actual consumer boundary, including the specific range, projection, concurrency or reopen behavior changed by the task.

Follow [oo-design](../oo-design/SKILL.md) and [decorator-pattern](../decorator-pattern/SKILL.md) when implementing a base or wrapper. Keep reusable findings in the central guides through the [guidance maintenance workflow](../../../agents/instructions.md#maintain-skills-and-patterns).
