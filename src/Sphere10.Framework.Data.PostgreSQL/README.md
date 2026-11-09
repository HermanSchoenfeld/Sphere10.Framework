<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.PostgreSQL

`Sphere10.Framework.Data.PostgreSQL` connects the shared `IDAC` data-access abstraction of [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) to PostgreSQL through Npgsql. Use it when an application built on the shared DAC, scope and SQL-builder APIs needs to read, write or administer PostgreSQL databases.

The package supplies `PostgreSQLDAC`, the `Tools.PostgreSQL` connection and administration helpers and `PostgreSQLDatabaseManager`, and SQL text comes from `PostgreSQLBuilder` in the shared package.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Data.PostgreSQL` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`PostgreSQLDAC`](PostgreSQLDAC.cs) | class | `DACBase` for PostgreSQL over Npgsql that uses `PostgreSQLBuilder`, and whose `BulkInsert` throws `NotImplementedException`. |
| [`PostgreSQLDatabaseManager`](PostgreSQLDatabaseManager.cs) | class | `DatabaseManagerBase` that tests, creates and drops PostgreSQL databases through `Tools.PostgreSQL`. |
| [`Tools.PostgreSQL`](PostgreSQLTool.cs) | tools | Opens a `PostgreSQLDAC`, builds connection strings, tests whether a database exists and creates or drops databases. |
<!-- domains:end -->

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.PostgreSQL
```

The package depends on `Npgsql`, `Sphere10.Framework.Data` and `Sphere10.Framework`. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.PostgreSQL/Sphere10.Framework.Data.PostgreSQL.csproj` with a `ProjectReference`.

The examples need an existing database and credentials, and server features depend on the server and driver versions in use.

## ⚡ Open a scope and query

Set `POSTGRESQL_CONNECTION_STRING` to your application's connection string. The remaining examples reuse `dac` and these imports.

```csharp
using System;
using System.Data;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING");
Guard.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));
var dac = new PostgreSQLDAC(connectionString);

using (dac.EnterScope()) {
	var serverVersion = dac.ExecuteScalar<string>("SELECT version()");
	Console.WriteLine(serverVersion);
}
```

Constructing a DAC, directly or through `Tools.PostgreSQL.Open(...)`, only stores its configuration. `EnterScope()` opens the connection by default, and the outermost scope owns that connection until it is disposed. The DAC itself is not disposable.

## 🧩 Public API

`Tools.PostgreSQL` is in the global `Tools` namespace, and the other types are in `Sphere10.Framework.Data`.

| API | Purpose |
|---|---|
| `PostgreSQLDAC(connectionString, logger)` | The provider's `DACBase` implementation of `IDAC`. |
| `Tools.PostgreSQL.Open(host, database, ...)` | Builds a connection string from separate settings and returns a `PostgreSQLDAC`. |
| `Tools.PostgreSQL.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `EnterScope()` | Returns a `DACScope`. The outermost scope owns the connection, and nested scopes share it. Call `BeginTransaction` on the scope for a local transaction. |
| `ExecuteScalar<T>(sql)`, `ExecuteQuery(sql)` | Return a converted scalar value or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)`, `ExecuteReader(sql)` | Execute a statement, or return a reader to consume inside an outer scope. |
| `ExecuteBatch(builder)` | Sends the builder's whole script as one command and returns its result sets as `DataTable` objects. |
| `Insert`, `Update`, `Delete` | Build SQL with `PostgreSQLBuilder` and return the affected-row count. |
| `CreateConnection()`, `CreateOpenConnection()` | Create a caller-owned `NpgsqlConnection`, closed or opened. |
| `CreateSQLBuilder()` | Returns a new `PostgreSQLBuilder` as `ISQLBuilder`. |
| `GetSchema()` | Reads table, column, key and trigger metadata, as described below. |
| `PostgreSQLDatabaseManager` | Implements the shared `DatabaseManagerBase` contract for PostgreSQL. |
| `DACFactory.CreateDAC(DBMSType.PostgreSQL, connectionString)` | Creates a `PostgreSQLDAC` by type name, so that an application can choose the provider at run time. |

## 🔧 Connection configuration

For separate configuration fields, use the helper:

```csharp
var configuredDac = Tools.PostgreSQL.Open(
	host: "localhost", database: "appdb", username: "appuser",
	password: Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD"),
	port: 5432, pooling: true, maxPoolSize: 50,
	connectionTimeout: TimeSpan.FromSeconds(15), searchPath: "public", applicationName: "ExampleApp"
);
```

`CreateConnectionString` takes the same settings except `logger`. Omitted settings keep the provider defaults.

| Option | Mapping |
|---|---|
| `host`, `database`, `username`, `password`, `port` | The Npgsql host, database, credentials and port. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection-pool settings. |
| `connectionTimeout` | `Timeout`, rounded to whole seconds. |
| `commandTimeout` | `CommandTimeout`, rounded to whole seconds. |
| `searchPath`, `applicationName` | `SearchPath` and `ApplicationName`. |
| `sslMode` | `true` selects `SslMode.Require` and `false` selects `Disable`. Omitting it keeps the provider default. |
| `logger` | An optional framework `ILogger` for the DAC. Only `Open` accepts it. |

For certificate-verification modes and other provider settings, build the connection string with `NpgsqlConnectionStringBuilder` and pass it to `new PostgreSQLDAC(connectionString)`. The Boolean `sslMode` option exposes only the two modes listed above.

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

`GetSchema()` queries `information_schema` for the tables and triggers of `current_schema()`, the first existing schema in the connection's search path. That is `public` by default, and a `searchPath` setting selects the same schema in which unqualified builder names resolve. Tables in other schemas are not read, and table names are reported without their schema. The result uses the shared `DBSchema` model of tables, columns, keys and trigger names.

A column is reported as auto-incrementing when its default starts with `nextval(`. Identity columns declared with `GENERATED ... AS IDENTITY` have no such default and are not detected, and sequence fields are not populated.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`PostgreSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Double quotes, unless the name already starts with one. Quoted names are case-sensitive. |
| Paging fragment | `LIMIT n OFFSET m`, emitted after `ORDER BY`, so `Select(limit: ...)` and the `dac.Select` extension page the query. |
| Sequence expression | `nextval('sequence')`. |
| Generated identity expression | `lastval()`, which reads the session's most recent sequence value. |
| Transaction SQL | `BEGIN`, `COMMIT` and `ROLLBACK`. |
| Variables | `VariableName`, `DeclareVariable` and `AssignVariable` throw `NotSupportedException`. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |
| Statement terminator | `;` |

The builder quotes every name, and PostgreSQL stores unquoted names in lower case. Pass builder table and column names in lower case for objects created without quotes.

`Insert`, `Update` and `Delete` render `ColumnValue` values as SQL literals rather than parameters, and `Insert` returns the affected-row count rather than a generated key.

## 🛠️ Database administration

`PostgreSQLDatabaseManager` implements the shared `DatabaseManagerBase` contract. `GenerateConnectionString` calls `Tools.PostgreSQL.CreateConnectionString`, and `DatabaseExists`, `CreateEmptyDatabase` and `DropDatabase` call the helpers below.

| Helper | Behavior |
|---|---|
| `Tools.PostgreSQL.TestConnectionString(connectionString)` | Opens and closes a scope, and returns `false` instead of throwing when that fails. |
| `Tools.PostgreSQL.Exists(host, databaseName, ...)` | Connects to the `postgres` maintenance database and checks `pg_database`. |
| `Tools.PostgreSQL.CreateDatabase(host, databaseName, ...)` | Creates the database through a maintenance connection. |
| `Tools.PostgreSQL.DropDatabase(host, databaseName, ...)` | Terminates the other sessions connected to the database and then drops it. It throws when the database is missing unless `throwIfNotExists` is `false`. |

`CreateDatabase` takes an `existsPolicy` that defaults to `AlreadyExistsPolicy.Error`. `Skip` keeps an existing database, and `Overwrite` drops and recreates it. Unlike `DropDatabase`, the overwrite path does not terminate other sessions first, so active connections can prevent the recreation.

These calls need access to the `postgres` maintenance database and administration privileges. Database names are formatted into the SQL text, so pass only trusted, validated names. The manager rebuilds its maintenance connection from the host, database, user name, password and port, so other settings in its connection string are not forwarded. `CreateApplicationDatabase` throws `NotSupportedException`, so create application tables, migrations and seed data in the application.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`, and the shared `BulkInsertAsync` wrapper reaches the same method.
- The shared `IDACAsyncExtensions` run the synchronous DAC methods through `Task.Run`, so they do not use the driver's asynchronous I/O.
- The package supplies no ORM, schema generator or migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — guards, logging and the transactional scope base classes.
- [Sphere10.Framework.Windows.Forms.PostgreSQL](../Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) — Windows Forms connection editors for PostgreSQL.
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) — NHibernate integration for the shared data layer.
- [PostgreSQLDAC](PostgreSQLDAC.cs), [Tools.PostgreSQL](PostgreSQLTool.cs), [PostgreSQLDatabaseManager](PostgreSQLDatabaseManager.cs) and the shared [PostgreSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/PostgreSQLBuilder.cs) — provider and dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.PostgreSQL/Sphere10.Framework.Data.PostgreSQL.csproj -c Release
```

## 📄 License

Distributed under the [MIT license](https://opensource.org/license/mit). See the repository [LICENSE](../../LICENSE).
