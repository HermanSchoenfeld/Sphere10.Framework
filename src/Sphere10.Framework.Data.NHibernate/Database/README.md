# Database Managers

Provides the NHibernate database-manager contract and base classes that create a schema, seed it and open session factories.

`NHDatabaseManagerBase` decorates a provider's `IDatabaseManager` from [Sphere10.Framework.Data](../../Sphere10.Framework.Data/README.md), which creates and drops the empty database. Subclasses supply `GetFluentConfig(connectionString)` and `CreateDataGenerator(...)`. `CreateApplicationDatabase` builds a session factory whose configuration runs `SchemaExport.Create`, calls `OnDatabaseSchemasCreated` and then runs the data generator for the `PrimingData` and `DemoData` policies.

`OpenDatabase(connectionString)` builds a new `ISessionFactory` on every call, so callers should keep the factory they get. `NHDataGeneratorBase` saves the objects from `CreateData()` with `SaveOrUpdate` inside one session and transaction, and creates [type-table](../DataPatterns/README.md) rows from enums. [`DataAccessScope`](../Scopes/README.md) uses an `INHDatabaseManager` to open its session.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`INHDatabaseManager`](INHDatabaseManager.cs) | interface | `IDatabaseManager` that also opens a database as an NHibernate `ISessionFactory` |
| [`NHDatabaseManagerBase`](NHDatabaseManagerBase.cs) | abstract class | `DatabaseManagerDecorator` that creates the mapped schema with `SchemaExport`, runs the policy's data generator and builds session factories from the subclass's `FluentConfiguration` |
| [`NHDataGeneratorBase`](NHDataGeneratorBase.cs) | abstract class | `IDataGenerator` that persists `CreateData()` in one transaction and creates `TypeTable` rows for every value of an enum |
