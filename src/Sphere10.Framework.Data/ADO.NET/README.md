# ADO.NET

Decorators and helpers over `System.Data` connections, transactions, readers and file-backed `DataSet` persistence.

The three abstract decorators forward every member of `IDbConnection`, `IDbTransaction` and `IDataReader` to a wrapped instance, so a subclass overrides only the members it changes. `RestrictedConnection` and `RestrictedTransaction` are the wrappers that [`DACScope`](../Scopes/README.md) hands out. They throw on `Close`, `Dispose`, `BeginTransaction`, `Commit` and `Rollback`, and the scope drives the real objects through the `*Internal` methods instead. `AutoClosingDataReader` closes its reader and disposes the command that produced it when the reader is disposed.

`FileDataSetBase` stores a `DataSet` in a file together with its schema version and format (`StreamDataSetFormat.XML` or `StreamDataSetFormat.Binary`). Subclasses supply the version and create the `DataSet` for a stored version. The data access components that use these types live in [DAC](../DAC/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AutoClosingDataReader`](AutoClosingDataReader.cs) | class | Data reader decorator that closes the reader and disposes its owning `IDbCommand` on `Dispose`. |
| [`BinaryDataSetInvalidException`](BinaryDataSetInvalidException.cs) | class | Thrown when binary `DataSet` data is not valid. |
| [`BinaryDataSetVersionException`](BinaryDataSetVersionException.cs) | class | Thrown when the version of binary `DataSet` data is not supported. |
| [`DataReaderDecorator`](DataReaderDecorator.cs) | abstract class | Forwards every `IDataReader` member to a wrapped reader through virtual members. |
| [`DateTimeInterval`](DateTimeInterval.cs) | enum | Calendar interval used by `Tools.Data.SampleDataByInterval` when sampling `DataView` rows. |
| [`DbConnectionDecorator`](DbConnectionDecorator.cs) | abstract class | Forwards every `IDbConnection` member to a wrapped connection through virtual members. |
| [`DbTransactionDecorator`](DbTransactionDecorator.cs) | abstract class | Forwards every `IDbTransaction` member to a wrapped transaction through virtual members. |
| [`FileDataSetBase`](FileDataSetBase.cs) | abstract class | Saves and loads a `DataSet` file with its schema version and selected payload format. |
| [`RestrictedConnection`](RestrictedConnection.cs) | class | Scope-owned connection that rejects direct close, dispose and transaction creation. |
| [`RestrictedTransaction`](RestrictedTransaction.cs) | class | Scope-owned transaction that rejects direct commit, rollback and dispose and tracks its outcome. |
| [`StreamDataSetFormat`](StreamDataSetFormat.cs) | enum | Selects the XML or compact binary format for `DataSet` streams. |
