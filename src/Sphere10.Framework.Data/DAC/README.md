# DAC

The `IDAC` data access component contract, its base class and decorators, a provider factory, and the query extension methods built on it.

An `IDAC` executes SQL against one connection string. It creates connections and dialect-specific [SQL builders](../SQLBuilder/README.md), runs queries, inserts, updates, deletes and bulk inserts, and reads the database [schema](../Schema/README.md). `DACBase` implements the execution members on top of `CreateConnection` and `CreateSQLBuilder`. Every call enters a [`DACScope`](../Scopes/README.md), so commands issued inside an enclosing scope share its connection and transaction while `UseScopeOsmosis` is `true`, which is the default. Provider packages such as `Sphere10.Framework.Data.Sqlite` and `Sphere10.Framework.Data.MSSQL` supply the concrete DAC classes. `DACFactory.CreateDAC` loads them by name from a `DBMSType`, so the provider assembly must be deployed with the application.

`DACDecorator` forwards every member to an inner DAC. `AutoIdentityDAC` uses it to return or generate primary key values on insert, and `AmbientDAC` uses it to run commands on a connection and transaction supplied by the caller. `IDACExtensions` adds scope entry, typed scalar reads, `Select`, `Count`, `Any` and `DataRow` save operations. `IDACAsyncExtensions` provides `*Async` forms of those operations that run the synchronous call through `Task.Run`. `SchemaCache` caches `GetSchema` results per connection string.

```csharp
var dac = DACFactory.CreateDAC(DBMSType.Sqlite, connectionString);
using (var scope = dac.EnterScope()) {
	scope.BeginTransaction();
	dac.Insert("Customer", new[] { new ColumnValue("Name", "Alice") });
	scope.Commit();
}
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AmbientDAC`](AmbientDAC.cs) | class | DAC decorator that executes on caller-supplied connections and transactions which it never closes or disposes. |
| [`AutoIdentityDAC`](AutoIdentityDAC.cs) | class | DAC decorator whose `Insert` returns the primary key and can generate it by DBMS auto-increment or by calculated increment or decrement. |
| [`DACBase`](DACBase.cs) | abstract class | Implements `IDAC` execution, CRUD and logging on top of provider-specific connection and SQL builder factories. |
| [`DACDecorator`](DACDecorator.cs) | abstract class | Forwards every `IDAC` member and event to a decorated DAC. |
| [`DACFactory`](DACFactory.cs) | static class | Creates the provider DAC for a `DBMSType` by activating its type from the provider assembly. |
| [`DACTool`](DACTool.cs) | static class | Resolves primary key column names and values from an `AutoIdentityDAC.PrimaryKeyConvention`. |
| [`IDAC`](IDAC.cs) | interface | Contract for executing SQL, CRUD, bulk insert, system-transaction enlistment and schema reads on one connection string. |
| [`IDACAsyncExtensions`](IDACAsyncExtensions.cs) | static class | `Task`-returning forms of the `IDAC` query, count, select and CRUD operations that wrap the synchronous calls in `Task.Run`. |
| [`IDACExtensions`](IDACExtensions.cs) | static class | Adds `EnterScope`, `BeginDirtyReadScope`, `ExecuteScalar<T>`, `ExecuteQuery`, `Select`, `Count`, `Any`, `GetMaxID` and `DataRow` insert, update and save to `IDAC`. |
| [`SchemaCache`](SchemaCache.cs) | static class | Caches `IDAC.GetSchema` results per connection string and invalidates them on request. |

`AutoIdentityDAC.AutoIdentityPolicy` and `AutoIdentityDAC.PrimaryKeyConvention` are public enums nested in `AutoIdentityDAC`.
