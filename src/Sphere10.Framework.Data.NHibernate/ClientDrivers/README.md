# Client Drivers

Provides an NHibernate SQL Server client driver that widens unsigned and byte parameter types to types SQL Server can store.

`ExtendedSql2008ClientDriver` derives from NHibernate's `Sql2008ClientDriver` and changes each command parameter's SQL type before NHibernate initializes it. `byte` becomes `Int16`, `ushort` becomes `Int32`, `uint` becomes `Int64` and `ulong` becomes `Decimal`. Pair it with [`ExtendedMssqlDialect`](../Dialects/README.md), which maps the same CLR types to `SMALLINT`, `INT`, `BIGINT` and `DECIMAL(28)` columns, so parameters and columns agree.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ExtendedSql2008ClientDriver`](ExtendedSql2008ClientDriver.cs) | class | `Sql2008ClientDriver` that maps `Byte`, `UInt16`, `UInt32` and `UInt64` parameters to `Int16`, `Int32`, `Int64` and `Decimal` |
