<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.MySQL

**MySQL implementation** for Sphere10.Framework.Data abstraction layer, enabling database access using MySqlConnector, the high-performance .NET data provider for MySQL and MariaDB.

Sphere10.Framework.Data.MySQL brings **MySQL/MariaDB capabilities** to the Sphere10 Framework while maintaining database-agnostic abstraction. Compatible with MySQL 5.7+, MySQL 8.0+, and MariaDB 10.2+.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.MySQL
```

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Data;

// Connect to MySQL
var dac = Tools.MySQL.Open("localhost", "mydb", "root", "password", port: 3306);
using (dac.BeginScope()) {
    var result = dac.ExecuteScalar("SELECT VERSION()");
}
```
