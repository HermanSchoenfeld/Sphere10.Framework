# Database

Database lifecycle contracts for creating, checking, populating and dropping application databases, plus the `DBMSType` provider enumeration.

`IDatabaseManager` builds connection strings and creates, checks and drops databases for one DBMS. `DatabaseManagerBase` implements the event plumbing. `CreateEmptyDatabase` and `DropDatabase` call the provider's `*Internal` hook and then raise `DatabaseCreated` or `DatabaseDropped`. Subclasses that build schemas call `NotifyDatabaseSchemasCreated` to raise `DatabaseSchemasCreated`. `DatabaseManagerDecorator` forwards every member to an inner manager so an application can add behaviour such as schema creation or data priming around a provider implementation.

`DatabaseGenerationDataPolicy` tells `CreateApplicationDatabase` whether to create no data, priming data or demo data, and `IDataGenerator` is the contract for code that populates that data. `DBMSType` identifies the provider and is used by [`DACFactory`](../DAC/README.md) and by schema [hints](../SQLBuilder/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DatabaseCreatedEventArgs`](DatabaseCreatedEventArgs.cs) | record | Event data carrying the connection string of a newly created database. |
| [`DatabaseGenerationAlreadyExistsPolicy`](DatabaseGenerationAlreadyExistsPolicy.cs) | enum | Whether generation into an existing database errors, appends or overwrites. |
| [`DatabaseGenerationDataPolicy`](DatabaseGenerationDataPolicy.cs) | enum | Whether a generated application database receives no data, priming data or demo data. |
| [`DatabaseManagerBase`](DatabaseManagerBase.cs) | abstract class | Base `IDatabaseManager` that raises created, schemas-created and dropped events around provider hooks. |
| [`DatabaseManagerDecorator`](DatabaseManagerDecorator.cs) | class | Forwards every `IDatabaseManager` member and event to an inner manager for layering behaviour. |
| [`DatabaseSchemasCreatedEventArgs`](DatabaseSchemasCreatedEventArgs.cs) | record | Event data carrying the connection string of a database whose schemas were created. |
| [`DBMSType`](DBMSType.cs) | enum | Supported database systems: SQL Server, Sqlite, Firebird, embedded Firebird, PostgreSQL, MySQL and Oracle. |
| [`EmptyDataGenerator`](EmptyDataGenerator.cs) | class | `IDataGenerator` whose `Populate` adds nothing. |
| [`IDatabaseManager`](IDatabaseManager.cs) | interface | Contract for generating connection strings and creating, checking and dropping databases. |
| [`IDataGenerator`](IDataGenerator.cs) | interface | Contract for code that populates a database with `Populate()`. |
