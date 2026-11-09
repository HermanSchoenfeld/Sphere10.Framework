# DAC

The SQLite data-access class and the journal-mode and synchronous-mode settings that its connection strings use.

`SqliteDAC` is the provider's `DACBase` implementation of `IDAC` over System.Data.SQLite. Constructing it only stores the connection string and optional logger, and `EnterScope()` opens the connection that the outermost scope owns. Besides the shared DAC members it reads `sqlite_master`, `sqlite_sequence` and the `table_info`, `index_list`, `index_info` and `foreign_key_list` PRAGMAs as typed rows, which `GetSchema()` uses. It sends each SQL-builder statement as its own command, and `BulkInsert` throws `NotImplementedException`. Most code obtains a `SqliteDAC` from `Tools.Sqlite.Open` or `Tools.Sqlite.Create` in the project root rather than constructing one.

`SqliteJournalMode` and `SqliteSyncMode` are the Framework's names for the provider's journal and synchronous modes. `SqliteHelper` converts them to and from the System.Data.SQLite enums. `Tools.Sqlite.CreateConnectionString` and the Windows Forms connection editors use it to read and write connection strings. `SqliteSqlHint` lists table and conflict clauses, and no code in the repository reads it yet. The [project README](../README.md) describes scopes, transactions, schema reading and the SQL dialect in detail.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`SqliteDAC`](SqliteDAC.cs) | class | `DACBase` for SQLite that creates `SQLiteConnection` objects, uses `SqliteSQLBuilder` and adds typed `sqlite_master`, `sqlite_sequence` and PRAGMA readers. |
| [`SqliteHelper`](SqliteHelper.cs) | static class | Converts `SqliteJournalMode` to and from `SQLiteJournalModeEnum`, and `SqliteSyncMode` to and from `SynchronizationModes`. |
| [`SqliteJournalMode`](SqliteJournalMode.cs) | enum | Journal mode of a connection, one of `Default`, `Delete`, `Memory`, `Off`, `Persist`, `Truncate` or `Wal`. |
| [`SqliteSqlHint`](SqliteSqlHint.cs) | enum | SQLite table and conflict clauses, `WithoutRowid` and the five `OnConflict` resolutions, not yet read by any code. |
| [`SqliteSyncMode`](SqliteSyncMode.cs) | enum | Synchronous mode of a connection, one of `Full`, `Normal` or `Off`. |
