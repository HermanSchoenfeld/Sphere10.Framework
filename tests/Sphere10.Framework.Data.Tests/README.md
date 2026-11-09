# Sphere10.Framework.Data.Tests

NUnit tests for the data-access library: `IDAC` scopes and transactions, the SQLite and SQL Server providers, SQL builder dialects, CSV and `DataSet` helpers, and the NHibernate `DataAccessScope`. The project references [Sphere10.Framework.Data](../../src/Sphere10.Framework.Data/README.md), [Sphere10.Framework.Data.NHibernate](../../src/Sphere10.Framework.Data.NHibernate/README.md) and [Sphere10.Framework.NUnit.DB](../../src/Sphere10.Framework.NUnit.DB/README.md), whose `DACTestFixture` creates a temporary database for each database-backed case.

## Run the tests

Run these commands from the repository root with the .NET 10 SDK. A machine with a .NET 11 SDK also builds and runs the `net11.0` target.

```powershell
dotnet test tests/Sphere10.Framework.Data.Tests/Sphere10.Framework.Data.Tests.csproj -c Release -p:BuildRevision=0
```

Run the connection-scope and transaction fixtures:

```powershell
dotnet test tests/Sphere10.Framework.Data.Tests/Sphere10.Framework.Data.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~DACScopeTests|FullyQualifiedName~TransactionScopeCompatibilityTests"
```

Run the SQLite fixtures only:

```powershell
dotnet test tests/Sphere10.Framework.Data.Tests/Sphere10.Framework.Data.Tests.csproj -c Release -p:BuildRevision=0 --filter "FullyQualifiedName~Sqlite"
```

## Requirements

The default run needs no database server. `DACTestFixture.DBMS` currently supplies only `DBMSType.Sqlite`, so database-backed cases create and drop temporary SQLite files. `MSSQLToolTest` is ignored by default. When it is enabled, it needs a SQL Server instance on `localhost` that accepts the `sa` login.

## Contents

### Project root

Fixtures for the core data-access library and its providers.

| Fixture | Purpose |
| --- | --- |
| [BoundDataViewEventTests](BoundDataViewEventTests.cs) | Typed `DataRow` deletion notifications, binding subscribers, row replacement and detachment on disposal of `BoundDataView`. |
| [CsvExportTests](CsvExportTests.cs) | CSV export framing, escaping of structural characters, multi-character delimiters, culture formatting and zero-column tables. |
| [CsvReaderDisposalTests](CsvReaderDisposalTests.cs) | A closed CSV data reader rejects reads, navigation and field access. |
| [DACDecoratorEventTests](DACDecoratorEventTests.cs) | `IDAC` decorators forward execution events with the original sender and SQL, and unsubscribe correctly. |
| [DACScopeTests](DACScopeTests.cs) | Connection reuse across nested and asynchronous scopes, commit, rollback and nested or consecutive transactions. |
| [DACTests](DACTests.cs) | Basic row insertion through a temporary database. |
| [DataAccessScopeDisposalTests](DataAccessScopeDisposalTests.cs) | NHibernate `DataAccessScope` asynchronous disposal and close or flush failures. |
| [ExtractedDataSetTests](ExtractedDataSetTests.cs) | Binary and XML `DataSet` stream formats, legacy namespace reading, subset schemas, truncation errors and `BoundDataView` editing. |
| [MSSQLProviderCompatibilityTests](MSSQLProviderCompatibilityTests.cs) | SQL Server connection strings, Entra authentication providers, encryption defaults, application intent and network library prefixes, without opening a connection. |
| [MSSQLToolTest](MSSQLToolTest.cs) | `Tools.MSSQL` database existence, creation and drop against a live server (ignored by default). |
| [SQLBuilderDialectTests](SQLBuilderDialectTests.cs) | Paging, `DISTINCT`, binary literals and statement terminators in the Firebird, SQL Server, MySQL, PostgreSQL and Oracle builders. |
| [SqliteDACTests](SqliteDACTests.cs) | SQLite connections, `Guid` round trips and schema text lengths against an embedded test database. |
| [SqliteSQLBuilderTests](SqliteSQLBuilderTests.cs) | `CAST` emission through the `ISQLBuilder` interface. |
| [SqliteToolTest](SqliteToolTest.cs) | `Tools.Sqlite` existence, creation options, passwords, page sizes, drop and connection-string creation. |
| [TestTables](Tables.cs) | Shared `TableSpecification` definitions used by the database fixtures (helper, not a fixture). |
| [TransactionScopeCompatibilityTests](TransactionScopeCompatibilityTests.cs) | `System.Transactions.TransactionScope` with automatic and manual enlistment, mixed scopes and enlistment errors. |

### Json

JSON serializer tests.

| Fixture | Purpose |
| --- | --- |
| [Sphere10FrameworkJsonSerializerTests](Json/Sphere10JsonSerializerTests.cs) | `Sphere10FrameworkJsonSerializer` serializes `null` as the JSON literal. |

### Resources

[TestDatabase.db3](Resources/TestDatabase.db3) is the SQLite database embedded through `Properties/Resource.resx` and copied out by `SqliteDACTests`. The folder contains no fixtures.

### Xml

[SchedulerXmlSerializerTest.cs](Xml/SchedulerXmlSerializerTest.cs) contains a commented-out scheduler serialization fixture. It compiles to no test.
