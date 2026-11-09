<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.NHibernate

`Sphere10.Framework.Data.NHibernate` adds NHibernate and FluentNHibernate helpers to [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md). Use it when an application maps its entities with FluentNHibernate and wants the shared database-manager contract to create its schema, seed data and open session factories.

The package supplies a database-manager base class, a data-generator base class, FluentNHibernate conventions, JSON column types, an enum type-table pattern, an auditing listener and SQL Server dialect and driver extensions. It has no session-factory builder of its own: configure NHibernate with FluentNHibernate's `Fluently.Configure()`.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Data.NHibernate` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|
| [ClientDrivers](ClientDrivers/README.md) | Provides an NHibernate SQL Server client driver that widens unsigned and byte parameter types to types SQL Server can store. | [source](ClientDrivers/) |
| [Conventions](Conventions/README.md) | Provides FluentNHibernate conventions for table naming, foreign-key naming, enum mapping and string and binary column sizes. | [source](Conventions/) |
| [DataPatterns](DataPatterns/README.md) | Provides the enum type-table pattern, an entity and FluentNHibernate map that store one row per enum value. | [source](DataPatterns/) |
| [Database](Database/README.md) | Provides the NHibernate database-manager contract and base classes that create a schema, seed it and open session factories. | [source](Database/) |
| [Dialects](Dialects/README.md) | Provides an NHibernate SQL Server dialect that maps unsigned and byte CLR types to wider signed column types. | [source](Dialects/) |
| [Extensions](Extensions/README.md) | Provides extension methods that add the auditing listener to a FluentNHibernate configuration and attach detached entities to a session. | [source](Extensions/) |
| [Json](Json/README.md) | Provides NHibernate `IUserType` implementations that store an object as a JSON string column, optionally encrypted. | [source](Json/) |
| [Listeners](Listeners/README.md) | Holds the NHibernate event listener behind the auditing extension and a checksum helper for business-object buffers. | [source](Listeners/) |
| [Scopes](Scopes/README.md) | Provides `DataAccessScope`, a nestable transactional scope that shares one NHibernate session and transaction across nested scopes. | [source](Scopes/) |

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.NHibernate`](NHibernate.cs) | tools | Holds `NVarCharMaxValue` (4001), a string length that NHibernate maps to an unbounded string column, used by the JSON column types |
<!-- domains:end -->

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.NHibernate
```

The package depends on `NHibernate`, `FluentNHibernate`, `Sphere10.Framework.Data`, `Sphere10.Framework.Data.MSSQL` and `Sphere10.Framework`. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.NHibernate/Sphere10.Framework.Data.NHibernate.csproj` with a `ProjectReference`.

## ⚡ Create a database and open sessions

Derive a manager from `NHDatabaseManagerBase`. It decorates a provider's `IDatabaseManager`, which creates, tests and drops the empty database, and adds the NHibernate configuration that creates the schema:

```csharp
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using Sphere10.Framework.Data;
using Sphere10.Framework.Data.NHibernate;

public class AppDatabaseManager : NHDatabaseManagerBase {

	public AppDatabaseManager()
		: base(new MSSQLDatabaseManager()) {
	}

	protected override FluentConfiguration GetFluentConfig(string connectionString)
		=> Fluently
			.Configure()
			.Database(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString))
			.Mappings(m => m.FluentMappings
				.AddFromAssemblyOf<AppDatabaseManager>()
				.Conventions.Add(new CoreConventions(), new ForeignKeyConventions(), new EnumConvention(), new StringColumnLengthConvention()))
			.AddBusinessObjectAuditor();

	protected override IDataGenerator CreateDataGenerator(ISessionFactory sessionFactory, string databaseName, DatabaseGenerationDataPolicy policy)
		=> new AppDataGenerator(sessionFactory);
}
```

Use it through the shared contract:

```csharp
var password = Environment.GetEnvironmentVariable("APPDB_PASSWORD");
var manager = new AppDatabaseManager();
var connectionString = manager.GenerateConnectionString("localhost", "AppDb", "appuser", password, null);
manager.CreateEmptyDatabase(connectionString);
manager.CreateApplicationDatabase(connectionString, DatabaseGenerationDataPolicy.PrimingData, "AppDb");

using var sessionFactory = manager.OpenDatabase(connectionString);
using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction()) {
	session.Save(new Customer { Name = "Alice" });
	transaction.Commit();
}
```

`Customer` and `AppDataGenerator` are application types. The session, transaction, query and mapping APIs are NHibernate's and FluentNHibernate's own.

## 🧩 Public API

The types are in `Sphere10.Framework.Data.NHibernate`, except `Tools.NHibernate`, which is in the global `Tools` namespace.

| API | Purpose |
|---|---|
| `INHDatabaseManager` | An `IDatabaseManager` that also opens databases as `ISessionFactory` instances. |
| `NHDatabaseManagerBase` | A `DatabaseManagerDecorator` implementing `INHDatabaseManager`. Subclasses supply `GetFluentConfig(connectionString)` and `CreateDataGenerator(...)`. |
| `NHDataGeneratorBase` | An `IDataGenerator` whose `Populate()` saves the objects from `CreateData()` with `SaveOrUpdate` in one session and transaction. `CreateTypeTable<TEnum>()` and `CreateTypeTable(enumType)` create one type-table row per enum value. |
| `TypeTable`, `TypeTable<TEnum>`, `TypeTableMap<TEnum>` | Entity and FluentNHibernate map for a lookup table of an enum's values, with an assigned `ID`, a unique `Name` and an optional `Description` from the enum value's description. The table is named after the enum unless a subclass overrides `TableName`, and `UniqueName` can turn off the unique constraint. |
| `JsonColumnType<T>` | An `IUserType` that stores an object as a JSON string through `Sphere10FrameworkJsonSerializer`. An empty or blank value reads as a new `T`. |
| `EncryptedJsonColumnTypeBase<T>` | The same column type with `EncryptedJsonSerializer`. Derive a type with a parameterless constructor that passes the password, because NHibernate constructs custom types itself. |
| `JsonColumnTypeBase<T>` | The base for JSON column types that use another `IJsonSerializer`. |
| `FluentConfiguration.AddBusinessObjectAuditor()` | Adds pre-insert and pre-update listeners. On insert they set `CreatedOnUtc` and `LastUpdatedOnUtc` to the current UTC time and `Active` to `true`, and on update they set `LastUpdatedOnUtc`. Each property is set only when the entity has a property with that name. |
| `ISession.Attach(entity)` | Adds a detached entity to the session's persistence context as loaded, without reading it from the database. The `mode` argument is ignored. |
| `ExtendedMssqlDialect`, `ExtendedSql2008ClientDriver` | SQL Server 2008 dialect and driver that store `byte`, `ushort`, `uint` and `ulong` values as `SMALLINT`, `INT`, `BIGINT` and `DECIMAL(28)`. |
| `Tools.NHibernate.NVarCharMaxValue` | `4001`, a string length that NHibernate maps to an unbounded string column such as `NVARCHAR(MAX)`. The JSON column types use it. |

## 🗂️ Schema creation and data policies

`CreateApplicationDatabase(connectionString, dataPolicy, databaseName)` builds a session factory from `GetFluentConfig(connectionString)`. While it builds, `SetCreateDatabaseConfiguration` runs NHibernate's `SchemaExport.Create`, which drops and then creates the mapped tables. Override `SetCreateDatabaseConfiguration` to change that step. The database must already exist, so call `CreateEmptyDatabase` first.

After the schema exists, the manager calls its `OnDatabaseSchemasCreated` hook. `DatabaseGenerationDataPolicy.NoData` then adds no rows, while `PrimingData` and `DemoData` call `CreateDataGenerator` and run the returned generator's `Populate()`. The session factory is disposed when the method returns, and `OpenDatabase(connectionString)` builds a new one from the same configuration.

`GenerateConnectionString`, `DatabaseExists`, `CreateEmptyDatabase` and `DropDatabase` pass through to the decorated provider manager, such as `MSSQLDatabaseManager` or `SqliteDatabaseManager`.

## 🔧 FluentNHibernate conventions

Add the conventions that suit the application's schema to `FluentMappings.Conventions`:

| Convention | Effect |
|---|---|
| `CoreConventions` | Names a many-to-many table after the owning entity and the plural of the other entity, such as `CustomerOrders`. |
| `ForeignKeyConventions` | Names a foreign-key column after its property or type with an `ID` suffix, such as `CustomerID`. |
| `EnumConvention` | Maps enum and nullable enum properties through their enum type, so NHibernate stores the numeric value. |
| `StringColumnLengthConvention` | Gives string properties without an explicit length a length of 4000. |
| `AnsiStringConvention` | Maps every string property as `AnsiString`, which is a non-Unicode column. |
| `BinaryColumnLengthConvention` | Maps `byte[]` properties as SQL Server `varbinary(MAX)`. |

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, database managers and the `IDataGenerator` contract.
- [Sphere10.Framework.Data.MSSQL](../Sphere10.Framework.Data.MSSQL/README.md) and [Sphere10.Framework.Data.Sqlite](../Sphere10.Framework.Data.Sqlite/README.md) — provider database managers to decorate.
- [NHDatabaseManagerBase](Database/NHDatabaseManagerBase.cs), [NHDataGeneratorBase](Database/NHDataGeneratorBase.cs), [conventions](Conventions), [JSON column types](Json) and [the auditing listener](Listeners/BusinessObjectAuditor.cs) — implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.NHibernate/Sphere10.Framework.Data.NHibernate.csproj -c Release
```

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
