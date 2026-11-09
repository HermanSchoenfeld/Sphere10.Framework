<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.MySQL

`Sphere10.Framework.Data.MySQL` connects the shared `IDAC` data-access abstraction of [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) to MySQL through MySqlConnector. Use it when an application built on the shared DAC, scope and SQL-builder APIs needs to read, write or administer MySQL databases.

The package supplies `MySQLDAC`, the `Tools.MySQL` connection and administration helpers and `MySQLDatabaseManager`, and SQL text comes from `MySQLBuilder` in the shared package.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Data.MySQL` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`MySQLDAC`](MySQLDAC.cs) | class | `DACBase` for MySQL over `MySqlConnector` that uses `MySQLBuilder`, and whose `BulkInsert` throws `NotImplementedException`. |
| [`MySQLDatabaseManager`](MySQLDatabaseManager.cs) | class | `DatabaseManagerBase` that tests, creates and drops MySQL databases through `Tools.MySQL`. |
| [`Tools.MySQL`](MySQLTool.cs) | tools | Opens a `MySQLDAC`, builds connection strings, tests whether a schema exists and creates or drops databases. |
<!-- domains:end -->

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.MySQL
```

The package depends on `MySqlConnector`, `Sphere10.Framework.Data` and `Sphere10.Framework`. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.MySQL/Sphere10.Framework.Data.MySQL.csproj` with a `ProjectReference`.

The examples need an existing database and credentials, and server features depend on the server and driver versions in use.

## ⚡ Open a scope and query

Set `MYSQL_CONNECTION_STRING` to your application's connection string. The remaining examples reuse `dac` and these imports.

```csharp
using System;
using System.Data;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING");
Guard.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));
var dac = new MySQLDAC(connectionString);

using (dac.EnterScope()) {
	var serverVersion = dac.ExecuteScalar<string>("SELECT VERSION()");
	Console.WriteLine(serverVersion);
}
```

Constructing a DAC, directly or through `Tools.MySQL.Open(...)`, only stores its configuration. `EnterScope()` opens the connection by default, and the outermost scope owns that connection until it is disposed. The DAC itself is not disposable.

## 🧩 Public API

`Tools.MySQL` is in the global `Tools` namespace, and the other types are in `Sphere10.Framework.Data`.

| API | Purpose |
|---|---|
| `MySQLDAC(connectionString, logger)` | The provider's `DACBase` implementation of `IDAC`. |
| `Tools.MySQL.Open(server, database, ...)` | Builds a connection string from separate settings and returns a `MySQLDAC`. |
| `Tools.MySQL.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `EnterScope()` | Returns a `DACScope`. The outermost scope owns the connection, and nested scopes share it. Call `BeginTransaction` on the scope for a local transaction. |
| `ExecuteScalar<T>(sql)`, `ExecuteQuery(sql)` | Return a converted scalar value or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)`, `ExecuteReader(sql)` | Execute a statement, or return a reader to consume inside an outer scope. |
| `ExecuteBatch(builder)` | Sends the builder's whole script as one command and returns its result sets as `DataTable` objects. |
| `Insert`, `Update`, `Delete` | Build SQL with `MySQLBuilder` and return the affected-row count. |
| `CreateConnection()`, `CreateOpenConnection()` | Create a caller-owned `MySqlConnection`, closed or opened. |
| `CreateSQLBuilder()` | Returns a new `MySQLBuilder` as `ISQLBuilder`. |
| `GetSchema()` | Reads table, column, key and trigger metadata, as described below. |
| `MySQLDatabaseManager` | Implements the shared `DatabaseManagerBase` contract for MySQL. |
| `DACFactory.CreateDAC(DBMSType.MySQL, connectionString)` | Creates a `MySQLDAC` by type name, so that an application can choose the provider at run time. |

## 🔧 Connection configuration

For separate configuration fields, use the helper:

```csharp
var configuredDac = Tools.MySQL.Open(
	server: "localhost", database: "appdb", username: "appuser",
	password: Environment.GetEnvironmentVariable("MYSQL_PASSWORD"),
	port: 3306, pooling: true, maxPoolSize: 50,
	connectionTimeout: TimeSpan.FromSeconds(15), applicationName: "ExampleApp"
);
```

`CreateConnectionString` takes the same settings except `logger`. Omitted settings keep the provider defaults.

| Option | Mapping |
|---|---|
| `server`, `database`, `username`, `password`, `port` | The MySqlConnector server, database, credentials and port. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection-pool settings. |
| `connectionTimeout` | `ConnectionTimeout`, rounded to whole seconds. |
| `commandTimeout` | `DefaultCommandTimeout`, rounded to whole seconds. |
| `characterSet`, `applicationName` | `CharacterSet` and `ApplicationName`. |
| `sslMode` | `true` selects `MySqlSslMode.Required` and `false` selects `None`. Omitting it keeps the provider default. |
| `logger` | An optional framework `ILogger` for the DAC. Only `Open` accepts it. |

For certificate-verification modes and other provider settings, build the connection string with `MySqlConnectionStringBuilder` and pass it to `new MySQLDAC(connectionString)`. The Boolean `sslMode` option exposes only the two modes listed above.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`, so `commandTimeout` affects only `ExecuteScalar` and the commands you create yourself. To enforce a per-command timeout, create a command explicitly as below.

## 🔎 Bound parameters

The DAC's string-based methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats its arguments into the SQL through the builder, so it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.EnterScope()) {
	using var command = scope.Connection.CreateCommand();
	command.CommandText = "SELECT @value";
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

The `Transaction` assignment attaches the command to any local transaction that the scope inherited, as the DAC's own methods do. Dispose commands and readers before their scope ends. `scope.Connection` belongs to the scope, so its `Close` and `Dispose` methods throw. A reader from `ExecuteReader` needs an outer scope that stays open while the reader is consumed, whereas `ExecuteQuery` returns a materialized `DataTable`.

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

## 🗂️ Schema and SQL dialect

`GetSchema()` queries `information_schema` for the database that `DATABASE()` selects and returns the shared `DBSchema` model of tables, columns, keys and trigger names. It does not enumerate the other databases on the server. Auto-increment columns are detected from the column's `EXTRA` metadata, and sequence fields are not populated.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`MySQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Backticks, unless the name already starts with one. |
| Paging fragment | `LIMIT n OFFSET m`, emitted after `ORDER BY`, so `Select(limit: ...)` and the `dac.Select` extension page the query. |
| Generated identity expression | `LAST_INSERT_ID()`. |
| Transaction SQL | `START TRANSACTION`, `COMMIT` and `ROLLBACK`. |
| Variables | `VariableName` emits `@name` and `AssignVariable` emits `SET @name = ...`. `DeclareVariable` emits nothing. |
| Sequence expression | `NextSequenceValue` throws `NotSupportedException`. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |
| Statement terminator | `;` |

`Insert`, `Update` and `Delete` render `ColumnValue` values as SQL literals rather than parameters, and `Insert` returns the affected-row count rather than a generated key.

## 🛠️ Database administration

`MySQLDatabaseManager` implements the shared `DatabaseManagerBase` contract. `GenerateConnectionString` calls `Tools.MySQL.CreateConnectionString`, and `DatabaseExists`, `CreateEmptyDatabase` and `DropDatabase` call the helpers below.

| Helper | Behavior |
|---|---|
| `Tools.MySQL.TestConnectionString(connectionString)` | Opens and closes a scope, and returns `false` instead of throwing when that fails. |
| `Tools.MySQL.Exists(server, databaseName, ...)` | Connects to `information_schema` and checks `SCHEMATA`. |
| `Tools.MySQL.CreateDatabase(server, databaseName, ...)` | Creates the database with the `utf8mb4` character set and the `utf8mb4_unicode_ci` collation. |
| `Tools.MySQL.DropDatabase(server, databaseName, ...)` | Drops the database. It throws when the database is missing unless `throwIfNotExists` is `false`. |

`CreateDatabase` takes an `existsPolicy` that defaults to `AlreadyExistsPolicy.Error`. `Skip` keeps an existing database, and `Overwrite` drops and recreates it. These calls need administration privileges. Database names are formatted into the SQL text, so pass only trusted, validated names.

The manager rebuilds its maintenance connection from the server, database, user name, password and port, so other settings in its connection string are not forwarded. `CreateApplicationDatabase` throws `NotSupportedException`, so create application tables, migrations and seed data in the application.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`, and the shared `BulkInsertAsync` wrapper reaches the same method.
- The shared `IDACAsyncExtensions` run the synchronous DAC methods through `Task.Run`, so they do not use the driver's asynchronous I/O.
- The package supplies no ORM, schema generator or migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — guards, logging and the transactional scope base classes.
- [Sphere10.Framework.Windows.Forms.MySQL](../Sphere10.Framework.Windows.Forms.MySQL/README.md) — Windows Forms connection editors for MySQL.
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) — NHibernate integration for the shared data layer.
- [MySQLDAC](MySQLDAC.cs), [Tools.MySQL](MySQLTool.cs), [MySQLDatabaseManager](MySQLDatabaseManager.cs) and the shared [MySQLBuilder](../Sphere10.Framework.Data/SQLBuilder/MySQLBuilder.cs) — provider and dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.MySQL/Sphere10.Framework.Data.MySQL.csproj -c Release
```

## 📄 License

Distributed under the [MIT license](https://opensource.org/license/mit). See the repository [LICENSE](../../LICENSE).
