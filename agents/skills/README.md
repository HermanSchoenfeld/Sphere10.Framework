# Sphere10 Framework Agent Skills

Task-specific skills for working in this repository. Load the skills that match the concrete abstraction and requested behavior before writing code. Shared words such as application or lifecycle alone do not select a skill. See [Agent Guidance](../../docs/agent-guidance.md) for repository entry points, required reading and precedence.

Use the [Patterns and Practices task index](../../docs/guidelines/patterns-and-practices.md) to select the relevant pattern topics. Read those pages and the selected skill references; a task does not require the entire pattern collection.

| Skill | Use when |
|---|---|
| [code-style](code-style/SKILL.md) | Any code change. Baseline formatting, naming, ordering, license header. |
| [oo-design](oo-design/SKILL.md) | Designing a type, hierarchy or service. Choose interface, base, concrete and decorator responsibilities that the abstraction needs. |
| [decorator-pattern](decorator-pattern/SKILL.md) | Layering behavior over an existing abstraction with a `FooDecorator` (`ExtendedListDecorator` reference). |
| [tools-namespace](tools-namespace/SKILL.md) | Adding or using a `Tools.*` static utility, or reaching for a raw BCL call that a tool covers. |
| [builder-pattern](builder-pattern/SKILL.md) | Creating a fluent `FooBuilder` with chainable `With*`/`Add*` methods and a terminal `.Build()`. |
| [state-machines](state-machines/SKILL.md) | Existing Stateless workflows or user-authorized adoption: typed triggers, hierarchical states, guarded transitions and async actions. Preserve the existing `IApplication` lifecycle without adding Stateless. |
| [protocols](protocols/SKILL.md) | Application message protocols using `Protocol`, `ProtocolBuilder`, and `ProtocolOrchestrator`: commands, request/response, handshakes, modes, and wire serialization. |
| [serialization](serialization/SKILL.md) | Writing `IItemSerializer<T>` implementations, using `SerializerBuilder`/`SerializerFactory`, or touching endian-aware I/O. |
| [collections](collections/SKILL.md) | Selecting or composing extended lists, dictionary families, bidirectional maps, synchronized wrappers, projections or persistent collections. |
| [persistence](persistence/SKILL.md) | Consuming or implementing repository CRUD, `ILoadable`/`ISaveable`/`IPersistable`, or transaction-backed scopes and collections. |
| [object-spaces](object-spaces/SKILL.md) | Modeling or using ObjectSpace dimensions/indexes, object-store CRUD screens, dirty tracking, transactional persistence and configuration profiling. |
| [guards-and-scopes](guards-and-scopes/SKILL.md) | Argument/invariant validation (`Guard`), or resource/state cleanup via disposable scopes. |
| [disposable-scopes](disposable-scopes/SKILL.md) | Acquiring a resource, entering a state, locking, or guaranteed cleanup — the `IScope`/`Tools.Scope`/synchronization/transaction scope idiom. |
| [data-source](data-source/SKILL.md) | Implementing `IDataSource<T>` CRUD, paging, refresh and validation with the item-first or batch-first sync/async bases. |
| [nhibernate-dal](nhibernate-dal/SKILL.md) | Application DAL scaffolding with the established `DataObjects`, `BusinessEntity` maps, `DatabaseManager`, `DataScope`, provider managers, and priming/demo generator names; includes the SQL `ApplicationDAC` profile. |
| [crypto](crypto/SKILL.md) | Hashing, random bytes, digital signatures, key derivation. Never raw BCL crypto. |
| [logging](logging/SKILL.md) | Adding diagnostics through `ILogger`, `IApplication.Logger`, `SystemLog`, decorators and owned sinks. |
| [winforms-ui](winforms-ui/SKILL.md) | WinForms application/plugin registration and lifecycle, blocks, hosted screens, async dialogs and wizards. |
| [crud-grid](crud-grid/SKILL.md) | Configuring or extending WinForms `CrudGrid`, inline and property-grid editing, data-source reference pickers, paging, and dropdown column layout. |
| [user-settings](user-settings/SKILL.md) | Persisting per-user preferences and UI state between sessions with `SettingsObject`, `UserSettings`, and automatic main-window placement. |
| [job-scheduler](job-scheduler/SKILL.md) | Recurring/one-shot background jobs via `JobBuilder` and `Scheduler`. |
| [unit-testing](unit-testing/SKILL.md) | Writing or modifying NUnit tests. |

These skills supplement the authoritative [repository instructions](../../agents/instructions.md); follow the precedence described in [Agent Guidance](../../docs/agent-guidance.md).

Before adding, changing, renaming or retiring a skill, follow the [guidance maintenance workflow](../../agents/instructions.md#maintain-skills-and-patterns).
