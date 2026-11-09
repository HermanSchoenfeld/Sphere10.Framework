<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.Firebird

`Sphere10.Framework.Data.Firebird` connects the shared `IDAC` data-access abstraction of [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) to Firebird through FirebirdSql.Data.FirebirdClient. Use it when an application built on the shared DAC, scope and SQL-builder APIs works with a Firebird server or an embedded Firebird database.

The package supplies `FirebirdDAC` and `FirebirdCorrectingReader`, and SQL text comes from `FirebirdSQLBuilder` in the shared package. Unlike the other providers, it has no `Tools.Firebird` helper class and no database manager.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.Firebird
```

The package depends on `FirebirdSql.Data.FirebirdClient`, `Sphere10.Framework.Data` and `Sphere10.Framework`. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.Firebird/Sphere10.Framework.Data.Firebird.csproj` with a `ProjectReference`.

The examples need an existing database and credentials. The FirebirdClient package contains no native libraries, so an embedded database also needs Firebird's native client library on the machine.

## ⚡ Open a scope and query

Set `FIREBIRD_CONNECTION_STRING` to your application's connection string. The remaining examples reuse `dac` and these imports.

```csharp
using System;
using System.Data;
using System.IO;
using FirebirdSql.Data.FirebirdClient;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var connectionString = Environment.GetEnvironmentVariable("FIREBIRD_CONNECTION_STRING");
Guard.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));
var dac = new FirebirdDAC(connectionString);

using (dac.EnterScope()) {
	var engineVersion = dac.ExecuteScalar<string>("SELECT rdb$get_context('SYSTEM', 'ENGINE_VERSION') FROM rdb$database");
	Console.WriteLine(engineVersion);
}
```

Constructing a DAC only stores its configuration. `EnterScope()` opens the connection by default, and the outermost scope owns that connection until it is disposed. The DAC itself is not disposable.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Data.Firebird` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`FirebirdCorrectingReader`](FirebirdCorrectingReader.cs) | class | `DataReaderDecorator` that reorders the bytes of `Guid` values read from Firebird into RFC 4122 order, with a static `CorrectGuid` for single values. |
| [`FirebirdDAC`](FirebirdDAC.cs) | class | `DACBase` for Firebird server and embedded databases that reports `Firebird` or `FirebirdFile`, corrects `Guid` values and runs `CREATE` statements in a transaction before the other statements of a batch. |
<!-- domains:end -->

## 🧩 Public API

`FirebirdCorrectingReader` is in `Sphere10.Framework.Data.Firebird`, and the other types are in `Sphere10.Framework.Data`.

| API | Purpose |
|---|---|
| `FirebirdDAC(connectionString, logger)` | The provider's `DACBase` implementation of `IDAC`. |
| `DBMSType` | `FirebirdFile` when the connection string's server type is `FbServerType.Embedded`, and `Firebird` otherwise. |
| `EnterScope()` | Returns a `DACScope`. The outermost scope owns the connection, and nested scopes share it. Call `BeginTransaction` on the scope for a local transaction. |
| `ExecuteScalar<T>(sql)`, `ExecuteQuery(sql)` | Return a converted scalar value or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)`, `ExecuteReader(sql)` | Execute a statement, or return a reader to consume inside an outer scope. |
| `ExecuteBatch(builder)` | Runs the builder's `CREATE` statements in one transaction and its other DDL and DML statements in a second, one statement at a time. |
| `Insert`, `Update`, `Delete` | Build SQL with `FirebirdSQLBuilder` and return the affected-row count. |
| `CreateConnection()`, `CreateOpenConnection()` | Create a caller-owned `FbConnection`, closed or opened. |
| `CreateSQLBuilder()` | Returns a new `FirebirdSQLBuilder` as `ISQLBuilder`. |
| `GetSchema()` | Reads table, column, key and trigger metadata, as described below. |
| `FirebirdCorrectingReader` | A reader decorator that converts `Guid` values, as described under bound parameters. |
| `DACFactory.CreateDAC(DBMSType.Firebird, connectionString)` | Creates a `FirebirdDAC` by type name, so that an application can choose the provider at run time. `DBMSType.FirebirdFile` creates the same type. |

## 🔧 Connection configuration

The package has no `Tools.Firebird` helper, so build connection strings with FirebirdClient's `FbConnectionStringBuilder`:

```csharp
var serverConnectionString = new FbConnectionStringBuilder {
	DataSource = "localhost",
	Database = "/data/appdb.fdb",
	UserID = "appuser",
	Password = Environment.GetEnvironmentVariable("FIREBIRD_PASSWORD"),
	ServerType = FbServerType.Default
}.ToString();
var serverDac = new FirebirdDAC(serverConnectionString);

// An embedded database file, opened through Firebird's native client library.
var embeddedConnectionString = new FbConnectionStringBuilder {
	Database = Path.Combine(AppContext.BaseDirectory, "data", "appdb.fdb"),
	UserID = "SYSDBA",
	ServerType = FbServerType.Embedded,
	ClientLibrary = "fbclient.dll"
}.ToString();
var embeddedDac = new FirebirdDAC(embeddedConnectionString);
Console.WriteLine(embeddedDac.DBMSType); // FirebirdFile

// The server connection with pool limits.
var pooledConnectionString = new FbConnectionStringBuilder(serverConnectionString) {
	Pooling = true,
	MinPoolSize = 5,
	MaxPoolSize = 20
}.ToString();
var pooledDac = new FirebirdDAC(pooledConnectionString);
```

| Property | Use |
|---|---|
| `DataSource`, `Port` | The server host and port. |
| `Database` | The database path on the server, or the database file of an embedded connection. |
| `UserID`, `Password` | Credentials. |
| `ServerType` | `FbServerType.Default` for a server, or `FbServerType.Embedded` for an embedded database. |
| `ClientLibrary` | The native client library that an embedded connection loads, such as `fbclient.dll`. |
| `Pooling`, `MinPoolSize`, `MaxPoolSize` | Connection-pool settings. |

`DBMSType` reads the server type through `FbConnectionStringBuilder`, so both `ServerType=1` and the `ServerType=Embedded` that the builder writes report `FirebirdFile`, whatever their case or spacing. A connection string that the builder cannot parse is reported as `Firebird` and fails when the connection opens.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery` and `ExecuteReader`, and the batch commands use `0` as well. `ExecuteScalar` keeps the provider's command default. To enforce a per-command timeout, create a command explicitly as below.

## 📝 Insert, query, update and delete

The example creates a table and a sequence for its keys, and then works with the rows in one scope:

```csharp
dac.ExecuteNonQuery(@"CREATE TABLE products (
	id INTEGER NOT NULL PRIMARY KEY,
	name VARCHAR(100) NOT NULL,
	price DECIMAL(10,2) NOT NULL,
	stock INTEGER NOT NULL
)");
dac.ExecuteNonQuery("CREATE SEQUENCE gen_products_id");

using (dac.EnterScope()) {
	// Insert returns the affected-row count, so RETURNING hands back the generated key.
	var laptopID = dac.ExecuteScalar<int>("INSERT INTO products (id, name, price, stock) VALUES (NEXT VALUE FOR gen_products_id, 'Laptop', 999.99, 50) RETURNING id");

	// The builder quotes names, so pass the upper-case names of objects created without quotes.
	var mouseID = dac.ExecuteScalar<int>("SELECT NEXT VALUE FOR gen_products_id FROM rdb$database");
	dac.Insert("PRODUCTS", new[] { new ColumnValue("ID", mouseID), new ColumnValue("NAME", "Mouse"), new ColumnValue("PRICE", 29.99m), new ColumnValue("STOCK", 200) });

	foreach (DataRow row in dac.ExecuteQuery("SELECT name, price FROM products WHERE price > 100 ORDER BY price DESC").Rows)
		Console.WriteLine($"{row["NAME"]} costs {row["PRICE"]}");

	var firstPage = dac.Select("PRODUCTS", columns: new[] { "NAME", "PRICE" }, orderByClause: "PRICE", limit: 10, offset: 0);
	var mice = dac.ExecuteQuery("SELECT * FROM products WHERE name = {0}", SQLBuilderCommand.Literal("Mouse"));
	var totalStock = dac.ExecuteScalar<long>("SELECT SUM(stock) FROM products");

	dac.Update("PRODUCTS", new[] { new ColumnValue("STOCK", 45), new ColumnValue("PRICE", 1099.99m) }, new[] { new ColumnValue("ID", laptopID) });
	dac.Delete("PRODUCTS", new[] { new ColumnValue("ID", mouseID) });
	dac.ExecuteNonQuery("DELETE FROM products WHERE stock < 10");
}
```

Each `ExecuteNonQuery` outside a scope transaction commits on its own, so the table and the sequence exist before the scope uses them. Firebird returns unquoted names in upper case, so the result columns are `NAME` and `PRICE`. `ExecuteQuery(sql, args)` fills `{0}` placeholders with `string.Format`, so wrap values in `SQLBuilderCommand.Literal` to have the builder quote and escape them. The builder writes `decimal` and `double` literals in the current culture, so run under a culture whose decimal separator is a period, such as `CultureInfo.InvariantCulture`.

## 🔎 Bound parameters

The DAC's string-based methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats its arguments into the SQL through the builder, so it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.EnterScope()) {
	using var command = scope.Connection.CreateCommand();
	command.CommandText = "SELECT CAST(@value AS INTEGER) FROM rdb$database";
	command.CommandTimeout = 30;
	if (scope.Transaction is RestrictedTransaction transaction)
		command.Transaction = transaction.DangerousInternalTransaction;

	var parameter = command.CreateParameter();
	parameter.ParameterName = "@value";
	parameter.DbType = DbType.Int32;
	parameter.Value = 42;
	command.Parameters.Add(parameter);

	var echoedValue = Tools.Object.ChangeType<int>(command.ExecuteScalar());
	Console.WriteLine(echoedValue); // 42
}
```

The `CAST` gives Firebird a type for the parameter in the select list. The `Transaction` assignment attaches the command to any local transaction that the scope inherited, as the DAC's own methods do. Dispose commands and readers before their scope ends. `scope.Connection` belongs to the scope, so its `Close` and `Dispose` methods throw. A reader from `ExecuteReader` needs an outer scope that stays open while the reader is consumed, whereas `ExecuteQuery` returns a materialized `DataTable`.

`ExecuteScalar` and the readers from `ExecuteReader` pass `Guid` values through `FirebirdCorrectingReader.CorrectGuid`, which reverses the byte order of the first three GUID groups on little-endian machines. `ExecuteQuery` reads through `ExecuteReader`, so its results are converted too. A command you create yourself returns the provider's values unchanged, so wrap its reader in `new FirebirdCorrectingReader(reader)` to read GUIDs the same way. The builder writes `Guid` literals with `CHAR_TO_UUID('...')`.

## 🔄 Transactions and lifecycle

The following example assumes an existing `app_counters` table with numeric `id` and `counter_value` columns and rows `1` and `2`:

```csharp
using (var scope = dac.EnterScope()) {
	scope.BeginTransaction(IsolationLevel.ReadCommitted);
	dac.ExecuteNonQuery("UPDATE app_counters SET counter_value = counter_value + 1 WHERE id = 1");
	dac.ExecuteNonQuery("UPDATE app_counters SET counter_value = counter_value + 1 WHERE id = 2");
	scope.Commit();
}
```

- `DefaultIsolationLevel` is `ReadCommitted`, which `scope.BeginTransaction()` uses when no level is given.
- Commit explicitly. Disposing the scope that began a transaction releases the transaction without committing it, and `scope.Rollback()` abandons it explicitly. The outermost scope closes the connection when it is disposed.
- With the default `UseScopeOsmosis = true`, nested scopes with the same connection string share one connection and transaction. A nested `Commit` does not commit on its own, and a nested `Rollback` turns the owning scope's `Commit` into a rollback. These are shared transactions, not savepoints.
- Setting `UseScopeOsmosis = false` keeps different DAC instances apart, while scopes on the same instance still nest.
- A scope that opens its connection inside an ambient `System.Transactions.TransactionScope` enlists the connection through the provider. Let the ambient scope complete the work, because `DACScope.BeginTransaction`, `Commit` and `Rollback` throw inside it. Ambient and distributed transaction support depends on the provider and platform.

The following money transfer assumes an `accounts` table with `id` and `balance` columns. It leaves its scope without committing when the balance check fails, so neither update is kept:

```csharp
try {
	using var scope = dac.EnterScope();
	scope.BeginTransaction();
	dac.ExecuteNonQuery("UPDATE accounts SET balance = balance - 1000 WHERE id = 1");
	if (dac.ExecuteScalar<decimal>("SELECT balance FROM accounts WHERE id = 1") < 0)
		throw new InvalidOperationException("Insufficient funds");
	dac.ExecuteNonQuery("UPDATE accounts SET balance = balance + 1000 WHERE id = 2");
	scope.Commit();
} catch (InvalidOperationException error) {
	Console.WriteLine($"Transfer abandoned: {error.Message}");
}
```

## 🧾 Stored procedures and triggers

`ExecuteNonQuery` sends one statement, so a procedure or trigger body needs no `SET TERM`. A selectable procedure, which returns rows through `SUSPEND`, is queried like a table:

```csharp
dac.ExecuteNonQuery(@"CREATE OR ALTER PROCEDURE get_products_up_to (max_price DECIMAL(10,2))
RETURNS (product_id INTEGER, product_name VARCHAR(100), product_price DECIMAL(10,2))
AS
BEGIN
	FOR SELECT id, name, price FROM products WHERE price <= :max_price
		INTO :product_id, :product_name, :product_price
	DO SUSPEND;
END");

var cheapProducts = dac.ExecuteQuery("SELECT * FROM get_products_up_to({0})", SQLBuilderCommand.Literal(50m));

dac.ExecuteNonQuery(@"CREATE TABLE audit_log (
	id INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
	table_name VARCHAR(50) NOT NULL,
	action VARCHAR(10) NOT NULL,
	audited_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)");
dac.ExecuteNonQuery(@"CREATE OR ALTER TRIGGER products_audit FOR products AFTER UPDATE
AS
BEGIN
	INSERT INTO audit_log (table_name, action) VALUES ('PRODUCTS', 'UPDATE');
END");

// The trigger writes an audit row for each updated product.
dac.Update("PRODUCTS", new[] { new ColumnValue("PRICE", 99.99m) }, new[] { new ColumnValue("ID", 1) });
Console.WriteLine(dac.ExecuteScalar<long>("SELECT COUNT(*) FROM audit_log"));
```

`GENERATED BY DEFAULT AS IDENTITY` needs Firebird 3 or later. On older servers, fill the key from a sequence in a `BEFORE INSERT` trigger.

## 🗂️ Schema and SQL dialect

`GetSchema()` reads Firebird's `RDB$` system tables for the relations and triggers whose `RDB$SYSTEM_FLAG` is `0`. The result uses the shared `DBSchema` model of tables, columns, keys and trigger names. Auto-increment columns are not detected, and sequence fields are not populated. Only the `CASCADE` foreign-key rule is reported as cascading. `RESTRICT`, `NO ACTION`, `SET NULL` and `SET DEFAULT` are not.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`FirebirdSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Double quotes, unless the name already starts with one. Quoted names are case-sensitive. |
| Statement terminator | `^`. For more than one statement, `ToString()` returns a script that starts with `SET TERM ^;`. |
| Paging fragment | `FIRST n SKIP m`, emitted directly after `SELECT` and before `DISTINCT`. |
| Sequence expression | `NEXT VALUE FOR "sequence"`. |
| Generated identity expression | `gen_id("sequence", 0)`. The hint must name the sequence, or `GetLastIdentity` throws `ArgumentNullException`. |
| Transaction SQL | `SAVEPOINT TRANn`, `COMMIT` and `ROLLBACK TO TRANn`. `ExecuteBatch` skips these statements. |
| Variables | `VariableName` emits `:name` and `AssignVariable` emits `name = value`. `DeclareVariable` records a declaration that `ToString()` emits as `DECLARE VARIABLE` inside an `EXECUTE BLOCK`. |
| Scalar value selection | `SelectValues` adds `FROM RDB$DATABASE`. |
| `Guid` literals | `CHAR_TO_UUID('...')`. |
| `byte[]` literals | Binary string literals such as `X'0AFF'`. |
| Temporary tables | `TableType.Temporary` creates a `GLOBAL TEMPORARY TABLE` with `ON COMMIT PRESERVE ROWS`, and `TableType.InMemory` throws. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |

The builder quotes every name, and Firebird stores unquoted names in upper case. Pass builder table and column names in upper case for objects created without quotes.

`SET TERM` is an isql command, so do not pass a multi-statement `ToString()` script to `ExecuteNonQuery`. Use `ExecuteBatch`, which sends each statement separately.

`Insert`, `Update` and `Delete` render `ColumnValue` values as SQL literals rather than parameters, and `Insert` returns the affected-row count rather than a generated key.

## 🛠️ Database administration

The package has no `Tools.Firebird` helper and no `DatabaseManagerBase` implementation. Create and drop databases with FirebirdClient, for example through `FbConnection.CreateDatabase` and `FbConnection.DropDatabase`, or with Firebird's administration tools.

```csharp
var newDatabaseConnectionString = new FbConnectionStringBuilder {
	DataSource = "localhost",
	Database = "/data/newdb.fdb",
	UserID = "appuser",
	Password = Environment.GetEnvironmentVariable("FIREBIRD_PASSWORD")
}.ToString();

FbConnection.CreateDatabase(newDatabaseConnectionString, pageSize: 16384, forcedWrites: true, overwrite: false);
var newDac = new FirebirdDAC(newDatabaseConnectionString);
using (newDac.EnterScope())
	Console.WriteLine(newDac.ExecuteScalar<string>("SELECT rdb$get_context('SYSTEM', 'DB_NAME') FROM rdb$database"));

// Pooled connections keep the database open, so clear the pools before dropping it.
FbConnection.ClearAllPools();
FbConnection.DropDatabase(newDatabaseConnectionString);
```

The user needs permission to create databases on the server, and the path is resolved on the server.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`, and the shared `BulkInsertAsync` wrapper reaches the same method.
- The shared `IDACAsyncExtensions` run the synchronous DAC methods through `Task.Run`, so they do not use the driver's asynchronous I/O.
- The package supplies no ORM, schema generator or migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — guards, logging and the transactional scope base classes.
- [Sphere10.Framework.Windows.Forms.Firebird](../Sphere10.Framework.Windows.Forms.Firebird/README.md) — Windows Forms connection editors for server and embedded Firebird databases.
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) — NHibernate integration for the shared data layer.
- [FirebirdDAC](FirebirdDAC.cs), [FirebirdCorrectingReader](FirebirdCorrectingReader.cs) and the shared [FirebirdSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/FirebirdSQLBuilder.cs) — provider and dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.Firebird/Sphere10.Framework.Data.Firebird.csproj -c Release
```

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
