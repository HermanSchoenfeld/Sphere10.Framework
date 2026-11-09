<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.Sqlite

`Sphere10.Framework.Data.Sqlite` connects the shared `IDAC` data-access abstraction of [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) to SQLite through System.Data.SQLite. Use it for file-based or in-memory databases in applications and tests that use the shared DAC, scope and SQL-builder APIs.

The package supplies `SqliteDAC`, the `Tools.Sqlite` file and connection helpers and `SqliteDatabaseManager`, and SQL text comes from `SqliteSQLBuilder` in the shared package.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.Sqlite
```

The package depends on `System.Data.SQLite`, `SourceGear.sqlite3`, `Sphere10.Framework.Data` and `Sphere10.Framework`, where `SourceGear.sqlite3` supplies the native SQLite library for each platform. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.Sqlite/Sphere10.Framework.Data.Sqlite.csproj` with a `ProjectReference`.

SQLite needs no server, so the examples run against a local file.

## ⚡ Open a scope and query

The remaining examples reuse `dac`, its `app_counters` table and these imports.

```csharp
using System;
using System.Data;
using System.IO;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var path = Path.Combine(Path.GetTempPath(), "example.sqlite");
var dac = Tools.Sqlite.Create(path, existsPolicy: AlreadyExistsPolicy.Overwrite);

using (dac.EnterScope()) {
	dac.ExecuteNonQuery("CREATE TABLE app_counters (id INTEGER PRIMARY KEY, counter_value INTEGER NOT NULL)");
	dac.Insert("app_counters", new[] { new ColumnValue("id", 1), new ColumnValue("counter_value", 0) });
	dac.Insert("app_counters", new[] { new ColumnValue("id", 2), new ColumnValue("counter_value", 0) });
	Console.WriteLine(dac.ExecuteScalar<long>("SELECT COUNT(*) FROM app_counters")); // 2
}
```

`Tools.Sqlite.Create` writes a new database file and returns a DAC for it, and `Tools.Sqlite.Open` returns a DAC for an existing path. Constructing a DAC only stores its configuration. `EnterScope()` opens the connection by default, and the outermost scope owns that connection until it is disposed. The DAC itself is not disposable.

Each connection to `:memory:` gets its own empty database, which disappears when the connection closes. Every outermost scope opens a new connection, so keep all work on an in-memory database inside one scope:

```csharp
var memoryDac = Tools.Sqlite.Open(":memory:");
using (memoryDac.EnterScope()) {
	memoryDac.ExecuteNonQuery("CREATE TABLE notes (id INTEGER PRIMARY KEY, body TEXT)");
	memoryDac.Insert("notes", new[] { new ColumnValue("id", 1), new ColumnValue("body", "draft") });
	Console.WriteLine(memoryDac.ExecuteScalar<long>("SELECT COUNT(*) FROM notes")); // 1
}
```

## 🧩 Public API

`Tools.Sqlite` is in the global `Tools` namespace, and the other types are in `Sphere10.Framework.Data`.

| API | Purpose |
|---|---|
| `SqliteDAC(connectionString, logger)` | The provider's `DACBase` implementation of `IDAC`. |
| `Tools.Sqlite.Open(path, ...)` | Returns a `SqliteDAC` for a database path, with an optional password, journal mode, synchronous mode and logger. |
| `Tools.Sqlite.Create(path, ...)` | Creates a database file, applies `existsPolicy` when the file exists, and returns a `SqliteDAC`. |
| `Tools.Sqlite.Drop(path)`, `ExistsByPath(path)`, `ExistsByConnectionString(connectionString)` | Delete a database file, or test whether it exists. |
| `Tools.Sqlite.CreateConnectionString(...)`, `ToConnectionString(path, password)` | Build connection strings. `ToConnectionString` sets `FailIfMissing = true`. |
| `Tools.Sqlite.GetFilePathFromConnectionString(...)`, `GetDatabaseNameFromConnectionString(...)` | Read the file path, or the file name without its extension, from a connection string. An overload of `GetFilePathFromConnectionString` also returns the password. |
| `Tools.Sqlite.ClearAllPools()` | Calls `SQLiteConnection.ClearAllPools()`. |
| `EnterScope()` | Returns a `DACScope`. The outermost scope owns the connection, and nested scopes share it. Call `BeginTransaction` on the scope for a local transaction. |
| `ExecuteScalar<T>(sql)`, `ExecuteQuery(sql)` | Return a converted scalar value or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)`, `ExecuteReader(sql)` | Execute a statement, or return a reader to consume inside an outer scope. |
| `ExecuteBatch(builder)` | Sends each builder statement as its own command, including the builder's transaction statements, and returns the result sets as `DataTable` objects. |
| `Insert`, `Update`, `Delete` | Build SQL with `SqliteSQLBuilder` and return the affected-row count. |
| `CreateConnection()`, `CreateOpenConnection()` | Create a caller-owned `SQLiteConnection`, closed or opened. |
| `SelectSqliteMaster()`, `SelectSqliteSequence()` | Read `sqlite_master` and `sqlite_sequence` as typed rows. |
| `PragmaTableInfo(table)`, `PragmaIndexList(table)`, `PragmaIndexInfo(index)`, `PragmaForeignKeyList(table)` | Read the matching PRAGMA results as typed rows. |
| `CreateSQLBuilder()` | Returns a new `SqliteSQLBuilder` as `ISQLBuilder`. |
| `GetSchema()` | Reads table, column, key and trigger metadata, as described below. |
| `SqliteDatabaseManager` | Implements the shared `DatabaseManagerBase` contract for database files. |
| `DACFactory.CreateDAC(DBMSType.Sqlite, connectionString)` | Creates a `SqliteDAC` by type name, so that an application can choose the provider at run time. |

## 🔧 Connection configuration

`Open` and `Create` take the common settings as arguments:

```csharp
var walDac = Tools.Sqlite.Open(path, journalMode: SqliteJournalMode.Wal, syncMode: SqliteSyncMode.Normal);
```

| Option | Mapping |
|---|---|
| `path` | The provider's `DataSource`, which is a file path or `:memory:`. |
| `password` | The provider's `Password`. Whether it encrypts the file depends on the native SQLite library, so verify encryption before relying on it. |
| `journalMode` | A `SqliteJournalMode` value (`Default`, `Delete`, `Memory`, `Off`, `Persist`, `Truncate` or `Wal`), mapped to the provider's `JournalMode`. |
| `syncMode` | A `SqliteSyncMode` value (`Full`, `Normal` or `Off`), mapped to the provider's `SyncMode`. |
| `pageSize` | The page size in bytes, taken by `Create` and `CreateConnectionString`. `Create` also issues `PRAGMA PAGE_SIZE`. |
| `failIfMissing` | Taken by `CreateConnectionString` only. `true` makes opening a missing file fail. |
| `existsPolicy` | Taken by `Create` only. `Error` (the default) throws for an existing file, `Skip` keeps it and `Overwrite` deletes it first. |
| `logger` | An optional framework `ILogger` for the DAC, taken by `Open` and `Create`. |

`Open` does not set `FailIfMissing`, so System.Data.SQLite creates a missing file when the connection opens. When a missing file must be an error, pass `Tools.Sqlite.ToConnectionString(path)` to `new SqliteDAC(...)`. `Create` uses the `Default` journal mode and the `Normal` synchronous mode unless you pass others.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`. `ExecuteScalar` keeps the provider's command default.

## 🔎 Bound parameters

The DAC's string-based methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats its arguments into the SQL through the builder, so it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.EnterScope()) {
	using var command = scope.Connection.CreateCommand();
	command.CommandText = "SELECT counter_value FROM app_counters WHERE id = @id";
	command.CommandTimeout = 30;
	if (scope.Transaction is RestrictedTransaction transaction)
		command.Transaction = transaction.DangerousInternalTransaction;

	var parameter = command.CreateParameter();
	parameter.ParameterName = "@id";
	parameter.DbType = DbType.Int32;
	parameter.Value = 1;
	command.Parameters.Add(parameter);

	var counterValue = Tools.Object.ChangeType<long>(command.ExecuteScalar());
	Console.WriteLine(counterValue); // 0
}
```

The `Transaction` assignment attaches the command to any local transaction that the scope inherited, as the DAC's own methods do. Dispose commands and readers before their scope ends. `scope.Connection` belongs to the scope, so its `Close` and `Dispose` methods throw. A reader from `ExecuteReader` needs an outer scope that stays open while the reader is consumed, whereas `ExecuteQuery` returns a materialized `DataTable`.

## 🔄 Transactions and lifecycle

The following example updates both rows of the `app_counters` table from the first example:

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

`GetSchema()` reads `sqlite_master` and the `table_info`, `index_list`, `index_info` and `foreign_key_list` PRAGMAs, and it skips the internal `sqlite_sequence` table. The result uses the shared `DBSchema` model of tables, columns, keys and trigger names, with these SQLite-specific values:

- Primary keys are named `PK <table>`, and foreign keys are named `FK <id> <table>`.
- A primary-key column is auto-incrementing only when its table's `CREATE TABLE` text contains `PRIMARY KEY AUTOINCREMENT`.
- Precision and scale are placeholder values of 10 and 5.
- A column whose declared type starts with `VAR` or `TEXT`, in any case, reports a length of `int.MaxValue`, and other columns report `0`.
- A foreign key that omits its referenced column resolves to the referenced table's single primary-key column. `GetSchema()` throws when that table has no single-column primary key.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`SqliteSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Square brackets, unless the name is already bracketed. |
| Paging | `Select` appends `LIMIT n OFFSET m` after `ORDER BY`, so `Select(limit: ..., offset: ...)` and the `dac.Select` extension produce valid paging SQL. |
| Generated identity expression | `last_insert_rowid()`. |
| Sequence expression | `NextSequenceValue` throws `NotSupportedException`. |
| Transaction SQL | `BEGIN TRANSACTION`, `COMMIT TRANSACTION` and `ROLLBACK TRANSACTION`. |
| Variables | A `_Variables` temporary table emulates them. `DeclareVariable` creates the table on first use, `AssignVariable` updates it and `VariableName` reads it through a subquery. `End()` drops the table. |
| Binary and `Guid` literals | `byte[]` values become `X'...'` blobs, and `Guid` values become the blob of `Guid.ToByteArray()`. |
| `InsertMany` | Splits the rows into statements of up to 500 rows. |
| Auto-increment toggles | `EnableAutoIncrementID` and `DisableAutoIncrementID` emit nothing. |
| Statement terminator | `;` |

`Insert`, `Update` and `Delete` render `ColumnValue` values as SQL literals rather than parameters, and `Insert` returns the affected-row count rather than a generated key.

## 🛠️ Database administration

Administration works on database files rather than on a server:

| Member | Behavior |
|---|---|
| `Tools.Sqlite.Create(path, ...)` | Creates the file, sets the page size when one is given, runs `VACUUM` so that the file is written, and then calls `Open`. |
| `Tools.Sqlite.Drop(path, throwIfNotExists)` | Deletes the file. It throws when the file is missing unless `throwIfNotExists` is `false`. |
| `SqliteDatabaseManager.GenerateConnectionString(server, database, username, password, port)` | Rejects a server, user name or port, requires a `database` path, which need not exist yet, and returns `Tools.Sqlite.CreateConnectionString(database, password)`. |
| `SqliteDatabaseManager.DatabaseExists(connectionString)` | Tests whether the connection string's file exists. |
| `SqliteDatabaseManager.CreateEmptyDatabase(connectionString)` | Calls `Tools.Sqlite.Create` with a page size of 32768 bytes and `AlreadyExistsPolicy.Error`. |
| `SqliteDatabaseManager.DropDatabase(connectionString)` | Calls `Tools.Sqlite.Drop`, which throws when the file is missing. |
| `SqliteDatabaseManager.CreateApplicationDatabase(...)` | Throws `NotSupportedException`. |

## 🚧 Implementation boundaries

- `BulkInsert` throws `NotImplementedException`, and the shared `BulkInsertAsync` wrapper reaches the same method.
- The shared `IDACAsyncExtensions` run the synchronous DAC methods through `Task.Run`, so they do not use the driver's asynchronous I/O.
- The package supplies no ORM, schema generator or migration system.

## 🧪 Tests

`SqliteDACTests`, `SqliteToolTest` and `SqliteSQLBuilderTests` in [Sphere10.Framework.Data.Tests](../../tests/Sphere10.Framework.Data.Tests) exercise this provider. The shared `DACTests`, `DACScopeTests` and `TransactionScopeCompatibilityTests` also run against SQLite through `DACTestFixture`. The tests create temporary database files and need no server. `SqliteToolTest.Create_Password` is marked `[Ignore]`, so password support has no active test.

```bash
dotnet test tests/Sphere10.Framework.Data.Tests/Sphere10.Framework.Data.Tests.csproj -c Release --filter "FullyQualifiedName~Sqlite"
```

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — guards, logging and the transactional scope base classes.
- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite/README.md) — Windows Forms connection editors for database files, with password, journal-mode and synchronous-mode settings.
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) — NHibernate integration for the shared data layer.
- [Sphere10.Framework.NUnit.DB](../Sphere10.Framework.NUnit.DB/README.md) — `DACTestFixture`, which creates and drops temporary SQLite databases for DAC tests.
- [SqliteDAC](DAC/SqliteDAC.cs), [Tools.Sqlite](SqliteTool.cs), [SqliteDatabaseManager](SqliteDatabaseManager.cs) and the shared [SqliteSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/SqliteSQLBuilder.cs) — provider and dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.Sqlite/Sphere10.Framework.Data.Sqlite.csproj -c Release
```

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
