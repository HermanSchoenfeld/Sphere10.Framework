# Sphere10 Framework Agent Skills

Task-specific skills for working in this repository. Load the skill that matches the task before writing code.

| Skill | Use when |
|---|---|
| [code-style](code-style/SKILL.md) | Any code change. Baseline formatting, naming, ordering, license header. |
| [oo-design](oo-design/SKILL.md) | Designing any new type, hierarchy, or service. Interface → base → concrete → decorator layering and OO idioms. |
| [decorator-pattern](decorator-pattern/SKILL.md) | Layering behavior over an existing abstraction with a `FooDecorator` (`ExtendedListDecorator` reference). |
| [tools-namespace](tools-namespace/SKILL.md) | Adding or using a `Tools.*` static utility, or reaching for a raw BCL call that a tool covers. |
| [builder-pattern](builder-pattern/SKILL.md) | Creating a fluent `FooBuilder` with chainable `With*`/`Add*` methods and a terminal `.Build()`. |
| [state-machines](state-machines/SKILL.md) | Application workflows and session lifecycles with Stateless, typed triggers, hierarchical states, guarded transitions, and async actions. |
| [protocols](protocols/SKILL.md) | Application message protocols using `Protocol`, `ProtocolBuilder`, and `ProtocolOrchestrator`: commands, request/response, handshakes, modes, and wire serialization. |
| [serialization](serialization/SKILL.md) | Writing `IItemSerializer<T>` implementations, using `SerializerBuilder`/`SerializerFactory`, or touching endian-aware I/O. |
| [guards-and-scopes](guards-and-scopes/SKILL.md) | Argument/invariant validation (`Guard`), or resource/state cleanup via disposable scopes. |
| [disposable-scopes](disposable-scopes/SKILL.md) | Acquiring a resource, entering a state, locking, or guaranteed cleanup — the `IScope`/`Tools.Scope`/synchronization/transaction scope idiom. |
| [data-source](data-source/SKILL.md) | Implementing `IDataSource<T>` sync/async CRUD hierarchies (`SyncBatchDataSourceBase`, `AsyncBatchDataSourceBase`, etc.). |
| [nhibernate-dal](nhibernate-dal/SKILL.md) | Application DAL scaffolding with the established `DataObjects`, `BusinessEntity` maps, `DatabaseManager`, `DataScope`, provider managers, and priming/demo generator names; includes the SQL `ApplicationDAC` profile. |
| [crypto](crypto/SKILL.md) | Hashing, random bytes, digital signatures, key derivation. Never raw BCL crypto. |
| [logging](logging/SKILL.md) | Adding log output; `ILogger`, `SystemLog`, decorators and sinks. |
| [winforms-ui](winforms-ui/SKILL.md) | WinForms screens, wizards (`WinFormsWizardBuilder`), application blocks (`WinFormsApplicationBlockBuilder`), `CrudGrid`. |
| [crud-grid](crud-grid/SKILL.md) | Configuring or extending WinForms `CrudGrid`, inline and property-grid editing, data-source reference pickers, paging, and dropdown column layout. |
| [user-settings](user-settings/SKILL.md) | Persisting per-user preferences and UI state between sessions with `SettingsObject`, `UserSettings`, and automatic main-window placement. |
| [job-scheduler](job-scheduler/SKILL.md) | Recurring/one-shot background jobs via `JobBuilder` and `Scheduler`. |
| [unit-testing](unit-testing/SKILL.md) | Writing or modifying NUnit tests. |

These skills supplement [../copilot-instructions.md](../copilot-instructions.md), which remains authoritative.
