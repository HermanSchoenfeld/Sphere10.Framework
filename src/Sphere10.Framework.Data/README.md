# 💾 Sphere10.Framework.Data

<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**Database-agnostic data access layer** providing the `IDAC` interface, SQL builders, transaction scopes, schema introspection, and data format utilities for CSV, JSON, and XML.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data
```

For database-specific support, also install the appropriate provider package:

```bash
dotnet add package Sphere10.Framework.Data.Sqlite    # SQLite
dotnet add package Sphere10.Framework.Data.MSSQL     # SQL Server  
dotnet add package Sphere10.Framework.Data.Firebird  # Firebird
```

## 🏗️ Core Architecture

### IDAC Interface

The **Data Access Context (DAC)** is the central abstraction for database operations. The `IDAC` interface provides:

| Member | Description |
|--------|-------------|
| `CreateConnection()` | Creates a new `IDbConnection` to the database |
| `CreateSQLBuilder()` | Creates a database-specific `ISQLBuilder` |
| `ExecuteNonQuery(query)` | Executes DDL/DML returning affected row count |
| `ExecuteScalar(query)` | Executes query returning single value |
| `ExecuteReader(query)` | Executes query returning `IDataReader` |
| `ExecuteBatch(sqlBuilder)` | Executes batched statements from SQL builder |
| `Insert(table, values)` | Inserts row returning identity (if applicable) |
| `Update(table, setValues, whereValues)` | Updates matching rows |
| `Delete(table, matchColumns)` | Deletes matching rows |
| `GetSchema()` | Returns complete `DBSchema` with tables, columns, keys |
| `BulkInsert(table, options, timeout)` | Bulk inserts from DataTable |

### DACScope - Connection and Transaction Management

The `DACScope` provides automatic connection and transaction management using the `using` pattern:

```csharp
using Sphere10.Framework.Data;

// Opens a connection scope (reuses connection if nested)
using (var scope = dac.BeginScope(openConnection: true)) {
    // All operations share this connection
    dac.Insert("Users", new[] { new ColumnValue("ID", 1) });
    dac.Insert("Users", new[] { new ColumnValue("ID", 2) });
}
// Connection automatically closed when scope disposes
```

**Transaction Support:**

```csharp
using (var scope = dac.BeginScope()) {
    scope.BeginTransaction();
    
    dac.Insert("BasicTable", new[] { new ColumnValue("ID", 1) });
    dac.Insert("BasicTable", new[] { new ColumnValue("ID", 2) });
    
    scope.Commit();  // Explicit commit required
}
// Uncommitted transaction auto-rollbacks on dispose
```

**Nested Scopes:**

Scopes reuse the same underlying connection when nested:

```csharp
using (var outerScope = dac.BeginScope()) {
    outerScope.BeginTransaction();
    
    using (var innerScope = dac.BeginScope()) {
        // Same connection as outerScope
        innerScope.BeginTransaction();  // Nested transaction
        dac.Insert("Table", new[] { new ColumnValue("ID", 1) });
        innerScope.Commit();
    }
    
    outerScope.Rollback();  // Rolls back everything including inner commits
}
```

**TransactionScope Integration:**

DACScope integrates with `System.Transactions.TransactionScope` for distributed transaction support:

```csharp
using (var txn = new TransactionScope(TransactionScopeOption.Required)) {
    using (dac.BeginScope(true)) {
        dac.Insert("Table", new[] { new ColumnValue("ID", 1) });
        txn.Complete();  // Enlists automatically
    }
}
```

### ColumnValue - Type-Safe Parameters

The `ColumnValue` struct represents a column name and value pair for CRUD operations:

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

## 🔧 DAC Extension Methods

The `IDACExtensions` class provides convenience methods for common operations:

### Query Methods

```csharp
// Execute query returning DataTable
DataTable result = dac.ExecuteQuery("SELECT * FROM Users");

// With format arguments (uses SQLBuilder)
DataTable result = dac.ExecuteQuery("SELECT * FROM {0}", SQLBuilderCommand.TableName("Users"));

// Generic scalar
int count = dac.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
```

### Select with Filtering

```csharp
// Select with column matches
DataTable users = dac.Select("Users",
    columns: new[] { "ID", "Name" },
    columnMatches: new[] { new ColumnValue("Status", "Active") }
);

// Select with limit and offset
DataTable page = dac.Select("Users",
    limit: 10,
    offset: 20,
    orderByClause: "Name ASC"
);

// Count records
long count = dac.Count("Users", columnMatches: new[] { new ColumnValue("Status", "Active") });

// Check existence
bool hasUsers = dac.Any("Users");
```

### DataRow Operations

```csharp
// Save DataRow (auto-detects Insert vs Update)
long result = dac.Save(dataRow);

// Insert DataRow
long identity = dac.Insert(dataRow, ommitAutoIncrementPK: true);

// Update DataRow
dac.Update(dataRow);
```

### Dirty Read Scope

For read-uncommitted isolation:

```csharp
using (var scope = dac.BeginDirtyReadScope()) {
    var data = dac.ExecuteQuery("SELECT * FROM LargeTable");
}
```

## 🔨 SQL Builder

The `ISQLBuilder` interface provides database-agnostic SQL generation with dialect-specific implementations:

| Implementation | Database |
|---------------|----------|
| `SqliteSQLBuilder` | SQLite |
| `MSSQLBuilder` | SQL Server |
| `FirebirdSQLBuilder` | Firebird |
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

string sql = builder.ToString();
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
DataTable[] results = dac.ExecuteBatch(builder);
```

### DDL Operations

```csharp
var builder = dac.CreateSQLBuilder();

// Create table
builder.CreateTable(new TableSpecification {
    Name = "Products",
    Type = TableType.Persistent,
    PrimaryKey = new PrimaryKeySpecification { Columns = new[] { "ID" } },
    Columns = new[] {
        new ColumnSpecification { Name = "ID", Type = typeof(int), Nullable = false },
        new ColumnSpecification { Name = "Name", Type = typeof(string), Nullable = false },
        new ColumnSpecification { Name = "Price", Type = typeof(decimal), Nullable = true }
    }
});

dac.ExecuteBatch(builder);

// Or use extension method
dac.CreateTable(tableSpecification);
```

### Transaction Control

```csharp
var builder = dac.CreateSQLBuilder();
builder.BeginTransaction();
builder.Insert("Users", new[] { new ColumnValue("ID", 1) });
builder.CommitTransaction();  // or RollbackTransaction()
dac.ExecuteBatch(builder);
```

## 📋 Schema Introspection

The `GetSchema()` method returns a complete `DBSchema` object:

```csharp
DBSchema schema = dac.GetSchema();

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
    foreach (var fk in table.ForeignKeys) {
        Console.WriteLine($"  FK: {fk.Name} -> {fk.ReferenceTable}");
    }
}
```

### Schema Objects

| Class | Description |
|-------|-------------|
| `DBSchema` | Complete database schema |
| `DBTableSchema` | Table definition with columns, keys, constraints |
| `DBColumnSchema` | Column definition with type, nullability, auto-increment |
| `DBPrimaryKeySchema` | Primary key definition |
| `DBForeignKeySchema` | Foreign key with cascade rules |
| `DBUniqueConstraintSchema` | Unique constraint definition |
| `DBTriggerSchema` | Trigger definition |

### Artificial Keys

For databases lacking native foreign key support, `ArtificialKeys` can define relationships programmatically:

```csharp
dac.ArtificialKeys = ArtificialKeys.FromXml(xmlConfig);
var schema = dac.GetSchema();  // Includes artificial FK definitions
```

## 📁 Data Format Utilities

### Tools.Data - General Utilities

```csharp
// Read CSV to DataTable
DataTable data = Tools.Data.ReadCsv("data.csv", hasHeaders: true);

// Create DataTable from type
DataTable table = Tools.Data.CreateDataTableForType<MyEntity>();
```

### Tools.Json - JSON Serialization

```csharp
// Serialize to string
string json = Tools.Json.WriteToString(myObject);

// Deserialize from string
MyClass obj = Tools.Json.ReadFromString<MyClass>(json);

// File operations
Tools.Json.WriteToFile("data.json", myObject);
MyClass loaded = Tools.Json.ReadFromFile<MyClass>("data.json");
```

### Tools.Xml - XML Serialization

```csharp
// Serialize to string
using StringWriter writer = new StringWriter();
Tools.Xml.Write(myObject, Encoding.Unicode, writer);
string xml = writer.ToString();

// Deserialize from string
MyClass obj = Tools.Xml.ReadFromString<MyClass>(xml);

// File operations
Tools.Xml.WriteToFile("data.xml", myObject);
MyClass loaded = Tools.Xml.ReadFromFile<MyClass>("data.xml");
```

### CSV Reader

Full-featured CSV parser with streaming support:

```csharp
using Sphere10.Framework.Data.Csv;

using (var reader = new CsvReader(new StreamReader("data.csv"), hasHeaders: true)) {
    while (reader.ReadNextRecord()) {
        string name = reader["Name"];
        string email = reader["Email"];
    }
}
```

## 🗃️ File Store

The `IFileStore<TFileKeyType>` interface provides a key-based file storage abstraction:

```csharp
// GUID-based file store
IFileStore<Guid> store = new GuidFileStore("/path/to/storage");

// Create new file
Guid key = store.NewFile();

// Write content
store.WriteAllText(key, "Hello, World!");

// Read content
string content = store.ReadAllText(key);

// Stream operations
using Stream stream = store.Open(key, FileMode.Open, FileAccess.Read);
```

**Implementations:**
- `GuidFileStore` - Uses GUIDs as file keys
- `SimpleFileStore` - Uses string keys (filename-based)
- `TempFileStore` - Temporary file storage
- `GuidStringFileStore` - GUID-based with string key interface

## 🔌 Database-Specific Packages

| Package | Description |
|---------|-------------|
| [Sphere10.Framework.Data.Sqlite](../Sphere10.Framework.Data.Sqlite) | SQLite with `Tools.Sqlite` |
| [Sphere10.Framework.Data.MSSQL](../Sphere10.Framework.Data.MSSQL) | SQL Server with `Tools.MSSQL` |
| [Sphere10.Framework.Data.Firebird](../Sphere10.Framework.Data.Firebird) | Firebird with `Tools.Firebird` |
| [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate) | NHibernate ORM integration |

### Example: SQLite

```csharp
// Create new database
var dac = Tools.Sqlite.Create("mydb.sqlite", pageSize: 4096);

// Open existing database  
var dac = Tools.Sqlite.Open("mydb.sqlite");

// Check existence
bool exists = Tools.Sqlite.ExistsByPath("mydb.sqlite");

// Drop database
Tools.Sqlite.Drop("mydb.sqlite");
```

### Example: SQL Server

```csharp
// Open connection
var dac = Tools.MSSQL.Open("localhost", "MyDatabase", "sa", "password");

// Create database
Tools.MSSQL.CreateDatabase("localhost", "NewDb", "sa", "password", useWindowsAuth: false);

// Drop database
Tools.MSSQL.DropDatabase("localhost", "NewDb", "sa", "password", useWindowsAuth: false);
```

## 🧩 Extending IDAC

Create custom DAC implementations by extending `DACBase`:

```csharp
public class CustomDAC : DACBase {
    public CustomDAC(string connectionString, ILogger logger = null)
        : base(connectionString, logger) { }
    
    public override DBMSType DBMSType => DBMSType.Other;
    
    public override IDbConnection CreateConnection() {
        return new CustomDbConnection(ConnectionString);
    }
    
    public override ISQLBuilder CreateSQLBuilder() {
        return new CustomSQLBuilder();
    }
    
    public override void EnlistInSystemTransaction(
        IDbConnection connection, 
        System.Transactions.Transaction transaction) {
        // Enlist connection in distributed transaction
    }
    
    public override void BulkInsert(
        DataTable table, 
        BulkInsertOptions options, 
        TimeSpan timeout) {
        // Implement bulk insert
    }
    
    protected override DataTable GetDenormalizedTableDescriptions() {
        // Return schema metadata
    }
    
    protected override DataTable GetDenormalizedTriggerDescriptions() {
        // Return trigger metadata
    }
}
```

## ✅ Best Practices

- **Always use `DACScope`** - Ensures proper connection cleanup and transaction handling
- **Use `ColumnValue` for parameters** - Prevents SQL injection and ensures type safety
- **Explicit Commit** - Transactions require explicit `Commit()`; uncommitted transactions auto-rollback
- **Nested scope behavior** - Inner scope commits don't persist if outer scope rolls back
- **One DAC per thread** - DAC instances are not thread-safe; create separate instances for concurrent access
- **Use `ISQLBuilder` for complex queries** - Provides database-agnostic SQL generation

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer

