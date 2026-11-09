# Dialects

Provides an NHibernate SQL Server dialect that maps unsigned and byte CLR types to wider signed column types.

`ExtendedMssqlDialect` derives from NHibernate's `MsSql2008Dialect` and registers `SMALLINT` for `byte`, `INT` for `ushort`, `BIGINT` for `uint` and `DECIMAL(28)` for `ulong`. Use it with [`ExtendedSql2008ClientDriver`](../ClientDrivers/README.md) so command parameters use the same widened types.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ExtendedMssqlDialect`](ExtendedMssqlDialect.cs) | class | `MsSql2008Dialect` that maps `Byte`, `UInt16`, `UInt32` and `UInt64` to `SMALLINT`, `INT`, `BIGINT` and `DECIMAL(28)` |
