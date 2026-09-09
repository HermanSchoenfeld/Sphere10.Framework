<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.Oracle

Oracle data access through the shared Sphere10 `IDAC` abstraction, backed by Oracle.ManagedDataAccess.Core. The package provides a DAC, connection-string helpers and an adapter for the common database-manager contract. SQL generation comes from `OracleSQLBuilder` in the shared data package.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.Oracle
```

Targets **.NET 10** (`net10.0`). The package brings in `Oracle.ManagedDataAccess.Core`, `Sphere10.Framework.Data` and the core framework. An existing database and credentials are required to execute the examples; server features depend on the server and provider versions in use.

## ⚡ Open a scope and query

Set `ORACLE_CONNECTION_STRING` to your application's connection string. The remaining examples reuse `dac` and these imports.

```csharp
using System;
using System.Data;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var connectionString = Environment.GetEnvironmentVariable("ORACLE_CONNECTION_STRING");
Guard.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));
var dac = new OracleDAC(connectionString);

using (dac.BeginScope()) {
	var currentSchema = dac.ExecuteScalar<string>("SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL");
	Console.WriteLine(currentSchema);
}
```

Constructing a DAC, including through `Tools.Oracle.Open(...)`, only prepares its configuration. `BeginScope()` opens the connection by default. The scope owns the connection; the DAC itself is not disposable.

## 🧩 Public API

Types are in `Sphere10.Framework.Data`; the static facade is in the global `Tools` namespace.

| API | Purpose |
|---|---|
| `OracleDAC(connectionString, logger)` | Provider implementation of `DACBase` / `IDAC`. |
| `Tools.Oracle.Open(...)` | Builds a connection string and returns a configured DAC. |
| `Tools.Oracle.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `CreateConnection()` / `CreateOpenConnection()` | Creates a caller-owned `OracleConnection`, closed or opened respectively. |
| `BeginScope()` | Opens a managed connection scope, optionally with a transaction. |
| `ExecuteScalar<T>(sql)` / `ExecuteQuery(sql)` | Returns a converted scalar or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)` / `ExecuteReader(sql)` | Executes a statement or returns a disposable reader. |
| `Insert`, `Update`, `Delete` | Shared SQL-builder helpers; return affected-row counts. |
| `CreateSQLBuilder()` | Returns `OracleSQLBuilder` through `ISQLBuilder`. |
| `GetSchema()` | Reads the provider-specific metadata described below. |
| `OracleDatabaseManager` | Implements the supported subset of the common database-management contract. |

## 🔧 Connection configuration

For separate configuration fields, use the facade:

```csharp
var configuredDac = Tools.Oracle.Open(
	dataSource: "localhost", serviceName: "FREEPDB1", userId: "appuser",
	password: Environment.GetEnvironmentVariable("ORACLE_PASSWORD"),
	port: 1521, pooling: true, maxPoolSize: 50,
	connectionTimeout: TimeSpan.FromSeconds(15)
);
```

`CreateConnectionString` accepts the same connection options. Omitted options retain provider defaults.

| Option | Mapping |
|---|---|
| `dataSource` with `serviceName` | Builds a TCP connect descriptor using `dataSource` as the host and `serviceName` as `SERVICE_NAME`. |
| `dataSource` without `serviceName` | Passed unchanged to the provider: for example, a configured alias or a complete connect descriptor. |
| `port` | Used when building the host/service descriptor; defaults there to `1521`. It has no effect when `serviceName` is omitted. |
| `userId`, `password` | Provider credentials. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection pool settings. |
| `connectionTimeout` | Time span rounded to whole seconds. |
| `logger` | Optional framework `ILogger` for the DAC; accepted by `Open`, not `CreateConnectionString`. |

The convenience overload has no command-timeout or TLS option. For a different transport descriptor or additional provider settings, build the complete connection string with `OracleConnectionStringBuilder` and pass it to `new OracleDAC(connectionString)`.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`. `ExecuteScalar` keeps the provider command default. To enforce a per-command timeout, create a command explicitly as below.

## 🔎 Bound parameters

The DAC's string-based execution methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats SQL through the builder; it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.BeginScope()) {
	using var command = scope.Connection.CreateCommand();
	command.CommandText = "SELECT :value FROM DUAL";
	command.CommandTimeout = 30;
	if (scope.Transaction is RestrictedTransaction transaction)
		command.Transaction = transaction.DangerousInternalTransaction;

	var parameter = command.CreateParameter();
	parameter.ParameterName = "value";
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

`GetSchema()` reads `USER_TAB_COLUMNS`, `USER_CONS_COLUMNS`, `USER_CONSTRAINTS` and `USER_TRIGGERS`. It describes objects owned by the connected user, not all schemas or every object accessible through grants. Recycle-bin table names beginning with `BIN$` are excluded.

The shared `DBSchema` result contains tables, columns, keys and trigger names. Identity-column detection uses `IDENTITY_COLUMN`; sequence fields are not populated and cascading updates are reported as unsupported.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`OracleSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Double quotes; quoted names preserve case. |
| Paging | `OFFSET ... ROWS` / `FETCH FIRST ... ROWS ONLY`. |
| Sequence expression | `sequence.NEXTVAL`. |
| Scalar value selection | `SelectValues` adds `FROM DUAL`. |
| Transaction SQL | `BeginTransaction` emits nothing; commit and rollback emit SQL. Use `DACScope.BeginTransaction()` to start an ADO.NET transaction. |
| Generated identity expression | `GetLastIdentity` throws `NotSupportedException`; use an application-specific sequence or a provider command with `RETURNING ... INTO`. |
| Variables | `VariableName` emits `:name`; declaration and assignment throw `NotSupportedException`. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |

The paging row describes the emitted SQL fragment. The inherited `SQLBuilderBase.Select` currently inserts that fragment before the selected columns, so `Select(limit: ...)` does not produce valid paging SQL for this provider. Use an explicit SQL query with the paging clause in its correct position.

`Insert` returns the affected-row count, not a generated identity. Builder methods generate SQL text; they do not create connections or bind command parameters.

## 🛠️ Database administration

`OracleDatabaseManager` adapts the shared manager interface to an existing Oracle service:

| Member | Behavior |
|---|---|
| `GenerateConnectionString(server, database, username, password, port)` | Maps `server` to `dataSource`, `database` to `serviceName`, and `username` to `userId`. |
| `DatabaseExists(connectionString)` | Calls `Tools.Oracle.TestConnectionString`: tests whether a connection opens, not whether an application schema has been created. |
| `CreateEmptyDatabase(connectionString)` | Throws `NotSupportedException`. |
| `DropDatabase(connectionString)` | Throws `NotSupportedException`. |
| `CreateApplicationDatabase(...)` | Throws `NotSupportedException`. |

`Tools.Oracle.TestConnectionString` returns `false` on a connection-opening failure and does not return diagnostic details. Provision the service, database users and application schema separately using the appropriate Oracle administration or migration tools. There are no `Tools.Oracle.CreateDatabase`, `DropDatabase` or `Exists` helpers.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`; the shared `BulkInsertAsync` wrapper reaches the same unimplemented method.
- Shared `IDACAsyncExtensions` use `Task.Run` around synchronous DAC methods. They do not expose the provider's native asynchronous I/O API.
- SQL dialect and metadata support are limited to the implementations described above. This package does not supply an ORM, an application schema generator or a migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — common utilities, guards, logging and transaction scopes.
- [Sphere10.Framework.Windows.Forms.Oracle](../Sphere10.Framework.Windows.Forms.Oracle/README.md) — Windows Forms connection UI for this provider.
- [OracleDAC](OracleDAC.cs), [Tools.Oracle](OracleTool.cs), [OracleDatabaseManager](OracleDatabaseManager.cs) — provider implementation.
- [OracleSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/OracleSQLBuilder.cs) — dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.Oracle/Sphere10.Framework.Data.Oracle.csproj -c Release
```

## 📄 License

Distributed under the [MIT license](https://opensource.org/license/mit). See the repository [LICENSE](../../LICENSE).
