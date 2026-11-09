<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.NHibernate

`Sphere10.Framework.Data.NHibernate` adds NHibernate and FluentNHibernate helpers to [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md). Use it when an application maps its entities with FluentNHibernate and wants the shared database-manager contract to create its schema, seed data and open session factories.

The package supplies a database-manager base class, a data-generator base class, FluentNHibernate conventions, JSON column types, an enum type-table pattern, an auditing listener and SQL Server dialect and driver extensions. It has no session-factory builder of its own: configure NHibernate with FluentNHibernate's `Fluently.Configure()`.

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

`Customer` and `AppDataGenerator` are application types, which the entity, mapping and seed-data sections below define. The session, transaction, query and mapping APIs are NHibernate's and FluentNHibernate's own.

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

## 🗺️ Entities and mappings

FluentNHibernate maps the virtual properties of plain classes, which lets NHibernate create lazy-loading proxies. The quick start's `Customer` belongs to this model, and the remaining examples use it:

```csharp
using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;
using Sphere10.Framework.Data.NHibernate;

public enum OrderStatus {
	Pending = 1,
	Shipped = 2,
	Cancelled = 3
}

public class CustomerPreferences {
	public string Language { get; set; }
	public bool Newsletter { get; set; }
}

public class Customer {
	public virtual int ID { get; set; }
	public virtual string Name { get; set; }
	public virtual string Email { get; set; }
	public virtual CustomerPreferences Preferences { get; set; }
	public virtual DateTime CreatedOnUtc { get; set; }
	public virtual DateTime LastUpdatedOnUtc { get; set; }
	public virtual bool Active { get; set; }
	public virtual IList<CustomerOrder> Orders { get; set; } = new List<CustomerOrder>();
}

public class Product {
	public virtual int ID { get; set; }
	public virtual string Name { get; set; }
	public virtual decimal Price { get; set; }
}

public class CustomerOrder {
	public virtual int ID { get; set; }
	public virtual Customer Customer { get; set; }
	public virtual Product Product { get; set; }
	public virtual int Quantity { get; set; }
	public virtual OrderStatus Status { get; set; }
	public virtual DateTime OrderedOnUtc { get; set; }
}

public class CustomerMap : ClassMap<Customer> {
	public CustomerMap() {
		Id(x => x.ID).GeneratedBy.Native();
		Map(x => x.Name).Not.Nullable();
		Map(x => x.Email);
		Map(x => x.Preferences).CustomType<JsonColumnType<CustomerPreferences>>();
		Map(x => x.CreatedOnUtc);
		Map(x => x.LastUpdatedOnUtc);
		Map(x => x.Active);
		HasMany(x => x.Orders).Inverse().Cascade.AllDeleteOrphan();
	}
}

public class ProductMap : ClassMap<Product> {
	public ProductMap() {
		Id(x => x.ID).GeneratedBy.Native();
		Map(x => x.Name).Not.Nullable();
		Map(x => x.Price).Precision(10).Scale(2);
	}
}

public class CustomerOrderMap : ClassMap<CustomerOrder> {
	public CustomerOrderMap() {
		Id(x => x.ID).GeneratedBy.Native();
		References(x => x.Customer).Not.Nullable();
		References(x => x.Product).Not.Nullable();
		Map(x => x.Quantity);
		Map(x => x.Status);
		Map(x => x.OrderedOnUtc);
	}
}

// One row per OrderStatus value, in a table named OrderStatus.
public class OrderStatusMap : TypeTableMap<OrderStatus> {
}
```

With the quick start's conventions, `ForeignKeyConventions` names the reference columns `CustomerID` and `ProductID`, and `EnumConvention` stores `Status` as its number. `AddBusinessObjectAuditor()` fills `CreatedOnUtc`, `LastUpdatedOnUtc` and `Active`, because `Customer` has properties with those names. The order entity is called `CustomerOrder` because `ORDER` is a reserved word in SQL.

`JsonColumnType<T>` stores `Preferences` as JSON text. To encrypt such a column, derive a column type from `EncryptedJsonColumnTypeBase<T>` and map the property with `CustomType<CustomerNotesColumnType>()`:

```csharp
public class CustomerNotes {
	public string Text { get; set; }
}

// NHibernate constructs custom types itself, so the password comes from a parameterless constructor.
public class CustomerNotesColumnType : EncryptedJsonColumnTypeBase<CustomerNotes> {
	public CustomerNotesColumnType()
		: base(Environment.GetEnvironmentVariable("APP_NOTES_KEY")) {
	}
}
```

## 🌱 Seed data

`CreateApplicationDatabase` calls `CreateDataGenerator` for `PrimingData` and `DemoData`. The quick start's `AppDataGenerator` writes one type-table row per `OrderStatus` value and two products:

```csharp
public class AppDataGenerator : NHDataGeneratorBase {

	public AppDataGenerator(ISessionFactory sessionFactory)
		: base(sessionFactory) {
	}

	protected override IEnumerable<object> CreateData() {
		foreach (var status in CreateTypeTable<OrderStatus>())
			yield return status;

		yield return new Product { Name = "Laptop", Price = 999.99m };
		yield return new Product { Name = "Mouse", Price = 29.99m };
	}
}
```

`Populate()` saves the objects with `SaveOrUpdate` in one session and transaction, in the order that `CreateData()` returns them. Use the `policy` argument of `CreateDataGenerator` to choose between priming data and demo data.

## 🔌 Other databases

The manager decorates any provider `IDatabaseManager`. For SQLite, decorate `SqliteDatabaseManager` from [Sphere10.Framework.Data.Sqlite](../Sphere10.Framework.Data.Sqlite/README.md) and configure FluentNHibernate's `SQLiteConfiguration`:

```csharp
using System.IO;

public class SqliteAppDatabaseManager : NHDatabaseManagerBase {

	public SqliteAppDatabaseManager()
		: base(new SqliteDatabaseManager()) {
	}

	protected override FluentConfiguration GetFluentConfig(string connectionString)
		=> Fluently
			.Configure()
			.Database(SQLiteConfiguration.Standard.ConnectionString(connectionString))
			.Mappings(m => m.FluentMappings
				.AddFromAssemblyOf<SqliteAppDatabaseManager>()
				.Conventions.Add(new CoreConventions(), new ForeignKeyConventions(), new EnumConvention()))
			.AddBusinessObjectAuditor();

	protected override IDataGenerator CreateDataGenerator(ISessionFactory sessionFactory, string databaseName, DatabaseGenerationDataPolicy policy)
		=> new AppDataGenerator(sessionFactory);
}
```

Create, seed and open the database file through it:

```csharp
var manager = new SqliteAppDatabaseManager();
var connectionString = manager.GenerateConnectionString(null, Path.Combine(Path.GetTempPath(), "app.sqlite"), null, null, null);
if (!manager.DatabaseExists(connectionString))
	manager.CreateEmptyDatabase(connectionString);
manager.CreateApplicationDatabase(connectionString, DatabaseGenerationDataPolicy.DemoData, "app");

using var sessionFactory = manager.OpenDatabase(connectionString);
```

For SQL Server columns of unsigned types, select the extended dialect and its client driver:

```csharp
var configuration = Fluently
	.Configure()
	.Database(MsSqlConfiguration.MsSql2008
		.ConnectionString(connectionString)
		.Dialect<ExtendedMssqlDialect>()
		.Driver<ExtendedSql2008ClientDriver>());
```

## 🔎 LINQ and HQL queries

Open sessions from the factory that `OpenDatabase` returns. `Query<T>()` is NHibernate's LINQ provider, and `Fetch` comes from `NHibernate.Linq`:

```csharp
using System.Linq;
using NHibernate.Linq;

using (var session = sessionFactory.OpenSession()) {
	var customersNamedA = session.Query<Customer>()
		.Where(c => c.Name.StartsWith("A"))
		.OrderBy(c => c.Name)
		.ToList();

	var since = DateTime.UtcNow.AddDays(-7);
	var recentOrders = session.Query<CustomerOrder>()
		.Where(o => o.OrderedOnUtc > since)
		.OrderByDescending(o => o.OrderedOnUtc)
		.Take(10)
		.Fetch(o => o.Product)
		.ToList();

	var expensiveProducts = session.Query<Product>()
		.Where(p => p.Price > 100m)
		.Select(p => new { p.Name, p.Price })
		.ToList();

	var averagePrice = session.Query<Product>().Average(p => p.Price);
	var maxPrice = session.Query<Product>().Max(p => p.Price);

	var totalsByCustomer = session.Query<CustomerOrder>()
		.GroupBy(o => o.Customer.Name)
		.Select(g => new { Customer = g.Key, OrderCount = g.Count(), Total = g.Sum(o => o.Quantity * o.Product.Price) })
		.ToList();

	var customersLikeA = session.CreateQuery("from Customer c where c.Name like :name order by c.Name")
		.SetParameter("name", "A%")
		.List<Customer>();

	var customerCount = session.CreateQuery("select count(c) from Customer c")
		.UniqueResult<long>();
}
```

HQL names entities and properties rather than tables and columns, and its `count` returns a `long`. `Fetch` loads each order's product in the same query, which avoids one extra query per order when code reads `order.Product`. Keep sessions short-lived, and dispose each one when its work completes.

## 🔗 Relationships and cascading

The first session saves a customer with two orders, and the second removes one of them:

```csharp
int customerID;
using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction()) {
	var laptop = new Product { Name = "Laptop", Price = 999.99m };
	var mouse = new Product { Name = "Mouse", Price = 29.99m };
	session.Save(laptop);
	session.Save(mouse);

	var customer = new Customer { Name = "Bob", Email = "bob@example.com" };
	customer.Orders.Add(new CustomerOrder { Customer = customer, Product = laptop, Quantity = 1, Status = OrderStatus.Pending, OrderedOnUtc = DateTime.UtcNow });
	customer.Orders.Add(new CustomerOrder { Customer = customer, Product = mouse, Quantity = 2, Status = OrderStatus.Pending, OrderedOnUtc = DateTime.UtcNow });

	// The cascade on Orders saves both orders with the customer.
	customerID = (int)session.Save(customer);
	transaction.Commit();
}

using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction()) {
	var customer = session.Get<Customer>(customerID);

	// Orders loads on first access, while the session is open.
	var firstOrder = customer.Orders.First();

	// Removing an order from the collection deletes the orphaned row on commit.
	customer.Orders.Remove(firstOrder);
	transaction.Commit();
}
```

`Cascade.AllDeleteOrphan()` saves, updates and deletes orders with their customer. `Inverse()` leaves the `CustomerID` column to each order's `Customer` reference, so set both sides when adding an order. Lazy collections and proxies load only while their session is open.

## 📦 Batches and bulk statements

The example inserts many entities through one session, runs HQL bulk statements and inserts through a stateless session:

```csharp
using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction()) {
	for (var i = 1; i <= 1000; i++) {
		session.Save(new Customer { Name = $"Customer {i}", Email = $"customer{i}@example.com" });

		// Write pending inserts and empty the session every 100 entities to bound its memory.
		if (i % 100 == 0) {
			session.Flush();
			session.Clear();
		}
	}
	transaction.Commit();
}

using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction()) {
	var deactivated = session.CreateQuery("update Customer c set c.Active = :active where c.Name like :pattern")
		.SetParameter("active", false)
		.SetParameter("pattern", "Customer 1%")
		.ExecuteUpdate();

	var deleted = session.CreateQuery("delete from CustomerOrder o where o.Status = :status")
		.SetParameter("status", OrderStatus.Cancelled)
		.ExecuteUpdate();

	Console.WriteLine($"Deactivated {deactivated} customers and deleted {deleted} orders");
	transaction.Commit();
}

using (var session = sessionFactory.OpenStatelessSession())
using (var transaction = session.BeginTransaction()) {
	foreach (var name in new[] { "Keyboard", "Monitor", "Webcam" })
		session.Insert(new Product { Name = name, Price = 49m });
	transaction.Commit();
}
```

`Flush` writes pending changes, and `Clear` detaches the saved entities so that the session stops growing. HQL `update` and `delete` statements run in the database and bypass the session's cache, cascades and listeners. A stateless session has no cache, change tracking, cascades or event listeners, so the auditor does not run for its inserts.

## 🔄 Share a session with DataAccessScope

`DataAccessScope` opens one session for the outermost scope of a database. Nested scopes for the same `DBMSType` and connection string reuse that session and its transaction:

```csharp
using (var scope = new DataAccessScope(manager, DBMSType.SQLServer, connectionString)) {
	scope.BeginTransaction();
	var customer = scope.Session.Query<Customer>().Single(c => c.Email == "alice@example.com");
	Rename(manager, connectionString, customer.ID, "Alice Smith");
	scope.Commit();
}

static void Rename(INHDatabaseManager manager, string connectionString, int customerID, string name) {
	// A nested scope for the same database shares the outer scope's session and transaction.
	using var scope = new DataAccessScope(manager, DBMSType.SQLServer, connectionString);
	scope.Session.Get<Customer>(customerID).Name = name;
}
```

The outermost scope flushes, closes and disposes the session when it ends. Each outermost scope calls `OpenDatabase`, which builds a new session factory, so open outermost scopes for units of work rather than for single queries.

## 📎 Attach a detached entity

`Attach` adds an entity that another session loaded, such as one kept in a cache, without reading it again. The session takes the entity's current values as its database state, so only changes made after `Attach` are written:

```csharp
using (var session = sessionFactory.OpenSession())
using (var transaction = session.BeginTransaction()) {
	session.Attach(cachedCustomer);
	cachedCustomer.Name = "Alice Smith";
	transaction.Commit();
}
```

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
