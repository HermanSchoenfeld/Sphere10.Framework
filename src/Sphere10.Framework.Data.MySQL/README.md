<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.MySQL

MySQL data access through the shared Sphere10 `IDAC` abstraction. The package supplies a MySqlConnector-backed DAC, connection-string and database administration helpers, and a database manager. SQL generation comes from `MySQLBuilder` in the shared data package.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.MySQL
```

Targets **.NET 10** (`net10.0`). The package brings in `MySqlConnector`, `Sphere10.Framework.Data` and the core framework. An existing database and credentials are required to execute the examples; server features depend on the server and provider versions in use.

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

using (dac.BeginScope()) {
	var serverVersion = dac.ExecuteScalar<string>("SELECT VERSION()");
	Console.WriteLine(serverVersion);
}
```

Constructing a DAC, including through `Tools.MySQL.Open(...)`, only prepares its configuration. `BeginScope()` opens the connection by default. The scope owns the connection; the DAC itself is not disposable.

## 🧩 Public API

Types are in `Sphere10.Framework.Data`; the static facade is in the global `Tools` namespace.

| API | Purpose |
|---|---|
| `MySQLDAC(connectionString, logger)` | Provider implementation of `DACBase` / `IDAC`. |
| `Tools.MySQL.Open(...)` | Builds a connection string and returns a configured DAC. |
| `Tools.MySQL.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `CreateConnection()` / `CreateOpenConnection()` | Creates a caller-owned `MySqlConnection`, closed or opened respectively. |
| `BeginScope()` | Opens a managed connection scope, optionally with a transaction. |
| `ExecuteScalar<T>(sql)` / `ExecuteQuery(sql)` | Returns a converted scalar or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)` / `ExecuteReader(sql)` | Executes a statement or returns a disposable reader. |
| `Insert`, `Update`, `Delete` | Shared SQL-builder helpers; return affected-row counts. |
| `CreateSQLBuilder()` | Returns `MySQLBuilder` through `ISQLBuilder`. |
| `GetSchema()` | Reads the provider-specific metadata described below. |
| `MySQLDatabaseManager` | Implements the supported subset of the common database-management contract. |

## 🔧 Connection configuration

For separate configuration fields, use the facade:

```csharp
var configuredDac = Tools.MySQL.Open(
	server: "localhost", database: "appdb", username: "appuser",
	password: Environment.GetEnvironmentVariable("MYSQL_PASSWORD"),
	port: 3306, pooling: true, maxPoolSize: 50,
	connectionTimeout: TimeSpan.FromSeconds(15), applicationName: "ExampleApp"
);
```

`CreateConnectionString` accepts the same connection options. Omitted options retain provider defaults.

| Option | Mapping |
|---|---|
| `server`, `database`, `username`, `password`, `port` | MySqlConnector server, database and credentials. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection pool settings. |
| `connectionTimeout`, `commandTimeout` | Time spans rounded to whole seconds; the latter sets `DefaultCommandTimeout`. |
| `characterSet`, `applicationName` | Provider connection-string settings. |
| `sslMode` | `true` selects `MySqlSslMode.Required`; `false` selects `None`; omitted leaves the provider default. |
| `logger` | Optional framework `ILogger` for the DAC; accepted by `Open`, not `CreateConnectionString`. |

For certificate-verification modes and other provider options, build a full connection string with `MySqlConnectionStringBuilder` and pass it to `new MySQLDAC(connectionString)`. The Boolean TLS option exposes only the two modes listed above.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`. `ExecuteScalar` keeps the provider command default. To enforce a per-command timeout, create a command explicitly as below.

## 🔎 Bound parameters

The DAC's string-based execution methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats SQL through the builder; it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.BeginScope()) {
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

The transaction assignment connects a manually created command to any local transaction inherited by the scope. DAC execution methods perform this assignment internally. Dispose commands and readers before their scope ends; do not close or dispose `scope.Connection` directly. Keep an outer scope alive while consuming `ExecuteReader` results, or use `ExecuteQuery` to materialize the result before returning.

## 🔄 Transactions and lifecycle

The following example assumes an existing `app_counters` table with numeric `id` and `counter_value` columns and rows `1` and `2`:

```csharp
using (var scope = dac.BeginScope()) {
	scope.BeginTransaction(IsolationLevel.ReadCommitted);
	dac.ExecuteNonQuery("UPDATE app_counters SET counter_value = counter_value + 1 WHERE id = 1");
	dac.ExecuteNonQuery("UPDATE app_counters SET counter_value = counter_value + 1 WHERE id = 2");
	scope.Commit();
}
```

`DefaultIsolationLevel` is `ReadCommitted`. Call `scope.Rollback()` when explicitly abandoning a transaction. Disposal releases an owned transaction and connection; it does not commit them.

With the default `UseScopeOsmosis = true`, nested scopes with the same connection string share the connection and current transaction. Setting it to `false` separates different DAC instances; scopes on the same instance can still nest. A nested `Commit` does not commit the database transaction independently. An explicit nested `Rollback` votes for rollback when the owning scope commits. These are shared transactions, not savepoints.

Scopes also enlist newly owned connections in an ambient `System.Transactions.TransactionScope` through the provider. In that case, let the ambient scope control completion; mixing its transaction with `DACScope.BeginTransaction`, `Commit` or `Rollback` is prohibited. Provider and platform support govern ambient/distributed transaction availability.

## 🗂️ Schema and SQL dialect

`GetSchema()` queries `information_schema` for the currently selected `DATABASE()`. It produces the shared `DBSchema` representation of tables, columns, keys and trigger names. It does not enumerate every database on the server. Auto-increment detection uses the column's `EXTRA` metadata; sequence fields are not populated.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`MySQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Backticks. |
| Paging | `LIMIT` / `OFFSET`. |
| Generated identity expression | `LAST_INSERT_ID()`. |
| Transaction SQL | `START TRANSACTION`, `COMMIT`, `ROLLBACK`. |
| Session variables | `@name` and `SET @name = ...`; declaring a variable emits nothing. |
| Sequence expression | `NextSequenceValue` throws `NotSupportedException`. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |

The paging row describes the emitted SQL fragment. The inherited `SQLBuilderBase.Select` currently inserts that fragment before the selected columns, so `Select(limit: ...)` does not produce valid paging SQL for this provider. Use an explicit SQL query with the paging clause in its correct position.

`Insert` returns the affected-row count, not a generated identity. Builder methods generate SQL text; they do not create connections or bind command parameters.

## 🛠️ Database administration

`MySQLDatabaseManager` implements the common `DatabaseManagerBase` contract. Its `GenerateConnectionString` delegates to `Tools.MySQL`; `DatabaseExists`, `CreateEmptyDatabase` and `DropDatabase` delegate to the corresponding administration helpers.

| Helper | Behavior |
|---|---|
| `Tools.MySQL.TestConnectionString(connectionString)` | Attempts to open a scope; returns `false` on an opening failure. |
| `Tools.MySQL.Exists(server, databaseName, ...)` | Connects to `information_schema` and checks `SCHEMATA`. |
| `Tools.MySQL.CreateDatabase(server, databaseName, ...)` | Creates a database using `utf8mb4` and `utf8mb4_unicode_ci`. |
| `Tools.MySQL.DropDatabase(server, databaseName, ...)` | Drops the database; `throwIfNotExists` defaults to `true`. |

Creation defaults to `AlreadyExistsPolicy.Error`; `Skip` preserves an existing database and `Overwrite` drops it before recreation. These calls require appropriate database administration privileges. Database names are interpolated into administration SQL, so supply trusted, validated configuration identifiers.

The manager reconstructs maintenance connections from server, database, username, password and port; additional settings in its input connection string are not forwarded. `CreateApplicationDatabase` throws `NotSupportedException`: application tables, migrations and seed data are the application's responsibility.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`; the shared `BulkInsertAsync` wrapper reaches the same unimplemented method.
- Shared `IDACAsyncExtensions` use `Task.Run` around synchronous DAC methods. They do not expose the provider's native asynchronous I/O API.
- SQL dialect and metadata support are limited to the implementations described above. This package does not supply an ORM, an application schema generator or a migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — common utilities, guards, logging and transaction scopes.
- [Sphere10.Framework.Windows.Forms.MySQL](../Sphere10.Framework.Windows.Forms.MySQL/README.md) — Windows Forms connection UI for this provider.
- [MySQLDAC](MySQLDAC.cs), [Tools.MySQL](MySQLTool.cs), [MySQLDatabaseManager](MySQLDatabaseManager.cs) — provider implementation.
- [MySQLBuilder](../Sphere10.Framework.Data/SQLBuilder/MySQLBuilder.cs) — dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.MySQL/Sphere10.Framework.Data.MySQL.csproj -c Release
```

## 📄 License

Distributed under the [MIT license](https://opensource.org/license/mit). See the repository [LICENSE](../../LICENSE).
