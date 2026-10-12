---
name: nhibernate-dal
description: Create or extend the user's application DAL with its established DataObjects, DataAccessLayer, BusinessEntity maps, DatabaseManager, DataScope, provider managers, and priming/demo generator names. Use for NHibernate application persistence and the related ApplicationDAC SQL profile.
---

# NHibernate DAC/DAL

Reproduce the established application architecture and naming contract when asked to build a DAL. Keep the model, mappings, database lifecycle, data generation, and business operations separate. The shared pattern supports two concrete persistence profiles; select the existing application's profile, or use the NHibernate runtime profile for a new NHibernate CRUD application.

- **NHibernate runtime profile:** `<Product>.DataObjects`, `<Product>.DataAccessLayer`, and `<Product>.BusinessLogic`. `DatabaseManager` owns the application session factory; `DataScope` owns a unit of work; business services/data sources use its session.
- **SQL DAC profile:** `<Product>.DataObjects`, `<Product>.DataAccess`, and `<Product>.DataAccess.NHibernate`. `ApplicationDAC` supplies typed SQL operations; NHibernate maps and generators supply schema and initial data. Use this profile for an existing split DAL or requested SQL/bulk processing architecture. Read [SQL DAC profile](references/sql-dac-profile.md) before implementing it.

Both use separate entity maps, provider-specific database managers, `PrimingDataGenerator : NHDataGeneratorBase`, and optional demo generation. A SQL-only CRUD layer is not an adequate implementation of a requested NHibernate runtime DAL.

Read [the naming and class contract](references/nhibernate-runtime-contract.md) before scaffolding the NHibernate runtime profile. Substitute only the product, entity, and supported-provider names. Preserve literal infrastructure names and hook names; do not rename them to generic repository, seeder, context, or service alternatives. Existing application contracts and explicit user changes take precedence. The profile choices consolidate observed variations; they do not imply both source applications declare every class.

Load [code-style](../code-style/SKILL.md), [oo-design](../oo-design/SKILL.md), and [disposable-scopes](../disposable-scopes/SKILL.md) for implementation. Inspect target dependencies and actual Framework types rather than copying historical namespaces or package versions.

## Core naming contract

| Role | NHibernate runtime name | Responsibility |
|---|---|---|
| Entity base | `BusinessEntity` | `ID`, `Active`, `RowVersion`, `LastUpdatedOn`; domain entities derive from it. |
| Base maps | `BusinessEntityMap<T>`, `BusinessEntitySubclassMap<T>` | Shared mapping and `MapEntity()` template method; subclass base only for mapped inheritance. |
| Entity/component maps | `<Entity>Map`, `<Component>Map` | `MapEntity()` for business entities; `ComponentMap<T>` for owned value objects. |
| Connection settings | `DatabaseConnectionSetting` | `DBMSType` and `ConnectionString`. |
| Application facade | `DatabaseManager` | Registration, creation, initialization, provider selection, `ApplicationSessionFactory` ownership. |
| Unit of work | `DataScope` | `Session`, `Transaction`, `Current`, `CurrentScopeExists`, `Commit()`, `Rollback()`. |
| Shared NH configuration | `<Product>DatabaseManagerBase` | `ApplyCommonConfiguration(...)`, generator policy selection. |
| Providers | `<Product>DatabaseManagerMSSQL`, `<Product>DatabaseManagerSqlite`, `<Product>DatabaseManagerFirebird` | Only implement supported providers; each supplies `GetFluentConfig(...)`. |
| Required initial records | `PrimingDataGenerator` | Sealed `CreateData()` pipeline and `CreateAdditionalData()` extension point. |
| Additional reference catalog | `ExtendedPrimingDataGenerator` | Optional second tier; extends `CreateAdditionalData()` and exposes `CreateExtendedAdditionalData()`. |
| Example/development records | `DemoDataGenerator`, `DevDataGenerator` | Optional leaf generators that append records without replacing priming. |
| CRUD adapter | `BusinessEntityDataSource<TEntity>` | Shared NH CRUD/query hooks in BusinessLogic, when data-source UI integration is needed. |
| Audit/log hooks | `NHibernateListener`, `LoggingInterceptor`, `SqlLog` | Optional entity audit updates and SQL diagnostics. |

These are application classes except the explicitly named Framework/NHibernate bases. Do not assume Framework already provides `BusinessEntity`, `DataScope`, `PrimingDataGenerator`, or an `NHibernateSessionFactory` helper.

## Data objects and mappings

For the runtime profile, keep entities in `.DataObjects` and maps in `.DataAccessLayer/Mappings`. `BusinessEntity` establishes the shared persistence fields; each business entity has virtual persistent members, initialized collections, and `Add<Child>` helpers that maintain both relationship ends. Keep session access and database configuration out of data objects.

`BusinessEntityMap<T> : ClassMap<T>` maps identity and common fields once, then delegates entity-specific layout to `public override void MapEntity()`. For this profile, do not replace every map with an independent `ClassMap<T>` constructor. Use `BusinessEntitySubclassMap<T> : SubclassMap<T>` only where mapped inheritance needs it, and avoid mapping inherited fields twice. Embedded value objects use `ComponentMap<T>` and `Component(...)`.

Keep ID strategy, entity ID type, assigned lookup IDs, column nullability/length/precision, hydrators and bulk column types aligned. The runtime baseline uses `int ID`, transient sentinel `-1`, native generation named `SEQ_<Entity>`, and `base(true)` for assigned keys. Change these together only for an actual domain/provider requirement; the SQL profile has its own identity contract.

Set explicit table names, relationship ownership, foreign-key names, and cascade behavior. Register maps through `FluentMappings.AddFromAssemblyOf<TMap>()`. Read the actual [conventions](../../../src/Sphere10.Framework.Data.NHibernate/Conventions): Framework `CoreConventions` does not automatically cascade, while older application-local conventions do. Apply cascade according to ownership; do not import blanket cascade as a naming convention. `BinaryColumnLengthConvention` hardcodes SQL Server `varbinary(MAX)` and needs provider/column-specific selection.

The runtime profile pairs fixed `Known<Lookup>Types` enum values (for example, `KnownTicketStatusTypes`) with assigned-key lookup entities. Alternatively, an application already using the generic Framework lookup model can use [TypeTable<TEnum>](../../../src/Sphere10.Framework.Data.NHibernate/DataPatterns/TypeTable.cs), a concrete subclass of [TypeTableMap<TEnum>](../../../src/Sphere10.Framework.Data.NHibernate/DataPatterns/TypeTableMap.cs), and `NHDataGeneratorBase.CreateTypeTable<TEnum>()`. Do not introduce duplicate lookup families or regenerate existing enum IDs.

`RowVersion` in the reference pattern is an application-maintained `long` audit counter mapped as a regular property. It is not automatically NHibernate optimistic concurrency. If concurrency checking is required, implement and test a compatible version mapping rather than assuming the field already supplies it.

## Database managers and factory ownership

Use `<Product>DatabaseManagerBase : NHDatabaseManagerBase` to share map registration, selected conventions, listeners/interceptors, identifier quoting, and generation policy. Its provider subclasses decorate the matching physical database manager and implement `GetFluentConfig(string ConnectionString)`. Keep `ApplyCommonConfiguration(FluentConfiguration Configuration)` centralized. Implement only providers the app supports.

`DatabaseManager` is the application facade, distinct from both the product manager base and the Framework physical manager. Retain `HasRegisteredDatabase`, `RegisterDatabase(...)`, `CreateDatabase(...)`, `InitializeApplicationDatabase(out string ErrorMessage)`, private `CreateDatabaseManager(DBMSType)`, and `ApplicationSessionFactory`. See the linked contract for argument roles.

Creation builds the schema and selected data through [NHDatabaseManagerBase](../../../src/Sphere10.Framework.Data.NHibernate/Database/NHDatabaseManagerBase.cs). Initialization only opens the configured database and publishes the factory after success. NHDatabaseManagerBase does not automatically call the physical manager to create an empty database; retain a provider-specific physical creation step where the DBMS requires it before schema export. Retain `SetCreateDatabaseConfiguration(...)` and `OnDatabaseSchemasCreated(...)` only for actual provider customization; an override that only deletes a file does not create its schema.

Open and cache one `ISessionFactory` for the application's configured database, create short-lived sessions, and dispose the owned factory on shutdown or safe database replacement. `OpenDatabase(string)` currently builds a factory on every call and is nonvirtual. Cache at the application facade or compose an `INHDatabaseManager` adapter; do not create factories per request or attempt a nonexistent caching override.

For the SQL profile's module-based composition, preserve `ModuleConfiguration` and named provider registration rather than adding a competing static selector. Match registration and retrieval service types; a named `IDatabaseManager` registration is not also a named `INHDatabaseManager` registration.

## Generator pipeline

`PrimingDataGenerator(ISessionFactory SessionFactory, ...required seed inputs...) : base(SessionFactory)` constructs mandatory initial records. Its `protected sealed override IEnumerable<object> CreateData()` composes named `Create<PluralRecords>()` methods in dependency order, then calls its profile's extension hook.

For the NH runtime profile, use `CreateAdditionalData()`. `ExtendedPrimingDataGenerator` overrides it, preserves `base.CreateAdditionalData()`, appends extra reference records, then calls `CreateExtendedAdditionalData()`. `DemoDataGenerator` and `DevDataGenerator` override that latter hook. If no extra reference tier exists, derive the leaf directly from `PrimingDataGenerator` and override `CreateAdditionalData()`. For the SQL profile, preserve the established `CreateNonPrimingData()` hook instead. Do not seal or replace the wrong hook.

[The Framework generator base](../../../src/Sphere10.Framework.Data.NHibernate/Database/NHDataGeneratorBase.cs) owns `Populate()`: open session, begin transaction, enumerate `CreateData()`, `SaveOrUpdate` each object, commit, dispose. Application generators return objects and extend generation hooks; they do not open independent sessions, start parallel transactions, or call `Populate()` on another generator. Build reference objects first and retain the same instances for dependent records.

`CreateDataGenerator(ISessionFactory, string DatabaseName, DatabaseGenerationDataPolicy)` selects `EmptyDataGenerator` for NoData, `PrimingDataGenerator` for PrimingData, and a complete priming-plus-demo generator for DemoData. The Framework currently bypasses that hook for NoData, so keep the direct hook consistent too. A `DemoDataGenerator` class existing on disk does not mean it is wired into the switch. `DevDataGenerator` is an explicitly selected development path; the Framework enum has no DevData member.

Pass necessary seed inputs such as a database name or an initial administrator hash through constructors. Do not invent an administrator/user model in apps that do not need one, or copy source passwords, sample identities, or business seed data. Required priming is deterministic; optional demo generation must preserve valid relationships and required priming. These are creation-time generators, not a replacement for schema migrations.

## DataScope and business operations

The runtime contract is `using (var Scope = new DataScope()) { ... Scope.Session ... Scope.Commit(); }`. Its default constructor obtains a session from `DatabaseManager.ApplicationSessionFactory` and starts a transaction; unlike a `DACScope`, it does not require a second `BeginTransaction()` call. `DataScope.Current.Session` supports participating business services during that unit of work. Define ownership, nesting and async behavior according to the linked contract; disposal never commits implicitly.

Keep all required lazy access inside the live session; return materialized/prefetched results. When merging a detached entity, use the managed object returned by `Session.Merge(...)`. Do not share an NH session between concurrent operations. Own the transaction around the complete business operation rather than silently assuming several independently committed CRUD calls are atomic.

When CRUD data sources are needed, retain `BusinessEntityDataSource<TEntity>` and concrete `<Entity>DataSource` names with `MainQuery`, `ApplySearchFilter`, `ApplySort`, `ApplyPaging`, `Prefetch`, `ValidateItem`, `SaveOrUpdate`, and `DeleteItem`. Load [data-source](../data-source/SKILL.md) and implement the current `SyncBatchDataSourceBase` Range signatures. Apply stable ordering before paging and prefetch page relationships before disposal. Honor `Active` for the chosen soft-delete model. The old per-item write scopes do not provide atomic batch writes.

Framework [DataAccessScope](../../../src/Sphere10.Framework.Data.NHibernate/Scopes/DataAccessScope.cs) is a different abstraction: it opens through `INHDatabaseManager`, supports context nesting, rejects ambient `System.Transactions`, and does not start a transaction merely by construction. Its current factory ownership and cleanup-flush behavior need checking before using it behind `DataScope`; it is not a drop-in alias. Do not assume NH and SQL DAC scopes join the same transaction just because their connection strings match.

## Implementation completion

Produce the chosen profile's project/file tree and use the literal class/hook names above. Add each feature's data object, map, required priming data, and business access path together. Explain any deliberate naming/profile deviation in the implementation result; ordinary product/entity substitutions need no extra approval.

Validate with [unit-testing](../unit-testing/SKILL.md) on disposable databases for supported providers: mapping discovery/schema creation, deterministic priming, NoData/PrimingData/DemoData contents, shared seed references, commit and rollback from a fresh session, scope cleanup, detached merge/prefetch, and relevant batch/concurrency behavior. Include listener/audit behavior if used. Verify that initialization preserves existing data and does not run schema creation or demo generation. Never claim source recipes are proven merely because the skill or map compiles.
