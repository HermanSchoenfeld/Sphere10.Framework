<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.Sqlite

**SQLite implementation** for Sphere10.Framework.Data abstraction layer, providing embedded database access without external dependencies for development, testing, and lightweight deployments.

Sphere10.Framework.Data.Sqlite enables **zero-configuration database access** with in-memory and file-based SQLite databases, ideal for embedded scenarios, testing, and resource-constrained environments. Seamlessly integrates with the Sphere10.Framework.Data abstraction for database-agnostic applications.

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Data;

// In-memory or file-based database
var dac = Tools.Sqlite.Open(":memory:");

// Create table
dac.ExecuteNonQuery(@"CREATE TABLE Users (
    ID INTEGER PRIMARY KEY,
    Name TEXT NOT NULL
)");

// Insert and query
dac.Insert("Users", new[] {
    new ColumnValue("ID", 1),
    new ColumnValue("Name", "Alice")
});

// Retrieve with parameters
int count = dac.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM Users WHERE Name = @name",
    new ColumnValue("@name", "Alice"));  // Returns 1
```

## 🏗️ Core Concepts

**In-Memory Database**: `:memory:` databases for fast testing and temporary data without disk I/O.

**File-Based Persistence**: `.db` files for persistent storage with automatic WAL (Write-Ahead Logging) mode.

**Zero Configuration**: No external service or connection strings required; embedded within application.

**Transaction Scopes**: Full ACID support with automatic SQLite transaction management.

**Query Parameterization**: Safe parameter binding prevents SQL injection attacks.

## 🔧 Core Examples

### Connection & Database Creation

```csharp
using Sphere10.Framework.Data;

// In-memory database (fast testing, no disk I/O)
var memoryDb = Tools.Sqlite.Open(":memory:");

// File-based database (persistent storage)
var fileDb = Tools.Sqlite.Open("myapp.db");

// Relative path (auto-created in application directory)
var localDb = Tools.Sqlite.Open("./data/local.db");

// Absolute path
var systemDb = Tools.Sqlite.Open(@"C:\data\app.db");

// Create table
fileDb.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Products (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
)");
```

### Insert & Retrieve Operations

```csharp
using Sphere10.Framework.Data;

var dac = Tools.Sqlite.Open("shop.db");

// Create products table
dac.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Products (
    ID INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Price REAL NOT NULL,
    Stock INTEGER
)");

// Insert single record
dac.Insert("Products", new[] {
    new ColumnValue("ID", 1),
    new ColumnValue("Name", "Widget"),
    new ColumnValue("Price", 9.99),
    new ColumnValue("Stock", 100)
});

// Insert multiple records in loop
for (int i = 2; i <= 5; i++) {
    dac.Insert("Products", new[] {
        new ColumnValue("ID", i),
        new ColumnValue("Name", $"Product {i}"),
        new ColumnValue("Price", 10.0 + i),
        new ColumnValue("Stock", 50 + (i * 10))
    });
}

// Query all records
var products = dac.ExecuteQuery("SELECT * FROM Products");

// Parameterized query (safe from SQL injection)
var cheapProducts = dac.ExecuteQuery(
    "SELECT * FROM Products WHERE Price < @maxPrice",
    new ColumnValue("@maxPrice", 15.0));

// Scalar query
var totalStock = dac.ExecuteScalar<int>(
    "SELECT SUM(Stock) FROM Products");

var expensiveCount = dac.ExecuteScalar<int>(
    "SELECT COUNT(*) FROM Products WHERE Price > @minPrice",
    new ColumnValue("@minPrice", 50.0));
```

### Transactions & Data Consistency

```csharp
using Sphere10.Framework.Data;

var dac = Tools.Sqlite.Open("bank.db");

// Create accounts table
dac.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Accounts (
    ID INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Balance REAL NOT NULL
)");

// Initialize accounts
dac.Insert("Accounts", new[] {
    new ColumnValue("ID", 1),
    new ColumnValue("Name", "Alice"),
    new ColumnValue("Balance", 1000.0)
});

dac.Insert("Accounts", new[] {
    new ColumnValue("ID", 2),
    new ColumnValue("Name", "Bob"),
    new ColumnValue("Balance", 500.0)
});

// Transactional money transfer
using (var scope = dac.BeginTransactionScope()) {
    // Withdraw from Alice
    dac.ExecuteNonQuery(
        "UPDATE Accounts SET Balance = Balance - @amount WHERE ID = 1",
        new ColumnValue("@amount", 100.0));

    // Deposit to Bob
    dac.ExecuteNonQuery(
        "UPDATE Accounts SET Balance = Balance + @amount WHERE ID = 2",
        new ColumnValue("@amount", 100.0));

    // Both operations succeed or rollback together
    scope.Commit();
}

// Verify transfer
var aliceBalance = dac.ExecuteScalar<double>(
    "SELECT Balance FROM Accounts WHERE ID = 1");
var bobBalance = dac.ExecuteScalar<double>(
    "SELECT Balance FROM Accounts WHERE ID = 2");

Console.WriteLine($"Alice: {aliceBalance}");  // 900
Console.WriteLine($"Bob: {bobBalance}");      // 600
```

### Update & Delete Operations

```csharp
using Sphere10.Framework.Data;

var dac = Tools.Sqlite.Open("inventory.db");

// Create product table
dac.ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS Products (
    ID INTEGER PRIMARY KEY,
    Name TEXT NOT NULL,
    Quantity INTEGER,
    Status TEXT
)");

// Insert sample product
dac.Insert("Products", new[] {
    new ColumnValue("ID", 1),
    new ColumnValue("Name", "Widget"),
    new ColumnValue("Quantity", 100),
    new ColumnValue("Status", "Active")
});

// Update single column
dac.Update("Products",
    new[] { new ColumnValue("Quantity", 85) },
    "WHERE ID = 1");

// Update multiple columns with WHERE clause
dac.Update("Products",
    new[] {
        new ColumnValue("Quantity", 50),
        new ColumnValue("Status", "Low Stock")
    },
    "WHERE Quantity < 75");

// Delete with WHERE clause
dac.ExecuteNonQuery("DELETE FROM Products WHERE Status = 'Inactive'");

// Verify results
var product = dac.ExecuteQuery("SELECT * FROM Products WHERE ID = 1");
```

## 🏗️ Architecture

**SqliteDatabaseManager**: Core abstraction implementing IDataAccessContext for SQLite operations.

**Connection Pooling**: Automatic connection reuse and lifecycle management through Sphere10.Framework.Data.

**SQLite Driver**: System.Data.SQLite (P/Invoke) or Microsoft.Data.Sqlite (managed) depending on platform.

**WAL Mode**: Optimized transaction handling with Write-Ahead Logging for concurrent access.

## 📋 Best Practices

- Use **in-memory databases** for unit tests and fast iteration (`:memory:`)
- Enable **WAL mode** for file-based databases to improve concurrency: `PRAGMA journal_mode=WAL`
- Always use **parameterized queries** with `ColumnValue` to prevent SQL injection
- Wrap related operations in **transaction scopes** for atomicity
- Use **connection pooling** through Sphere10.Framework.Data rather than creating new connections
- Consider database **file location** in application deployment (relative vs absolute paths)
- Test with both **in-memory** (fast) and **file-based** (realistic) databases

## 📊 Status & Compatibility

- **Framework**: .NET 5.0+, .NET Framework 4.7+
- **Platform**: Windows, Linux, macOS (cross-platform via System.Data.SQLite or Microsoft.Data.Sqlite)
- **Performance**: Embedded, zero-configuration, no external service required
- **Concurrency**: Good for single-process or limited concurrent access

## 📦 Dependencies

- **Sphere10.Framework.Data**: Data abstraction layer
- **System.Data.SQLite** or **Microsoft.Data.Sqlite**: SQLite database provider
- **.NET Standard 2.1+**: Cross-platform compatibility

## 📚 Related Projects

- [Sphere10.Framework.Data](../Sphere10.Framework.Data) - Core data abstraction layer
- [Sphere10.Framework.Data.MSSQL](../Sphere10.Framework.Data.MSSQL) - SQL Server implementation
- [Sphere10.Framework.Data.Firebird](../Sphere10.Framework.Data.Firebird) - Firebird implementation
- [Sphere10.Framework.Data.NHibernate](../Sphere10.Framework.Data.NHibernate) - NHibernate ORM integration
- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite) - WinForms data binding for SQLite
- [Sphere10.Framework.Tests](../../tests/Sphere10.Framework.Tests) - Test patterns and examples

## 📄 License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld, Sphere 10 Software (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.

