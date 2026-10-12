# NHibernate runtime scaffold contract

Use this contract for a new NHibernate CRUD application. Names are intentional: replace `Product` with the application's product prefix and `Entity`/`Component` with domain type names, while preserving the remaining names. Retain existing names when extending an established application. Classes below are application-owned unless a framework/library base is identified.

## Project and namespace layout

```text
Product.DataObjects/
  BusinessEntity.cs
  Entity.cs
  Component.cs                         when owned value objects exist
  KnownLookupTypes.cs                  when fixed lookup values exist
Product.DataAccessLayer/
  DatabaseConnectionSetting.cs
  DatabaseManager.cs
  DataScope.cs
  Mappings/
    BusinessEntityMap.cs
    BusinessEntitySubclassMap.cs       only for mapped inheritance
    EntityMap.cs
    ComponentMap.cs
  DatabaseManagers/
    ProductDatabaseManagerBase.cs
    ProductDatabaseManagerMSSQL.cs     choose supported providers
    ProductDatabaseManagerSqlite.cs
    ProductDatabaseManagerFirebird.cs
  DataGenerators/
    PrimingDataGenerator.cs
    ExtendedPrimingDataGenerator.cs    only for a reusable extra reference tier
    DemoDataGenerator.cs               when demo data is requested
    DevDataGenerator.cs                when development fixtures are requested
  Conventions/CoreConventions.cs       only for required application conventions
  NHibernateListener.cs               when shared audit fields are maintained
  Interceptors/LoggingInterceptor.cs   when SQL diagnostics are needed
  SqlLog.cs                            when a separate SQL sink is needed
Product.BusinessLogic/
  BusinessEntityDataSource.cs          when CRUD data-source integration is needed
  EntityDataSource.cs
  DomainManager.cs                     name for the actual business capability
```

In `KnownLookupTypes.cs`, substitute the lookup name: a `TicketStatus` lookup can use `KnownTicketStatusTypes.cs` declaring `KnownTicketStatusTypes`, with explicit stable enum values. Preserve an existing application's `Known*Types` naming when extending it.

Dependencies flow from BusinessLogic to DataAccessLayer/DataObjects, and from DataAccessLayer to DataObjects plus Framework/NHibernate/provider libraries. DataObjects must not reference the DAL or UI.

Use namespaces `Product.DataObjects`, `Product.DataAccessLayer`, and `Product.BusinessLogic`. `DatabaseManagers`, `DataGenerators`, and `Mappings` are organizational folders, not a requirement to add a namespace segment. The runtime reference uses `.DataAccessLayer` for shared infrastructure and mixed map namespaces; normalize new maps consistently to `.DataAccessLayer` rather than reproducing that inconsistency. The SQL profile has a different established `.DataAccess.NHibernate.Mappings` namespace.

## Data object and mapping surface

`public abstract class BusinessEntity` has virtual properties:

| Member | Runtime-profile baseline |
|---|---|
| `int ID` | `-1` before persistence; generated or explicitly assigned according to the map. |
| `bool Active` | Defaults true; shared query/soft-delete behavior. |
| `long RowVersion` | Defaults 0; application audit counter unless a real NH version contract is added. |
| `DateTime LastUpdatedOn` | Defaults to current UTC time; refreshed by the selected audit mechanism. |

A domain entity derives `BusinessEntity`, initializes its collections, and uses `Add<Child>(Child)` plus optional `Append<Children>` convenience setters to maintain both ends. Preserve a `CreateNew()` factory only when it supplies meaningful default initialization. Component/value objects need no artificial identity or BusinessEntity inheritance.

The mapping base contract is:

```csharp
public abstract class BusinessEntityMap<T> : ClassMap<T>
	where T : BusinessEntity {
	protected BusinessEntityMap(bool ExplicitID = false) {
		var PrimaryKey = Id(Entity => Entity.ID).Column("ID");
		if (ExplicitID)
			PrimaryKey.GeneratedBy.Assigned();
		else
			PrimaryKey.GeneratedBy.Native("SEQ_" + typeof(T).Name);

		MapEntity();
		Map(Entity => Entity.Active);
		Map(Entity => Entity.RowVersion);
		Map(Entity => Entity.LastUpdatedOn);
	}

	public abstract void MapEntity();
}
```

This is a contract fragment; include `using FluentNHibernate.Mapping;`, the data-object namespace, file-scoped namespace, and the repository source header when implementing it. `MapEntity()` is called from the base constructor, so overrides must not depend on derived-constructor initialization.

`EntityMap : BusinessEntityMap<Entity>` implements `public override void MapEntity()` to set the table and domain properties/relationships. Use `base(true)` for fixed-ID lookup entities. Table prefixes vary by application; choose one explicitly and use it consistently. `BusinessEntitySubclassMap<T> : SubclassMap<T>` keeps the same `MapEntity()` hook and uses `KeyColumn("ID")`; map shared fields in the correct inheritance table exactly once. `ComponentMap<Component>` maps embedded properties for `Component(...)` use.

If entity equality is implemented, account for NH proxies and transient identities; do not copy equality based on type-name strings or hash values that become invalid while an entity is held in a hash collection.

`NHibernateListener : IPreInsertEventListener, IPreUpdateEventListener` uses `Instance`, `OnPreInsert`, `OnPreUpdate`, and the installed version's async members. The reference initializes/increments `RowVersion` and sets UTC `LastUpdatedOn`; when changing mapped values in a pre-event, update both the entity and NH's state array by property name. Decide whether insertion may force `Active = true`; do not silently revive intentionally inactive imports. Raw DAC writes bypass these listeners.

## Application facade and unit of work

`DatabaseConnectionSetting : SettingsObject` exposes `DBMSType DBMSType` and `string ConnectionString`. Keep actual credentials in the target application's approved settings/secret mechanism.

`DatabaseManager` retains these static member names:

| Member | Contract |
|---|---|
| `ISessionFactory ApplicationSessionFactory` | Initialized application factory; fail clearly if accessed before successful initialization. |
| `bool HasRegisteredDatabase` | Whether the application has its database connection settings. |
| `bool RegisterDatabase(DBMSType, string ConnectionString, out string ErrorMessage)` | Register the selected database and initialize it; preserve the app's settings behavior. |
| `bool InitializeApplicationDatabase(out string ErrorMessage)` | Open the registered DB, publish its factory only after success. |
| `bool CreateDatabase(DBMSType, string ConnectionString, bool IncludeDemoData, ..., out string ErrorMessage)` | Create schema plus chosen seed policy; `...` represents only domain-required seed inputs, such as an administrator hash. |
| `CreateDatabaseManager(DBMSType)` | Private provider selector returning `ProductDatabaseManagerBase`. |

Dispose the previously owned factory when safely replacing it and the current factory at application shutdown; active units of work must finish before replacement. Failed initialization must not leave a new unusable factory published.

The application `DataScope` owns a session and transaction and retains:

```text
DataScope()
DataScope(ISession Session)
DataScope(ISession Session, ITransaction Transaction)
ISession Session { get; }
ITransaction Transaction { get; }
static bool CurrentScopeExists { get; }
static DataScope Current { get; }
void Commit()
void Rollback()
void Dispose()
```

The default constructor opens a session from `DatabaseManager.ApplicationSessionFactory`; the session constructor starts a transaction. The two-resource overload accepts ownership of supplied resources unless an existing application explicitly defines otherwise. Default `FlushMode` is Commit. Dispose rolls back any still-active uncommitted transaction, disposes it and the session, and clears/restores ambient scope state even if cleanup fails. Follow available disposable-scope helpers for guaranteed cleanup.

The inspected runtime implementation is synchronous, thread-local and rejects nested scopes. Preserve that contract for existing callers. A new application requiring async or nesting must implement those semantics deliberately behind the same `DataScope` surface and test them; do not carry a thread-local scope across `await`. Validate ambient-state conflicts before acquiring resources, and clean up partially acquired resources on construction failure. Do not copy global named-TLS-slot deletion as cleanup.

## Product and provider managers

`ProductDatabaseManagerBase : NHDatabaseManagerBase` has:

```text
protected ProductDatabaseManagerBase(IDatabaseManager InternalDatabaseManager)
    : base(InternalDatabaseManager)
protected FluentConfiguration ApplyCommonConfiguration(FluentConfiguration Configuration)
protected override IDataGenerator CreateDataGenerator(
    ISessionFactory SessionFactory, string DatabaseName, DatabaseGenerationDataPolicy Policy)
```

It holds only domain-required seed options and centralizes map assembly scanning, chosen conventions, optional `NHibernateListener.Instance`, `LoggingInterceptor`, and quoting. Use `LoggingInterceptor : EmptyInterceptor` with `OnPrepareStatement(SqlString)` returning the SQL unchanged, and `SqlLog` as the optional framework-logger facade. Do not enable verbose SQL logging of sensitive literals by default.

Each `ProductDatabaseManagerMSSQL` / `ProductDatabaseManagerSqlite` / supported additional provider derives from the product base, has a parameterless constructor supplying `new MSSQLDatabaseManager()` / `new SqliteDatabaseManager()` / the matching real physical manager, and overrides `GetFluentConfig(string ConnectionString)`. It calls `ApplyCommonConfiguration` around the provider-specific Fluent configuration. Historical provider stubs, dialects, and deletion-only SQLite schema hooks are not a functional provider implementation.

## Generator template methods

Retain this inheritance and hook structure, pruning optional tiers when unused:

```text
NHDataGeneratorBase                          Framework owns Populate()
  PrimingDataGenerator
    sealed override CreateData()             required generators + CreateAdditionalData()
    virtual Create<PluralRecords>()          required records in dependency order
    virtual CreateAdditionalData()           empty default extension
      ExtendedPrimingDataGenerator
        override CreateAdditionalData()      base + extra catalog + CreateExtendedAdditionalData()
        virtual CreateExtendedAdditionalData()
          DemoDataGenerator
            override CreateExtendedAdditionalData()
          DevDataGenerator
            override CreateExtendedAdditionalData()
```

The signatures are `protected ... IEnumerable<object>` for `CreateData`, `CreateAdditionalData`, and `CreateExtendedAdditionalData`; domain-specific `Create<PluralRecords>()` may return `IEnumerable<ConcreteEntity>`. Constructors accept `ISessionFactory` first and pass it to the base, then preserve any seed inputs along the chain. Leaf overrides append their records to the base hook rather than replacing required data.

When no ExtendedPriming tier is needed, `DemoDataGenerator : PrimingDataGenerator` overrides `CreateAdditionalData()`. Do not add empty extra tiers solely because the complete diagram includes them. Conversely, when the additional catalog is required by both demo and development records, share it in `ExtendedPrimingDataGenerator` rather than duplicating it.

Order generation so referenced objects exist before dependants are persisted; retain shared object instances in protected fields/collections when later steps need them. Do not enumerate a lazy generation pipeline twice. Seed fixed lookup IDs explicitly. Verify ownership/cascade mapping for nested graphs. The provider policy must construct the selected leaf so base priming is included exactly once.

## CRUD data-source hooks

When data-source consumers exist, `BusinessEntityDataSource<TEntity> : SyncBatchDataSourceBase<TEntity>` (constraint `where TEntity : BusinessEntity, new()`) keeps `NewItem`, `MainQuery`, `ApplySearchFilter`, `ApplySort`, `ApplyPaging`, `Prefetch`, `ValidateItem`, `SaveOrUpdate`, and `DeleteItem`. Concrete `EntityDataSource` classes override domain queries/validation and live in `.BusinessLogic`.

Use the installed Framework's actual Range overrides rather than reproducing stale signatures. `MainQuery` operates on `DataScope.Current.Session`; load required results before scope disposal. `SaveOrUpdate` uses the merged managed instance for detached inputs. `DeleteItem` uses `Active = false` when soft delete is the contract. Retain input validation and sorting hooks, but fix unstable paging, negative empty pages, and per-item transactions where the requested behavior needs stable pages or an atomic batch.
