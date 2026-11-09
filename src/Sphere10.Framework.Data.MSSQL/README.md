<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.MSSQL

`Sphere10.Framework.Data.MSSQL` connects the shared `IDAC` data-access abstraction of [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) to Microsoft SQL Server and Azure SQL Database through Microsoft.Data.SqlClient. Use it when an application built on the shared DAC, scope and SQL-builder APIs needs to read, write, bulk-load or administer SQL Server databases.

The package supplies `MSSQLDAC`, the `Tools.MSSQL` connection and administration helpers and `MSSQLDatabaseManager`, and SQL text comes from `MSSQLBuilder` in the shared package.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.MSSQL
```

The package depends on `Microsoft.Data.SqlClient`, `Microsoft.Data.SqlClient.Extensions.Azure`, `System.Data.SqlClient`, `Sphere10.Framework.Data` and `Sphere10.Framework`. The project targets `net10.0`, and a build with a .NET 11 SDK also targets `net11.0`. To build against source instead, reference `src/Sphere10.Framework.Data.MSSQL/Sphere10.Framework.Data.MSSQL.csproj` with a `ProjectReference`.

- `Microsoft.Data.SqlClient.Extensions.Azure` supplies the Microsoft Entra authentication providers that SqlClient 7 ships separately, so connection strings with `Authentication=Active Directory ...` keep a built-in provider. See [Microsoft Entra authentication in SqlClient](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql/azure-active-directory-authentication?view=sql-server-ver17).
- `System.Data.SqlClient` remains only because `Tools.MSSQL` still accepts its obsolete `ApplicationIntent` enum.

The examples need an existing server, database and credentials.

## ⚡ Open a scope and query

Set `MSSQL_CONNECTION_STRING` to your application's connection string. The remaining examples reuse `dac` and these imports.

```csharp
using System;
using System.Data;
using Sphere10.Framework;
using Sphere10.Framework.Data;

var connectionString = Environment.GetEnvironmentVariable("MSSQL_CONNECTION_STRING");
Guard.ArgumentNotNullOrEmpty(connectionString, nameof(connectionString));
var dac = new MSSQLDAC(connectionString);

using (dac.EnterScope()) {
	var serverVersion = dac.ExecuteScalar<string>("SELECT @@VERSION");
	Console.WriteLine(serverVersion);
}
```

Constructing a DAC, directly or through `Tools.MSSQL.Open(...)`, only stores its configuration. `EnterScope()` opens the connection by default, and the outermost scope owns that connection until it is disposed. The DAC itself is not disposable.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Data.MSSQL` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`MSSQLDAC`](MSSQLDAC.cs) | class | `DACBase` for SQL Server over `Microsoft.Data.SqlClient`, with `SqlBulkCopy` bulk inserts, an `IsAzure()` check and batches wrapped in `SET XACT_ABORT ON` and one transaction. |
| [`MSSQLDatabaseManager`](MSSQLDatabaseManager.cs) | class | `DatabaseManagerBase` that tests, creates and drops SQL Server databases through `Tools.MSSQL`. |
| [`Tools.MSSQL`](MSSQLTool.cs) | tools | Opens an `MSSQLDAC`, builds connection strings, tests whether a database exists, creates and drops databases through `master` and clears connection pools. |
<!-- domains:end -->

## 🧩 Public API

`Tools.MSSQL` is in the global `Tools` namespace, and the other types are in `Sphere10.Framework.Data`.

| API | Purpose |
|---|---|
| `MSSQLDAC(connectionString, logger)` | The provider's `DACBase` implementation of `IDAC`. |
| `Tools.MSSQL.Open(dataSource, initialCatalog, ...)` | Builds a connection string from separate settings and returns an `MSSQLDAC`. |
| `Tools.MSSQL.CreateConnectionString(...)` | Returns a provider connection string without opening a connection. |
| `EnterScope()` | Returns a `DACScope`. The outermost scope owns the connection, and nested scopes share it. Call `BeginTransaction` on the scope for a local transaction. |
| `ExecuteScalar<T>(sql)`, `ExecuteQuery(sql)` | Return a converted scalar value or a materialized `DataTable`. |
| `ExecuteNonQuery(sql)`, `ExecuteReader(sql)` | Execute a statement, or return a reader to consume inside an outer scope. |
| `ExecuteBatch(builder)` | Sends consecutive DML statements as one batch and each DDL statement as its own batch, between `SET XACT_ABORT ON`, `BEGIN TRANSACTION` and `COMMIT TRANSACTION`. |
| `Insert`, `Update`, `Delete` | Build SQL with `MSSQLBuilder` and return the affected-row count. |
| `BulkInsert(table, options, timeout)` | Copies a `DataTable` with `SqlBulkCopy`, as described below. |
| `IsAzure()` | Returns `true` when `SERVERPROPERTY('EngineEdition')` is `5`, which identifies Azure SQL Database. |
| `CreateConnection()`, `CreateOpenConnection()` | Create a caller-owned `Microsoft.Data.SqlClient.SqlConnection`, closed or opened. |
| `CreateSQLBuilder()` | Returns a new `MSSQLBuilder` as `ISQLBuilder`. |
| `GetSchema()` | Reads table, column, key and trigger metadata, as described below. |
| `MSSQLDatabaseManager` | Implements the shared `DatabaseManagerBase` contract for SQL Server. |
| `DACFactory.CreateDAC(DBMSType.SQLServer, connectionString)` | Creates an `MSSQLDAC` by type name, so that an application can choose the provider at run time. |

Code that casts a connection to its concrete type must use `Microsoft.Data.SqlClient.SqlConnection`, not the `System.Data.SqlClient` type.

## 🔧 Connection configuration

For separate configuration fields, use the helper:

```csharp
var configuredDac = Tools.MSSQL.Open(
	dataSource: "localhost", initialCatalog: "appdb", integratedSecurity: true,
	encrypt: true, trustServerCertificate: false,
	connectTimeout: TimeSpan.FromSeconds(15), applicationName: "ExampleApp"
);

// A named instance with a SQL Server login and a larger connection pool.
var expressDac = Tools.MSSQL.Open(
	dataSource: @"localhost\SQLEXPRESS", initialCatalog: "appdb",
	userID: "appuser", password: Environment.GetEnvironmentVariable("MSSQL_PASSWORD"),
	pooling: true, minPoolSize: 10, maxPoolSize: 100
);

// A server on another port, as a connection string.
var remoteConnectionString = Tools.MSSQL.CreateConnectionString(server: "db.example.com", initialCatalog: "appdb", integratedSecurity: true, encrypt: true, port: 14330);

// Azure SQL Database with Microsoft Entra authentication.
var azureDac = new MSSQLDAC("Server=tcp:example.database.windows.net,1433;Database=appdb;Authentication=Active Directory Default;Encrypt=True;");
```

`CreateConnectionString` takes the same settings except `networkLibrary` and `logger`, and it names the first one `server`. It also takes a `port`, which `Open` lacks, and appends it to the server as `, port`. Omitted settings keep the provider defaults, except encryption.

| Option | Mapping |
|---|---|
| `dataSource`, `initialCatalog` | `DataSource` and `InitialCatalog`. |
| `userID`, `password`, `integratedSecurity` | SQL Server authentication or Windows integrated security. |
| `encrypt` | `true` selects `Encrypt=Mandatory` and `false` selects `Optional`. Omitting it also selects `Optional`. |
| `trustServerCertificate` | `TrustServerCertificate`. |
| `pooling`, `minPoolSize`, `maxPoolSize` | Connection-pool settings. |
| `connectTimeout`, `loadBalanceTimeout` | `ConnectTimeout` and `LoadBalanceTimeout`, rounded to whole seconds. |
| `applicationIntent` | `ApplicationIntent`, given as the obsolete `System.Data.SqlClient.ApplicationIntent` enum and converted to the `Microsoft.Data.SqlClient` value. |
| `networkLibrary` | Only `Open` accepts it. Microsoft.Data.SqlClient has no `Network Library` setting, so `dbmssocn`, `dbnmpntw` and `dbmslpcn` become the `tcp:`, `np:` and `lpc:` prefix of the data source, and other libraries throw `NotSupportedException`. |
| `logger` | An optional framework `ILogger` for the DAC. Only `Open` accepts it. |
| Other options | `failoverPartner`, `attachDBFilename`, `persistSecurityInfo`, `enlist`, `multipleActiveResultSets`, `replication`, `packetSize`, `typeSystemVersion`, `applicationName`, `currentLanguage`, `workstationID`, `userInstance`, `transactionBinding` and `multiSubnetFailover` set the `SqlConnectionStringBuilder` properties of the same name. |

`MSSQLDAC.CreateConnection()` also selects `Encrypt=Optional` when a connection string omits `Encrypt`, and it preserves an explicit `True`, `False` or `Strict` setting. For `Strict` and for settings that the helper lacks, pass a complete connection string to `new MSSQLDAC(connectionString)`. Certificate validation follows Microsoft.Data.SqlClient, so an omitted `Encrypt` option does not bypass certificate validation when the server forces encryption.

The shared DAC sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`. `ExecuteScalar` keeps the provider's command default. To enforce a per-command timeout, create a command explicitly as below.

## 📝 Insert, query, update and delete

The DAC's `Insert`, `Update` and `Delete` methods and its `Select` and `Count` extensions build the SQL for you. The example keeps its work in one scope, so every statement uses the same connection:

```csharp
using (dac.EnterScope()) {
	dac.ExecuteNonQuery(@"CREATE TABLE products (
		product_id INT IDENTITY(1,1) PRIMARY KEY,
		name NVARCHAR(100) NOT NULL,
		price DECIMAL(10,2) NOT NULL,
		stock INT NOT NULL
	)");

	// SQL Server assigns the IDENTITY key, so the insert omits it.
	dac.Insert("products", new[] { new ColumnValue("name", "Laptop"), new ColumnValue("price", 999.99m), new ColumnValue("stock", 50) });

	// Insert returns the affected-row count, so read a generated key through OUTPUT.
	var mouseID = dac.ExecuteScalar<int>(dac.QuickString(
		"INSERT INTO products (name, price, stock) OUTPUT INSERTED.product_id VALUES ({0}, {1}, {2})",
		SQLBuilderCommand.Literal("Mouse"), SQLBuilderCommand.Literal(29.99m), SQLBuilderCommand.Literal(200)
	));

	foreach (DataRow row in dac.ExecuteQuery("SELECT name, price FROM products WHERE price > 100 ORDER BY price DESC").Rows)
		Console.WriteLine($"{row["name"]} costs {row["price"]}");

	var topTen = dac.Select("products", columns: new[] { "name", "price" }, orderByClause: "price DESC", limit: 10);
	var laptops = dac.ExecuteQuery("SELECT * FROM products WHERE name = {0}", SQLBuilderCommand.Literal("Laptop"));
	var totalStock = dac.ExecuteScalar<int>("SELECT SUM(stock) FROM products");
	var productCount = dac.Count("products");

	dac.Update("products", new[] { new ColumnValue("stock", 45), new ColumnValue("price", 1099.99m) }, new[] { new ColumnValue("name", "Laptop") });
	dac.Delete("products", new[] { new ColumnValue("product_id", mouseID) });
	dac.ExecuteNonQuery("DELETE FROM products WHERE stock < 10");
}
```

`Update` and `Delete` match each `ColumnValue` condition with `=`, or with `IN` when its value is a list. `ExecuteQuery(sql, args)` and `QuickString(sql, args)` fill `{0}` placeholders with `string.Format`, so wrap values in `SQLBuilderCommand.Literal` to have the builder quote and escape them. String literals have no `N` prefix, so bind text outside the database's code page as a parameter. The builder writes `decimal` and `double` literals in the current culture, so run under a culture whose decimal separator is a period, such as `CultureInfo.InvariantCulture`.

## 🔎 Bound parameters

The DAC's string-based methods execute SQL text. `ExecuteQuery(sql, params object[] args)` formats its arguments into the SQL through the builder, so it does not bind ADO.NET parameters. Create a command for bound values:

```csharp
using (var scope = dac.EnterScope()) {
	using var command = scope.Connection.CreateCommand();
	command.CommandText = "SELECT @value";
	command.CommandTimeout = 30;
	if (scope.Transaction is RestrictedTransaction transaction)
		command.Transaction = transaction.DangerousInternalTransaction;

	var parameter = command.CreateParameter();
	parameter.ParameterName = "@value";
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

The following money transfer assumes an `accounts` table with `account_id` and `balance` columns. It leaves its scope without committing when the balance check fails, so neither update is kept:

```csharp
try {
	using var scope = dac.EnterScope();
	scope.BeginTransaction();
	dac.ExecuteNonQuery("UPDATE accounts SET balance = balance - 1000 WHERE account_id = 1");
	if (dac.ExecuteScalar<decimal>("SELECT balance FROM accounts WHERE account_id = 1") < 0)
		throw new InvalidOperationException("Insufficient funds");
	dac.ExecuteNonQuery("UPDATE accounts SET balance = balance + 1000 WHERE account_id = 2");
	scope.Commit();
} catch (InvalidOperationException error) {
	Console.WriteLine($"Transfer abandoned: {error.Message}");
}
```

## 🧾 Stored procedures

`ExecuteNonQuery` sends its text as one batch, so it can create a procedure. Call the procedure as SQL text, or bind its parameters through a stored-procedure command:

```csharp
dac.ExecuteNonQuery(@"CREATE OR ALTER PROCEDURE get_products_up_to @maxPrice DECIMAL(10,2) AS
BEGIN
	SELECT product_id, name, price FROM products WHERE price <= @maxPrice ORDER BY price
END");

// Format the argument into the call as a literal.
var cheapProducts = dac.ExecuteQuery("EXEC get_products_up_to @maxPrice = {0}", SQLBuilderCommand.Literal(50m));

// Or bind it as a parameter of a stored-procedure command.
using (var scope = dac.EnterScope()) {
	using var command = scope.Connection.CreateCommand();
	command.CommandType = CommandType.StoredProcedure;
	command.CommandText = "get_products_up_to";
	if (scope.Transaction is RestrictedTransaction transaction)
		command.Transaction = transaction.DangerousInternalTransaction;

	var parameter = command.CreateParameter();
	parameter.ParameterName = "@maxPrice";
	parameter.DbType = DbType.Decimal;
	parameter.Value = 50m;
	command.Parameters.Add(parameter);

	using var reader = command.ExecuteReader();
	var products = reader.ToDataTable();
	Console.WriteLine(products.Rows.Count);
}

// A procedure without a result set runs through ExecuteNonQuery.
dac.ExecuteNonQuery(@"CREATE OR ALTER PROCEDURE restock_products @minimum INT AS
BEGIN
	UPDATE products SET stock = @minimum WHERE stock < @minimum
END");
dac.ExecuteNonQuery("EXEC restock_products @minimum = 20");
```

`CREATE OR ALTER` needs SQL Server 2016 SP1 or later, or Azure SQL Database. `ToDataTable()` is the shared `IDataReader` extension that `ExecuteQuery` also uses.

## 📥 Bulk insert

`BulkInsert` copies a `DataTable` into the table named by its `TableName`. It uses the current scope's connection and transaction, so it joins an outer scope and its transaction.

```csharp
var rows = new DataTable("app_counters");
rows.Columns.Add("id", typeof(int));
rows.Columns.Add("counter_value", typeof(int));
rows.Rows.Add(3, 0);
rows.Rows.Add(4, 0);

dac.BulkInsert(rows, BulkInsertOptions.Default, TimeSpan.FromMinutes(1));
```

`BulkInsertOptions.KeepIdentity` maps to `SqlBulkCopyOptions.KeepIdentity`, and `BulkInsertOptions.MaintainForeignKeys` maps to `SqlBulkCopyOptions.CheckConstraints`. No column mappings are set, so the `DataTable` columns must match the destination columns by position. The timeout becomes `SqlBulkCopy.BulkCopyTimeout`, rounded to whole seconds.

## 🗂️ Schema and SQL dialect

`GetSchema()` reads `sys.tables`, `sys.columns`, `sys.types`, `sys.indexes`, `sys.index_columns`, `sys.foreign_keys` and `sys.foreign_key_columns` for the connected database, and trigger names from `sys.triggers`. The result uses the shared `DBSchema` model of tables, columns, keys and trigger names. Identity columns are detected from `is_identity`, precision and scale come from the column's declaration in `sys.columns`, and sequence fields are not populated.

Table names are reported without their schema, so equally named tables in different schemas are merged into one entry when you compare schemas.

```csharp
var schema = dac.GetSchema();
foreach (var table in schema.Tables)
	Console.WriteLine(table.Name);
```

`MSSQLBuilder` is implemented in the shared data project:

| Builder behavior | Implementation |
|---|---|
| Identifier quoting | Square brackets. Names that start with `SYS.` are emitted unquoted. |
| Temporary and in-memory tables | `TableType.Temporary` prefixes the table name with `#`, and `TableType.InMemory` prefixes it with `@`. |
| Paging | `Select(limit: n)` emits `TOP n`, after `DISTINCT` when that is requested. With an offset, `Select` wraps the query in a `ROW_NUMBER()` CTE ordered by `orderByClause`, or by `@@IDENTITY` when that is omitted. An offset without a limit throws `NotSupportedException`. |
| Generated identity expression | `SCOPE_IDENTITY()`. |
| Sequence expression | `NextSequenceValue` throws `NotSupportedException`. |
| Transaction SQL | `SET XACT_ABORT ON` with `BEGIN TRANSACTION`, then `COMMIT TRANSACTION` or `ROLLBACK TRANSACTION`. `ExecuteBatch` replaces these statements with its own. |
| Variables | `DeclareVariable` emits `DECLARE @name TYPE`, `AssignVariable` emits `SET @name = ...` and `VariableName` emits `@name`. |
| Binary literals | `CONVERT(varbinary(max), ..., 1)`. |
| Statement terminator | `;`. `ToString()` also puts a `GO` line before each DDL statement. |
| Auto-increment toggles | `DisableAutoIncrementID` and `EnableAutoIncrementID` emit `SET IDENTITY_INSERT ... ON` and `OFF` when the given table has an identity column. |

`GO` is a client batch separator rather than T-SQL, so run DDL builders through `ExecuteBatch` instead of passing their `ToString()` script to `ExecuteNonQuery`.

`Insert`, `Update` and `Delete` render `ColumnValue` values as SQL literals rather than parameters, and `Insert` returns the affected-row count rather than a generated key.

## 🛠️ Database administration

`MSSQLDatabaseManager` implements the shared `DatabaseManagerBase` contract. `GenerateConnectionString` calls `Tools.MSSQL.CreateConnectionString`, and `DatabaseExists`, `CreateEmptyDatabase` and `DropDatabase` read the server, database and credentials from the connection string and call the helpers below.

| Helper | Behavior |
|---|---|
| `Tools.MSSQL.TestConnectionString(connectionString)` | Opens and closes a scope, and returns `false` instead of throwing when that fails. |
| `Tools.MSSQL.Exists(server, databaseName, userID, password, integratedSecurity)` | Connects to `master` and checks `sys.databases`. |
| `Tools.MSSQL.CreateDatabase(server, databaseName, username, password, useIntegratedSecurity, existsPolicy)` | Creates the database and, outside Azure SQL Database, sets `RECOVERY SIMPLE`. |
| `Tools.MSSQL.DropDatabase(server, databaseName, username, password, useIntegratedSecurity, throwIfNotExists)` | Drops the database. Outside Azure SQL Database, it first switches the database to `SINGLE_USER WITH ROLLBACK IMMEDIATE`, which disconnects other sessions. |
| `Tools.MSSQL.GetDatabaseNameFromConnectionString(connectionString)` | Returns the connection string's `InitialCatalog`. |
| `Tools.MSSQL.ClearAllPools()` | Calls `SqlConnection.ClearAllPools()`. |

The helpers take the server and the credentials of a login that may create databases:

```csharp
var server = "localhost";
var adminUser = Environment.GetEnvironmentVariable("MSSQL_ADMIN_USER");
var adminPassword = Environment.GetEnvironmentVariable("MSSQL_ADMIN_PASSWORD");

Tools.MSSQL.CreateDatabase(server, "appdb", adminUser, adminPassword, existsPolicy: AlreadyExistsPolicy.Skip);
Console.WriteLine(Tools.MSSQL.Exists(server, "appdb", adminUser, adminPassword)); // True
Tools.MSSQL.DropDatabase(server, "appdb", adminUser, adminPassword, throwIfNotExists: false);

// The same operations through the shared database-manager contract.
var manager = new MSSQLDatabaseManager();
var connectionString = manager.GenerateConnectionString(server, "appdb", adminUser, adminPassword, null);
if (!manager.DatabaseExists(connectionString))
	manager.CreateEmptyDatabase(connectionString);
```

`CreateDatabase` takes an `existsPolicy` that defaults to `AlreadyExistsPolicy.Error`. `Skip` keeps an existing database, and `Overwrite` drops it the same way as `DropDatabase` before recreating it. `DropDatabase` throws when the database is missing unless `throwIfNotExists` is `false`. These calls need permission to create and drop databases. Database names are formatted into the SQL text, so pass only trusted, validated names.

`Exists`, `CreateDatabase` and `DropDatabase`, and therefore the manager, build their `master` connection from the server name and credentials alone, so encryption, certificate and other settings are not forwarded. `CreateApplicationDatabase` throws `NotSupportedException`, so create application tables, migrations and seed data in the application.

## 🚧 Implementation boundaries

- The shared `IDACAsyncExtensions`, including `BulkInsertAsync`, run the synchronous DAC methods through `Task.Run`, so they do not use the driver's asynchronous I/O.
- The package supplies no ORM, schema generator or migration system.

## 🧪 Tests

`MSSQLProviderCompatibilityTests` in [Sphere10.Framework.Data.Tests](../../tests/Sphere10.Framework.Data.Tests) checks connection-string encryption, the legacy `ApplicationIntent` and `networkLibrary` mappings and the Microsoft Entra providers without a server. `MSSQLToolTest` covers the administration helpers against a SQL Server at `localhost` with the `sa` login, and it is marked `[Ignore]`.

```bash
dotnet test tests/Sphere10.Framework.Data.Tests/Sphere10.Framework.Data.Tests.csproj -c Release --filter "FullyQualifiedName~MSSQLProviderCompatibilityTests"
```

## 🔗 Related projects and source

- [Sphere10.Framework.Data](../Sphere10.Framework.Data/README.md) — shared DAC contracts, scopes, SQL builders and schema types.
- [Sphere10.Framework](../Sphere10.Framework/README.md) — guards, logging and the transactional scope base classes.
- [Sphere10.Framework.Windows.Forms.MSSQL](../Sphere10.Framework.Windows.Forms.MSSQL/README.md) — Windows Forms connection editors for SQL Server.
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) — NHibernate integration for the shared data layer, including `ExtendedMssqlDialect`.
- [MSSQLDAC](MSSQLDAC.cs), [Tools.MSSQL](MSSQLTool.cs), [MSSQLDatabaseManager](MSSQLDatabaseManager.cs) and the shared [MSSQLBuilder](../Sphere10.Framework.Data/SQLBuilder/MSSQLBuilder.cs) — provider and dialect implementation.

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Data.MSSQL/Sphere10.Framework.Data.MSSQL.csproj -c Release
```

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
