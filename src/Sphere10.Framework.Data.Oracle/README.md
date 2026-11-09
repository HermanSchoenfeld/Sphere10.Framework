<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.Oracle

`Sphere10.Framework.Data.Oracle` connects the shared `IDAC` data-access abstraction of [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) to Oracle Database through Oracle.ManagedDataAccess.Core. Use it when an application built on the shared DAC, scope and SQL-builder APIs works with an existing Oracle service and schema.

The package supplies `OracleDAC`, the `Tools.Oracle` connection helpers and `OracleDatabaseManager`, and SQL text comes from `OracleSQLBuilder` in the shared package.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.Oracle
```

The package depends on `Oracle.ManagedDataAccess.Core`, `Sphere10.Framework.Data` and `Sphere10.Framework`. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.Oracle/Sphere10.Framework.Data.Oracle.csproj` with a `ProjectReference`.

The examples need an existing service, user and credentials, and server features depend on the server and driver versions in use.

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

using (dac.EnterScope()) {
	var currentSchema = dac.ExecuteScalar<string>("SELECT SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL");
	Console.WriteLine(currentSchema);
}
```

Constructing a DAC, directly or through `Tools.Oracle.Open(...)`, only stores its configuration. `EnterScope()` opens the connection by default, and the outermost scope owns that connection until it is disposed. The DAC itself is not disposable.

## 🧩 Public API

`Tools.Oracle` is in the global `Tools` namespace, and the other types are in `Sphere10.Framework.Data`.

| API | Purpose |
|---|---|
| `OracleDAC(connectionString, logger)` | The provider's `DACBase` implementation of `IDAC`. |
| `Tools.Oracle.Open(dataSource, ...)` | Builds a connection string from separate settings and returns an `OracleDAC`. |
| `Tools.Oracle.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `EnterScope()` | Returns a `DACScope`. The outermost scope owns the connection, and nested scopes share it. Call `BeginTransaction` on the scope for a local transaction. |
| `ExecuteScalar<T>(sql)`, `ExecuteQuery(sql)` | Return a converted scalar value or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)`, `ExecuteReader(sql)` | Execute a statement, or return a reader to consume inside an outer scope. |
| `ExecuteBatch(builder)` | Sends the builder's statements one command at a time, skipping its `COMMIT` and `ROLLBACK` statements, and returns their result sets as `DataTable` objects. |
| `Insert`, `Update`, `Delete` | Build SQL with `OracleSQLBuilder` and return the affected-row count. |
| `CreateConnection()`, `CreateOpenConnection()` | Create a caller-owned `OracleConnection`, closed or opened. |
| `CreateSQLBuilder()` | Returns a new `OracleSQLBuilder` as `ISQLBuilder`. |
| `GetSchema()` | Reads table, column, key and trigger metadata, as described below. |
| `OracleDatabaseManager` | Adapts the shared `DatabaseManagerBase` contract to an existing Oracle service. |
| `DACFactory.CreateDAC(DBMSType.Oracle, connectionString)` | Creates an `OracleDAC` by type name, so that an application can choose the provider at run time. |

## 🔧 Connection configuration

For separate configuration fields, use the helper:

```csharp
var configuredDac = Tools.Oracle.Open(
	dataSource: "localhost", serviceName: "FREEPDB1", userId: "appuser",
	password: Environment.GetEnvironmentVariable("ORACLE_PASSWORD"),
	port: 1521, pooling: true, maxPoolSize: 50,
	connectionTimeout: TimeSpan.FromSeconds(15)
);
```

`CreateConnectionString` takes the same settings except `logger`. Omitted settings keep the provider defaults.

| Option | Mapping |
|---|---|
| `dataSource` with `serviceName` | Builds a TCP connect descriptor with `dataSource` as `HOST` and `serviceName` as `SERVICE_NAME`. |
| `dataSource` without `serviceName` | Passed to the provider unchanged, for example as a configured alias or a complete connect descriptor. |
| `port` | The descriptor's `PORT`, which defaults to `1521`. It has no effect without `serviceName`. |
| `userId`, `password` | Provider credentials. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection-pool settings. |
| `connectionTimeout` | `ConnectionTimeout`, rounded to whole seconds. |
| `logger` | An optional framework `ILogger` for the DAC. Only `Open` accepts it. |

The helper has no command-timeout or TLS option. For another transport descriptor or other provider settings, build the connection string with `OracleConnectionStringBuilder` and pass it to `new OracleDAC(connectionString)`.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`. `ExecuteScalar` keeps the provider's command default. To enforce a per-command timeout, create a command explicitly as below.

## 🔎 Bound parameters

The DAC's string-based methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats its arguments into the SQL through the builder, so it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.EnterScope()) {
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

`GetSchema()` reads `USER_TAB_COLUMNS`, `USER_CONS_COLUMNS`, `USER_CONSTRAINTS` and `USER_TRIGGERS`. It describes the objects that the connected user owns, not other schemas or objects reached through grants, and it skips recycle-bin tables whose names begin with `BIN$`.

The result uses the shared `DBSchema` model of tables, columns, keys and trigger names. Identity columns are detected from `IDENTITY_COLUMN`, sequence fields are not populated and cascading updates are always reported as false.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`OracleSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Double quotes, unless the name already starts with one. Quoted names are case-sensitive. |
| Paging fragment | `OFFSET m ROWS FETCH FIRST n ROWS ONLY`, emitted after `ORDER BY`. It needs Oracle Database 12c or later. |
| Sequence expression | `sequence.NEXTVAL`. |
| Scalar value selection | `SelectValues` adds `FROM DUAL`. |
| Transaction SQL | `BeginTransaction` emits nothing, while `CommitTransaction` and `RollbackTransaction` emit `COMMIT` and `ROLLBACK`. Use `DACScope.BeginTransaction()` to start an ADO.NET transaction. |
| Generated identity expression | `GetLastIdentity` throws `NotSupportedException`. Use a sequence, or a provider command with `RETURNING ... INTO`. |
| Variables | `VariableName` emits `:name`. `DeclareVariable` and `AssignVariable` throw `NotSupportedException`. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |
| Statement terminator | None. Statements end with a line break, because ODP.NET runs one statement per command and rejects a trailing `;`. |

The builder quotes every name, and Oracle stores unquoted names in upper case. Pass builder table and column names in upper case for objects created without quotes.

A multi-statement `ToString()` script is therefore not one runnable command, so pass it to `ExecuteBatch`, which sends each statement separately. SQL that you pass to `ExecuteNonQuery`, `ExecuteScalar` and `ExecuteQuery` is sent as written, so leave out a trailing `;` there too, except at the end of a PL/SQL block.

`Insert`, `Update` and `Delete` render `ColumnValue` values as SQL literals rather than parameters, and `Insert` returns the affected-row count rather than a generated key.

## 🛠️ Database administration

`OracleDatabaseManager` adapts the shared manager contract to an existing Oracle service:

| Member | Behavior |
|---|---|
| `GenerateConnectionString(server, database, username, password, port)` | Maps `server` to `dataSource`, `database` to `serviceName` and `username` to `userId`. |
| `DatabaseExists(connectionString)` | Calls `Tools.Oracle.TestConnectionString`, which tests whether a connection opens rather than whether an application schema exists. |
| `CreateEmptyDatabase(connectionString)` | Throws `NotSupportedException`. |
| `DropDatabase(connectionString)` | Throws `NotSupportedException`. |
| `CreateApplicationDatabase(...)` | Throws `NotSupportedException`. |

`Tools.Oracle.TestConnectionString` returns `false` when the connection fails to open, and it gives no diagnostic detail. Provision the service, database users and application schema with Oracle's administration or migration tools. `Tools.Oracle` has no `CreateDatabase`, `DropDatabase` or `Exists` helpers.

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`, and the shared `BulkInsertAsync` wrapper reaches the same method.
- The shared `IDACAsyncExtensions` run the synchronous DAC methods through `Task.Run`, so they do not use the driver's asynchronous I/O.
- The package supplies no ORM, schema generator or migration system.

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — guards, logging and the transactional scope base classes.
- [Sphere10.Framework.Windows.Forms.Oracle](../Sphere10.Framework.Windows.Forms.Oracle/README.md) — Windows Forms connection editors for Oracle.
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) — NHibernate integration for the shared data layer.
- [OracleDAC](OracleDAC.cs), [Tools.Oracle](OracleTool.cs), [OracleDatabaseManager](OracleDatabaseManager.cs) and the shared [OracleSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/OracleSQLBuilder.cs) — provider and dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.Oracle/Sphere10.Framework.Data.Oracle.csproj -c Release
```

## 📄 License

Distributed under the [MIT license](https://opensource.org/license/mit). See the repository [LICENSE](../../LICENSE).
