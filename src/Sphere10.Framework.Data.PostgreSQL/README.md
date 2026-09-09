<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.PostgreSQL

PostgreSQL data access through the shared Sphere10 `IDAC` abstraction. The package supplies an Npgsql-backed DAC, connection-string and database administration helpers, and a database manager. SQL generation comes from `PostgreSQLBuilder` in the shared data package.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.PostgreSQL
```

Targets **.NET 10** (`net10.0`). The package brings in `Npgsql`, `Sphere10.Framework.Data` and the core framework. An existing database and credentials are required to execute the examples; server features depend on the server and provider versions in use.

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

using (dac.BeginScope()) {
	var serverVersion = dac.ExecuteScalar<string>("SELECT version()");
	Console.WriteLine(serverVersion);
}
```

Constructing a DAC, including through `Tools.PostgreSQL.Open(...)`, only prepares its configuration. `BeginScope()` opens the connection by default. The scope owns the connection; the DAC itself is not disposable.

## 🧩 Public API

Types are in `Sphere10.Framework.Data`; the static facade is in the global `Tools` namespace.

| API | Purpose |
|---|---|
| `PostgreSQLDAC(connectionString, logger)` | Provider implementation of `DACBase` / `IDAC`. |
| `Tools.PostgreSQL.Open(...)` | Builds a connection string and returns a configured DAC. |
| `Tools.PostgreSQL.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `CreateConnection()` / `CreateOpenConnection()` | Creates a caller-owned `NpgsqlConnection`, closed or opened respectively. |
| `BeginScope()` | Opens a managed connection scope, optionally with a transaction. |
| `ExecuteScalar<T>(sql)` / `ExecuteQuery(sql)` | Returns a converted scalar or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)` / `ExecuteReader(sql)` | Executes a statement or returns a disposable reader. |
| `Insert`, `Update`, `Delete` | Shared SQL-builder helpers; return affected-row counts. |
| `CreateSQLBuilder()` | Returns `PostgreSQLBuilder` through `ISQLBuilder`. |
| `GetSchema()` | Reads the provider-specific metadata described below. |
| `PostgreSQLDatabaseManager` | Implements the supported subset of the common database-management contract. |

## 🔧 Connection configuration

For separate configuration fields, use the facade:

```csharp
var configuredDac = Tools.PostgreSQL.Open(
	host: "localhost", database: "appdb", username: "appuser",
	password: Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD"),
	port: 5432, pooling: true, maxPoolSize: 50,
	connectionTimeout: TimeSpan.FromSeconds(15), searchPath: "public", applicationName: "ExampleApp"
);
```

`CreateConnectionString` accepts the same connection options. Omitted options retain provider defaults.

| Option | Mapping |
|---|---|
| `host`, `database`, `username`, `password`, `port` | Npgsql host, database and credentials. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection pool settings. |
| `connectionTimeout`, `commandTimeout` | Time spans rounded to whole seconds; set Npgsql `Timeout` and `CommandTimeout`. |
| `searchPath`, `applicationName` | Session search path and application name. |
| `sslMode` | `true` selects `SslMode.Require`; `false` selects `Disable`; omitted leaves the provider default. |
| `logger` | Optional framework `ILogger` for the DAC; accepted by `Open`, not `CreateConnectionString`. |

For certificate-verification modes and other provider options, build a full connection string with `NpgsqlConnectionStringBuilder` and pass it to `new PostgreSQLDAC(connectionString)`. The Boolean TLS option exposes only the two modes listed above.

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

`GetSchema()` queries `information_schema` with a hardcoded `public` schema filter for both tables and triggers. A different connection `searchPath` changes SQL name resolution but does not expand that metadata scope. The result uses the shared `DBSchema` representation of tables, columns, keys and trigger names.

Auto-increment detection currently checks for a `nextval(...)` column default. It does not provide comprehensive identity-column detection, and sequence fields are not populated.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`PostgreSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Double quotes; quoted names preserve case. |
| Paging | `LIMIT` / `OFFSET`. |
| Sequence expression | `nextval('sequence')`. |
| Generated identity expression | `lastval()` for the current session. |
| Transaction SQL | `BEGIN`, `COMMIT`, `ROLLBACK`. |
| Variables | `VariableName`, `DeclareVariable` and `AssignVariable` throw `NotSupportedException`. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |

The paging row describes the emitted SQL fragment. The inherited `SQLBuilderBase.Select` currently inserts that fragment before the selected columns, so `Select(limit: ...)` does not produce valid paging SQL for this provider. Use an explicit SQL query with the paging clause in its correct position.

`Insert` returns the affected-row count, not a generated identity. Builder methods generate SQL text; they do not create connections or bind command parameters.

## 🛠️ Database administration

`PostgreSQLDatabaseManager` implements the common `DatabaseManagerBase` contract. Its `GenerateConnectionString` delegates to `Tools.PostgreSQL`; `DatabaseExists`, `CreateEmptyDatabase` and `DropDatabase` delegate to the corresponding administration helpers.

| Helper | Behavior |
|---|---|
| `Tools.PostgreSQL.TestConnectionString(connectionString)` | Attempts to open a scope; returns `false` on an opening failure. |
| `Tools.PostgreSQL.Exists(host, databaseName, ...)` | Connects to the `postgres` maintenance database and checks `pg_database`. |
| `Tools.PostgreSQL.CreateDatabase(host, databaseName, ...)` | Creates the requested database through a maintenance connection. |
| `Tools.PostgreSQL.DropDatabase(host, databaseName, ...)` | Terminates other sessions connected to the target database, then drops it. |

Creation defaults to `AlreadyExistsPolicy.Error`; `Skip` preserves an existing database and `Overwrite` drops it before recreation. The overwrite path does not terminate existing sessions, unlike the separate `DropDatabase` helper, so active connections can prevent recreation. `DropDatabase` defaults `throwIfNotExists` to `true`.

These calls require access to the `postgres` maintenance database and the appropriate administration privileges. Database names are interpolated into administration SQL, so supply trusted, validated configuration identifiers. The manager reconstructs maintenance connections from host, database, username, password and port; additional settings in its input connection string are not forwarded.

`CreateApplicationDatabase` throws `NotSupportedException`: application tables, migrations and seed data are the application's responsibility.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`; the shared `BulkInsertAsync` wrapper reaches the same unimplemented method.
- Shared `IDACAsyncExtensions` use `Task.Run` around synchronous DAC methods. They do not expose the provider's native asynchronous I/O API.
- SQL dialect and metadata support are limited to the implementations described above. This package does not supply an ORM, an application schema generator or a migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — common utilities, guards, logging and transaction scopes.
- [Sphere10.Framework.Windows.Forms.PostgreSQL](../Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) — Windows Forms connection UI for this provider.
- [PostgreSQLDAC](PostgreSQLDAC.cs), [Tools.PostgreSQL](PostgreSQLTool.cs), [PostgreSQLDatabaseManager](PostgreSQLDatabaseManager.cs) — provider implementation.
- [PostgreSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/PostgreSQLBuilder.cs) — dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.PostgreSQL/Sphere10.Framework.Data.PostgreSQL.csproj -c Release
```

## 📄 License

Distributed under the [MIT license](https://opensource.org/license/mit). See the repository [LICENSE](../../LICENSE).
