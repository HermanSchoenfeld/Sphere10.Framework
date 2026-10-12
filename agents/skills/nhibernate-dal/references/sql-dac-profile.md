# SQL DAC profile

Use this profile when an application needs typed SQL operations and bulk persistence over Framework `IDAC`, with NHibernate defining the relational schema and seed data. It supplements [the main NHibernate skill](../SKILL.md); do not introduce this additional profile when a session-based DAL already covers the task.

## Exact scaffold names and layout

Replace `<Product>` with the application's namespace/product prefix and `<Entity>` with an entity name. Keep the shared class names below. Folder partitions do not automatically add namespace segments.

```text
<Product>.DataObjects/
  <Entity>.cs
<Product>.DataAccess/
  <Product>Database.cs
  DAC/
    ApplicationDAC.cs
    <Entity>DAC.cs
    BatchInsertDAC.cs                    # when bulk operations are needed
  Hydrators/
    <Entity>Hydrator.cs
  VendorSpecific/                       # when provider differences require it
    IDBVendorSpecificImplementation.cs
    DBVendorSpecificImplementationBase.cs
    MSSQLVendorSpecificImplementation.cs
    NoOpVendorSpecificImplementation.cs
<Product>.DataAccess.NHibernate/
  ModuleConfiguration.cs
  DatabaseManagers/
    <Product>DatabaseManagerMSSQL.cs
    <Product>DatabaseManagerSqlite.cs
  DataGenerators/
    PrimingDataGenerator.cs
    DemoDataGenerator.cs                 # only when demo data is required
  Mappings/
    <Entity>Map.cs
```

| Files | Declared types and namespace |
|---|---|
| `DataObjects/<Entity>.cs` | Entity in `<Product>.DataObjects` |
| `DataAccess/DAC/ApplicationDAC.cs` | `public partial class ApplicationDAC : DACDecorator`, namespace `<Product>.DataAccess` |
| `DataAccess/DAC/<Entity>DAC.cs` | Another `partial ApplicationDAC`, **not** a separate `<Entity>DAC` class |
| `DataAccess/Hydrators/<Entity>Hydrator.cs` | `public static partial class Hydrators`, namespace `<Product>.DataAccess`, **not** separate hydrator classes |
| `DataAccess/VendorSpecific/*` | Provider strategies in `<Product>.DataAccess` |
| NH manager/generator/registration files | Namespace `<Product>.DataAccess.NHibernate` |
| NH `Mappings/<Entity>Map.cs` | `<Entity>Map : ClassMap<TEntity>`, namespace `<Product>.DataAccess.NHibernate.Mappings` |

`DataAccess` depends on `DataObjects` and the required Framework data providers. `DataAccess.NHibernate` supplies maps/managers and references the entity and data-access layers plus the required NHibernate integration packages. Keep entity types independent of the persistence implementation.

## Factory and application DAC

Use `public static class <Product>Database` as the creation facade in `<Product>.DataAccess`. Its naming contract is:

- `ApplicationDAC NewDAC(DBMSType DBMS, string ConnectionString, ILogger? Logger = null)` selects the low-level provider, then delegates to the next overload.
- `ApplicationDAC NewDAC(IDAC DAC, ILogger? Logger = null)` wraps an existing provider and selects any vendor strategy.
- `IDatabaseManager NewDatabaseManager(DBMSType DBMS)` resolves the application's matching schema/database manager from its composition root.

Use `MSSQLDAC` or `SqliteDAC` for supported providers; do not add unsupported DBMS branches. Preserve an existing application's factory name when extending it. `ApplicationDAC` is an instance wrapper, not a static session accessor named `DAL`.

The core constructor is `internal ApplicationDAC(IDAC DecoratedDAC, IDBVendorSpecificImplementation VendorSpecificImplementation) : base(DecoratedDAC)` when a strategy is needed. Omit the strategy dependency if there are no provider-specific operations. Reuse [DACDecorator](../../../../src/Sphere10.Framework.Data/DAC/DACDecorator.cs), its protected `DecoratedDAC` field, and existing `IDAC` behavior.

Keep `IDBVendorSpecificImplementation` and `DBVendorSpecificImplementationBase` for provider-sensitive queries, maintenance, or optimizations. Concrete names are `MSSQLVendorSpecificImplementation` and, for genuinely optional behavior, `NoOpVendorSpecificImplementation`. A no-op must not hide an unsupported operation required for correctness. Follow the application's logger ownership; a facade overload must not silently discard a supplied logger.

## Schema managers, registration, and seed hooks

Use `<Product>DatabaseManagerMSSQL : NHDatabaseManagerBase` with a parameterless constructor chaining to `base(new MSSQLDatabaseManager())`; the SQLite counterpart chains to `base(new SqliteDatabaseManager())`. The standard overrides are `GetFluentConfig(string)` and `CreateDataGenerator(ISessionFactory, string, DatabaseGenerationDataPolicy)`. Override `SetCreateDatabaseConfiguration(string, string, Configuration)` or `OnDatabaseSchemasCreated(string)` only for necessary provider behavior. Normal database opening must not recreate the schema.

`ModuleConfiguration : ModuleConfigurationBase` overrides `RegisterComponents(IServiceCollection)` and registers managers using `AddNamedTransient<IDatabaseManager, TManager>(DBMSType.SQLServer.ToString())` or the corresponding provider key. The creation facade resolves the same keys. Use the target's existing composition mechanism when it differs.

For this generator variant, `PrimingDataGenerator : NHDataGeneratorBase` has constructor `(ISessionFactory SessionFactory, string DatabaseName) : base(SessionFactory)` and a `protected readonly string DatabaseName` field when the seed process needs it. Its `protected sealed override IEnumerable<object> CreateData()` concatenates required seed sequences and finishes with `CreateNonPrimingData()`.

Declare `protected virtual IEnumerable<object> CreateNonPrimingData()` returning an empty sequence in the priming generator. `DemoDataGenerator : PrimingDataGenerator` uses the same constructor and overrides **`CreateNonPrimingData()`**, preserving mandatory priming data. Merely creating a demo generator does not wire it into `CreateDataGenerator`: add the explicit `DemoData` policy branch only when the application supports it. Do not inherit historical unsupported-policy branches or destructive SQLite creation stubs as conventions.

For lookup tables, choose one model family. Older scaffolds used `Table<Enum> : TypeTable` and `Table<Enum>Map : ClassMap<Table<Enum>>` inside `Mappings/TypeTables`, with assigned IDs. Some also left unused internal `<Enum>Table` copies in `DataObjects/TypeTables`; do not generate both families. Prefer the current [TypeTable<TEnum>](../../../../src/Sphere10.Framework.Data.NHibernate/DataPatterns/TypeTable.cs) and [TypeTableMap<TEnum>](../../../../src/Sphere10.Framework.Data.NHibernate/DataPatterns/TypeTableMap.cs) when they fit the chosen model and target API. Keep persisted enum IDs stable.

## Context and transaction ownership

An application can expose a `BizLogicScope : SyncContextScope` in its processing/business layer, with `Current`, `DAC`, `CreateDAC()`, and optional `EnterDatabaseFreeScope()`. An optional `BizComponent : IBizComponent` can capture that context and expose `CustomDAC` plus `DAC => CustomDAC ?? Scope.DAC`. These are application scaffold types, not Framework classes. Add them only when the application uses ambient business context.

In this variant the business-scope constructor accepts `(DBMSType DBMS, string ConnectionString, ILogger? Logger = null)` and uses `ContextScopePolicy.MustBeRoot` with a context key based on its type. `Current` requires an active context; database access fails in a database-free context. `CreateDAC()` delegates to the product database factory. Preserve these semantics if adopting this scaffold rather than silently allowing nested business roots.

A business context supplies dependencies; it does **not** automatically open a database transaction. Declare the transaction at the coordinating operation: `DAC.BeginScope()`, then `BeginTransaction()`, perform all related work, and `Commit()`. Dispose every scope. Reads can use a connection scope without a transaction when the consistency requirement permits it.

[DACScope](../../../../src/Sphere10.Framework.Data/Scopes/DACScope.cs) shares a connection/transaction with matching nested scopes; `UseScopeOsmosis` affects sharing across DAC instances. Nested operations join via `BeginTransaction()`. A child `Commit()` does not commit the owning transaction, while explicit child `Rollback()` votes against it. Propagate failures to the owning operation or explicitly roll back before handling a child failure: current [TransactionalScopeBase](../../../../src/Sphere10.Framework/Transactions/TransactionalScopeBase.cs) does not automatically cast that rollback vote on child disposal.

Do not execute parallel commands on the same shared connection. Do not assume `DACScope` and an NHibernate `DataAccessScope` share one transaction merely because their connection strings match. Keep direct SQL changes and NHibernate tracked entities behind an explicit refresh/clear boundary if both operate on the same data.

## Queries, hydration, and writes

Expose typed `Find<Entities>`, `Get<Entity>ByID`, `Insert<Entity>`, and related operations on partial `ApplicationDAC`. Use `IDAC.Select`, `Insert`, `Update`, and `Delete` with `ColumnValue` collections. Centralize projected columns so SQL selection, hydration, and bulk tables agree on names, types, and nullability.

For specialized SQL, use `CreateSQLBuilder()` or `QuickString` with `SQLBuilderCommand.TableName`, `ColumnName`, and `Literal`. These render SQL; they are not parameter binding. Use bound parameters when the selected execution API supports them. Do not place caller-controlled text directly in raw `whereClause` or `orderByClause` fragments.

`Hydrators` exposes `Hydrate<Entity>(DataRow)`, an overload with a column prefix, and an overload hydrating an existing entity. Read values through `DataRow.Get<T>`. Preserve the distinction between ID-only relationship stubs and fully loaded entities. Batch-fetch optional relationships and assemble dictionaries keyed by entity ID instead of querying once per parent.

Make result cardinality part of the method contract: a getter requiring exactly one row reports `NoSingleRecordException`; a method supporting absence returns that explicitly. Materialize deferred results before leaving resources they depend on.

## Bulk operations and verification

For high-volume writes, `BatchInsertDAC.cs` extends partial `ApplicationDAC`. Materialize inputs once, build typed `DataTable` batches, insert parents before dependants, and propagate their IDs into foreign keys. Match database column types and nulls. Use `BulkInsertOptions.KeepIdentity` only when supplying IDs; select `MaintainForeignKeys` deliberately. Bound batches and put all dependent writes inside an explicit transaction.

Historical imports allocated IDs from the current maximum and used `KeepIdentity`. That requires exclusive/coordinated allocation; do not apply it to concurrent writers without an allocator or database-generated IDs. Keep any index/constraint suspension in the provider strategy with restoration and subsequent validation.

Use disposable test databases for changed mappings, queries, or writes. Verify representative null/relationship round trips, durable commit, failed/nested rollback, and bulk identity/foreign-key consistency when those paths change. Read final results through a fresh connection/session. A rollback-based `UnitTestScope` must dispose the DAC transaction/scope as well as rolling it back. Follow the target's NUnit `Assert.That` conventions.
