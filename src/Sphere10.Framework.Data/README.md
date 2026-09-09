# 💾 Sphere10.Framework.Data

<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**Database-agnostic data access layer** providing the `IDAC` interface, SQL builders, transaction scopes, schema introspection, and data format utilities for CSV, JSON, and XML.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data
```

Targets **.NET 10** (`net10.0`). For database-specific support, also install the appropriate provider package:

```bash
dotnet add package Sphere10.Framework.Data.Sqlite    # SQLite
dotnet add package Sphere10.Framework.Data.MSSQL     # SQL Server
dotnet add package Sphere10.Framework.Data.Firebird  # Firebird
dotnet add package Sphere10.Framework.Data.MySQL     # MySQL
dotnet add package Sphere10.Framework.Data.PostgreSQL # PostgreSQL
dotnet add package Sphere10.Framework.Data.Oracle    # Oracle
```

Examples are independent snippets using a configured `IDAC dac` from a provider package. Query and CRUD examples assume the named tables and columns already exist; creation examples require unused names. `dataRow`, `myObject`, `MyEntity` and `MyClass` represent application data and model types.

## 🏗️ Core Architecture

### IDAC Interface

The **Data Access Context (DAC)** is the central abstraction for database operations. The `IDAC` interface provides:

| Member | Description |
|--------|-------------|
| `CreateConnection()` | Creates a closed, caller-owned `IDbConnection` |
| `CreateSQLBuilder()` | Creates a database-specific `ISQLBuilder` |
| `ExecuteNonQuery(query)` | Executes DDL/DML returning affected row count |
| `ExecuteScalar(query)` | Executes query returning single value |
| `ExecuteReader(query)` | Returns a disposable reader; keep an outer DAC scope alive while consuming it |
| `ExecuteBatch(sqlBuilder)` | Executes batched statements from SQL builder |
| `Insert(table, values)` | Standard provider DACs return the affected-row count |
| `Update(table, setValues, whereValues)` | Updates matching rows |
| `Delete(table, matchColumns)` | Deletes matching rows |
| `GetSchema()` | Returns the metadata exposed by the provider as a `DBSchema` |
| `BulkInsert(table, options, timeout)` | Provider-specific bulk insertion; some providers throw `NotImplementedException` |

`AutoIdentityDAC` is a separate decorator that can return a generated key according to its policy and the underlying SQL dialect. That behavior is not the return contract of `DACBase.Insert`.

### DACScope - Connection and Transaction Management

The DAC holds configuration and is not disposable. `DACScope` owns the connection and any local transaction it creates. `BeginScope()` opens the connection by default; constructing the DAC does not. Common imports for these examples are:

```csharp
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Transactions;
using Sphere10.Framework;
using Sphere10.Framework.Data;

// Opens a connection scope (reuses connection if nested)
using (var scope = dac.BeginScope(openConnection: true)) {
	// All operations share this connection
	dac.Insert("Users", new[] { new ColumnValue("ID", 1) });
	dac.Insert("Users", new[] { new ColumnValue("ID", 2) });
}
// The owning scope closes and disposes the connection
```

**Transaction Support:**

```csharp
using (var scope = dac.BeginScope()) {
	scope.BeginTransaction();

	dac.Insert("BasicTable", new[] { new ColumnValue("ID", 1) });
	dac.Insert("BasicTable", new[] { new ColumnValue("ID", 2) });

	scope.Commit();  // Explicit commit required
}
// Disposal releases the transaction and connection; it does not commit
```

**Nested Scopes:**

With `UseScopeOsmosis = true` (the default), nested scopes with the same connection string share their connection and transaction. Setting it to `false` separates different DAC instances; scopes on the same instance can still nest. These are shared transactions, not savepoints:

```csharp
using (var outerScope = dac.BeginScope()) {
	outerScope.BeginTransaction();

	using (var innerScope = dac.BeginScope()) {
		// Same connection as outerScope
		innerScope.BeginTransaction();  // Joins the existing transaction
		dac.Insert("Table", new[] { new ColumnValue("ID", 1) });
		innerScope.Commit();
	}

	outerScope.Rollback();  // Rolls back everything including inner commits
}
```

A nested `Commit` does not commit the database transaction independently. An explicit nested `Rollback` votes for rollback when the owning scope commits. Call `Commit` or `Rollback` deliberately; simply disposing a non-owning inner scope is not an explicit rollback vote.

**TransactionScope Integration:**

When a DAC scope opens its own connection inside an ambient `System.Transactions.TransactionScope`, it asks the provider to enlist. Local and distributed transaction support depends on the provider and platform. Let the ambient scope control completion; do not also call the DAC scope's `BeginTransaction`, `Commit` or `Rollback`:

```csharp
using (var transactionScope = new TransactionScope(TransactionScopeOption.Required)) {
	using (dac.BeginScope()) {
		dac.Insert("Users", new[] { new ColumnValue("ID", 1) });
	}
	transactionScope.Complete();
}
```

### ColumnValue - Column/Value Pairs

`ColumnValue` represents a column name and value pair for builder-based CRUD operations. These values are rendered as SQL literals; they are not ADO.NET command parameters. Treat identifiers and raw SQL fragments as trusted application configuration:

```csharp
// Insert with ColumnValue array
dac.Insert("Users", new[] {
	new ColumnValue("ID", 1),
	new ColumnValue("Name", "Alice"),
	new ColumnValue("Email", "alice@example.com")
});

// Update using setValues and whereValues
dac.Update("Users",
	setValues: new[] { new ColumnValue("Name", "Alice Smith") },
	whereValues: new[] { new ColumnValue("ID", 1) }
);

// Delete matching rows
dac.Delete("Users", new[] { new ColumnValue("ID", 1) });
```

### Bound Command Parameters

For bound values, create an ADO.NET command through the scope. This scalar example uses the `@value` placeholder supported by SQL Server, MySQL, PostgreSQL and SQLite. For Oracle, use `SELECT :value FROM DUAL` and the parameter name `value`.

```csharp
using (var scope = dac.BeginScope()) {
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
	var result = command.ExecuteScalar();
}
```

The transaction assignment attaches a manually created command to the scope's local transaction; DAC execution methods do this internally. Dispose commands and readers before the scope ends, and let the scope close its connection. `CreateOpenConnection()` is an alternative that returns a separate, caller-owned open connection.

## 🔧 DAC Extension Methods

The `IDACExtensions` class provides convenience methods for common operations. The separate `IDACAsyncExtensions` methods wrap synchronous work in `Task.Run`; they do not expose native provider asynchronous I/O.

`DACBase` sets `CommandTimeout = 0` for `ExecuteNonQuery`, `ExecuteReader` and `ExecuteBatch`; `ExecuteScalar` retains the provider default. Use an explicit command when you need its own timeout.

### Query Methods

```csharp
// Execute query returning DataTable
var users = dac.ExecuteQuery("SELECT * FROM Users");

// SQL formatting through the builder, not parameter binding
var formattedUsers = dac.ExecuteQuery("SELECT * FROM {0}", SQLBuilderCommand.TableName("Users"));

// Generic scalar
var count = dac.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
```

### Select with Filtering

The paging example requires a provider whose `Select` implementation supports its limit syntax, such as SQLite or SQL Server. The current MySQL, PostgreSQL and Oracle builders inherit a `Select` implementation that places their paging clause before the selected columns; use explicit dialect SQL for paging with those providers.

```csharp
// Select with column matches
var users = dac.Select("Users",
	columns: new[] { "ID", "Name" },
	columnMatches: new[] { new ColumnValue("Status", "Active") }
);

// Select with limit and offset
var page = dac.Select("Users",
	limit: 10,
	offset: 20,
	orderByClause: "Name ASC"
);

// Count records
var count = dac.Count("Users", columnMatches: new[] { new ColumnValue("Status", "Active") });

// Check existence
var hasUsers = dac.Any("Users");
```

### DataRow Operations

```csharp
// Save DataRow (auto-detects Insert vs Update)
var savedRows = dac.Save(dataRow);

// Insert DataRow
var insertedRows = dac.Insert(dataRow, ommitAutoIncrementPK: true);

// Update DataRow
dac.Update(dataRow);
```

`Save` chooses insert only for `DataRowState.Added`; other states take the update path. Configure the `DataTable.PrimaryKey` for row-based updates. Standard provider DACs return affected-row counts; `ommitAutoIncrementPK` is the API's existing parameter spelling.

### Dirty Read Scope

`BeginDirtyReadScope()` begins a transaction with `IsolationLevel.ReadUncommitted`; use it only with providers that support that isolation level:

```csharp
using (var scope = dac.BeginDirtyReadScope()) {
	var data = dac.ExecuteQuery("SELECT * FROM LargeTable");
}
```

## 🔨 SQL Builder

The `ISQLBuilder` interface provides common SQL construction methods with dialect-specific implementations. Supported types, paging, variables, sequences and DDL differ between builders; inspect the generated SQL for the target database.

| Implementation | Database |
|---------------|----------|
| `SqliteSQLBuilder` | SQLite |
| `MSSQLBuilder` | SQL Server |
| `FirebirdSQLBuilder` | Firebird |
| `MySQLBuilder` | MySQL |
| `PostgreSQLBuilder` | PostgreSQL |
| `OracleSQLBuilder` | Oracle |
| `ANSI2003SQLBuilder` | ANSI SQL 2003 |

### Building Queries

```csharp
var builder = dac.CreateSQLBuilder();

// SELECT statement
builder.Select("Users",
	columns: new object[] { "ID", "Name" },
	distinct: true,
	limit: 10,
	whereClause: "Status = 'Active'",
	orderByClause: "Name ASC"
);

var sql = builder.ToString();
```

### Building Statements

```csharp
var builder = dac.CreateSQLBuilder();

// Insert
builder.Insert("Users", new[] {
	new ColumnValue("Name", "Alice"),
	new ColumnValue("Email", "alice@example.com")
});

// Update
builder.Update("Users",
	setColumns: new[] { new ColumnValue("Name", "Alice Smith") },
	matchColumns: new[] { new ColumnValue("ID", 1) }
);

// Delete
builder.Delete("Users", new[] { new ColumnValue("ID", 1) });

// Execute batch
var results = dac.ExecuteBatch(builder);
```

### DDL Operations

```csharp
var tableSpecification = new TableSpecification {
	Name = "Products",
	Type = TableType.Persistent,
	PrimaryKey = new PrimaryKeySpecification { Columns = new[] { "ID" } },
	Columns = new[] {
		new ColumnSpecification { Name = "ID", Type = typeof(int), Nullable = false },
		new ColumnSpecification { Name = "Name", Type = typeof(string), Nullable = false },
		new ColumnSpecification { Name = "Price", Type = typeof(decimal), Nullable = true }
	}
};

var builder = dac.CreateSQLBuilder();
builder.CreateTable(tableSpecification);
dac.ExecuteBatch(builder);
```

Alternatively, call `dac.CreateTable(tableSpecification)` instead of building and executing that batch.

### Transaction Control

Builder transaction methods emit dialect-specific SQL; they do not create a `DACScope` transaction. Prefer scope transactions for application operations and avoid mixing the two mechanisms. For example, `OracleSQLBuilder.BeginTransaction()` emits nothing. The following illustrates SQL generation only:

```csharp
var builder = dac.CreateSQLBuilder();
builder.BeginTransaction();
builder.Insert("Users", new[] { new ColumnValue("ID", 1) });
builder.CommitTransaction();  // or RollbackTransaction()
var transactionSql = builder.ToString();
```

## 📋 Schema Introspection

`GetSchema()` returns a normalized `DBSchema` populated from the provider's metadata queries. It is not a complete database export: available objects, type mappings and fields vary by provider. MySQL inspects the selected database, PostgreSQL currently inspects only `public`, and Oracle uses the connected user's `USER_*` metadata views.

```csharp
var schema = dac.GetSchema();

// Tables
foreach (var table in schema.Tables) {
	Console.WriteLine($"Table: {table.Name}");

	// Columns
	foreach (var column in table.Columns) {
		Console.WriteLine($"  {column.Name}: {column.DataType} " +
			$"(PK: {column.IsPrimaryKey}, Nullable: {column.IsNullable})");
	}

	// Primary key
	if (table.PrimaryKey != null) {
		Console.WriteLine($"  PK: {string.Join(", ", table.PrimaryKey.ColumnNames)}");
	}

	// Foreign keys
	foreach (var foreignKey in table.ForeignKeys) {
		Console.WriteLine($"  FK: {foreignKey.Name} -> {foreignKey.ReferenceTable}");
	}
}
```

### Schema Objects

| Class | Description |
|-------|-------------|
| `DBSchema` | Normalized schema metadata supplied by a provider |
| `DBTableSchema` | Table definition with columns, keys, constraints |
| `DBColumnSchema` | Column definition with type, nullability, auto-increment |
| `DBPrimaryKeySchema` | Primary key definition |
| `DBForeignKeySchema` | Foreign key with cascade rules |
| `DBUniqueConstraintSchema` | Unique constraint definition |
| `DBTriggerSchema` | Trigger metadata; base normalization currently populates names |

### Artificial Keys

`ArtificialKeys` overlays keys and relationships in the in-memory schema model; it does not create database constraints. Load the XML configuration using `LoadFromString` or `LoadFromFile`. Invalidate an existing cached schema after changing that configuration:

```csharp
dac.ArtificialKeys = ArtificialKeys.LoadFromString(xmlConfig);
dac.InvalidateCachedSchema();
var schema = dac.GetSchema();  // Includes configured artificial keys
```

## 📁 Data Format Utilities

### Tools.Data - General Utilities

```csharp
// Read CSV to DataTable
var data = Tools.Data.ReadCsv("data.csv", hasHeaders: true);

// Create DataTable from type
var table = Tools.Data.CreateDataTableForType<MyEntity>();
```

### Tools.Json - JSON Serialization

```csharp
// Serialize to string
var json = Tools.Json.WriteToString(myObject);

// Deserialize from string
var restoredObject = Tools.Json.ReadFromString<MyClass>(json);

// File operations
Tools.Json.WriteToFile("data.json", myObject);
var loadedObject = Tools.Json.ReadFromFile<MyClass>("data.json");
```

### Tools.Xml - XML Serialization

```csharp
// Serialize to string
using var writer = new StringWriter();
Tools.Xml.Write(myObject, Encoding.Unicode, writer);
var xml = writer.ToString();

// Deserialize from string
var restoredObject = Tools.Xml.ReadFromString<MyClass>(xml);

// File operations
Tools.Xml.WriteToFile("data.xml", myObject);
var loadedObject = Tools.Xml.ReadFromFile<MyClass>("data.xml");
```

### CSV Reader

Full-featured CSV parser with streaming support:

```csharp
using Sphere10.Framework.Data.Csv;

using (var reader = new CsvReader(new StreamReader("data.csv"), hasHeaders: true)) {
	while (reader.ReadNextRecord()) {
		var name = reader["Name"];
		var email = reader["Email"];
	}
}
```

## 🗃️ File Store

The `IFileStore<TFileKeyType>` interface provides a key-based file storage abstraction:

```csharp
// GUID-based file store
using var store = new GuidFileStore("storage");

// Create new file
var key = store.NewFile();

// Write content
store.WriteAllText(key, "Hello, World!");

// Read content
var content = store.ReadAllText(key);

// Stream operations
using var stream = store.Open(key, FileMode.Open, FileAccess.Read);
```

**Implementations:**

- `GuidFileStore` - Uses GUIDs as file keys
- `SimpleFileStore` - Uses string keys (filename-based)
- `TempFileStore` - Temporary file storage
- `GuidStringFileStore` - GUID-based with string key interface

## 🔌 Database-Specific Packages

| Package | Description |
|---------|-------------|
| [Sphere10.Framework.Data.Sqlite](../Sphere10.Framework.Data.Sqlite/README.md) | SQLite with `Tools.Sqlite` |
| [Sphere10.Framework.Data.MSSQL](../Sphere10.Framework.Data.MSSQL/README.md) | SQL Server with `Tools.MSSQL` |
| [Sphere10.Framework.Data.Firebird](../Sphere10.Framework.Data.Firebird/README.md) | Firebird with `Tools.Firebird` |
| [Sphere10.Framework.Data.MySQL](../Sphere10.Framework.Data.MySQL/README.md) | MySQL with `Tools.MySQL` |
| [Sphere10.Framework.Data.PostgreSQL](../Sphere10.Framework.Data.PostgreSQL/README.md) | PostgreSQL with `Tools.PostgreSQL` |
| [Sphere10.Framework.Data.Oracle](../Sphere10.Framework.Data.Oracle/README.md) | Oracle with `Tools.Oracle` |
| [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate/README.md) | NHibernate ORM integration |

### Example: SQLite

```csharp
// Create new database
var createdDac = Tools.Sqlite.Create("mydb.sqlite", pageSize: 4096);

// Open existing database
var existingDac = Tools.Sqlite.Open("mydb.sqlite");

// Check existence
var exists = Tools.Sqlite.ExistsByPath("mydb.sqlite");

// Drop database
Tools.Sqlite.Drop("mydb.sqlite");
```

### Example: SQL Server

```csharp
// Open connection
var dac = Tools.MSSQL.Open("localhost", "MyDatabase", "sa", "password");

// Create database
Tools.MSSQL.CreateDatabase("localhost", "NewDb", "sa", "password", useIntegratedSecurity: false);

// Drop database
Tools.MSSQL.DropDatabase("localhost", "NewDb", "sa", "password", useIntegratedSecurity: false);
```

## 🧩 Extending IDAC

Create custom implementations by extending [DACBase](DAC/DACBase.cs). Its concrete execution methods use the connection, SQL builder and metadata hooks supplied by the provider:

| Override | Responsibility |
|---|---|
| `DBMSType` | Identify the provider using the existing enum. |
| `CreateConnection()` | Return a new, closed `IDbConnection`. |
| `CreateSQLBuilder()` | Return the provider's `ISQLBuilder`. |
| `EnlistInSystemTransaction(connection, transaction)` | Enlist the concrete provider connection where supported. |
| `BulkInsert(table, options, timeout)` | Implement provider bulk insertion or explicitly reject it. |
| `GetDenormalizedTableDescriptions()` | Return the metadata columns expected by `DACBase` normalization. |
| `GetDenormalizedTriggerDescriptions()` | Return trigger metadata with a `Name` column. |

Use an existing [MySQLDAC](../Sphere10.Framework.Data.MySQL/MySQLDAC.cs), [PostgreSQLDAC](../Sphere10.Framework.Data.PostgreSQL/PostgreSQLDAC.cs) or [OracleDAC](../Sphere10.Framework.Data.Oracle/OracleDAC.cs) as a concrete reference. All three currently reject bulk insertion with `NotImplementedException`; their metadata queries and manager capabilities are documented in their package READMEs.

## ✅ Best Practices

- **Keep scoped resources together** — dispose readers and commands before their owning DAC scope. `ExecuteQuery` materializes a `DataTable` inside its own scope.
- **Bind external values** — use ADO.NET parameters when needed; `ColumnValue` and `ExecuteQuery` format arguments generate SQL text. Keep identifiers and raw SQL fragments under application control.
- **Commit explicitly** — scope disposal does not commit. Inner commits do not independently persist shared transactions, and explicit inner rollbacks vote against the owner's commit.
- **Respect connection ownership** — do not share a scope's live connection across concurrent operations; create separate units of work.
- **Check provider capabilities** — builders, bulk operations, isolation levels and schema inspection are not identical across databases.

## ⚖️ License

Distributed under the **MIT License**.

See the LICENSE file for full details. More information: [MIT License](https://opensource.org/license/mit)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

`DACDecorator.Executing` and `Executed` subscriptions are forwarded to the underlying DAC. `SqliteSQLBuilder.Cast` also dispatches correctly through `ISQLBuilder` and `SQLBuilderBase`. XML serialization preserves the original stack trace when rethrowing errors. Formatter-based CSV exception serialization members remain available but are marked obsolete, matching the .NET exception contract.
