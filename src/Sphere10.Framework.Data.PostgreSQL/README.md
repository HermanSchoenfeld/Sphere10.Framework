<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 💾 Sphere10.Framework.Data.PostgreSQL

**PostgreSQL implementation** for Sphere10.Framework.Data abstraction layer, enabling database access using Npgsql, the standard .NET data provider for PostgreSQL.

Sphere10.Framework.Data.PostgreSQL brings **PostgreSQL capabilities** to the Sphere10 Framework while maintaining database-agnostic abstraction. Compatible with PostgreSQL 12+ and cloud-hosted PostgreSQL services.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Data.PostgreSQL
```

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Data;

// Connect to PostgreSQL
var dac = Tools.PostgreSQL.Open("localhost", "mydb", "postgres", "password", port: 5432);
using (dac.BeginScope()) {
    var result = dac.ExecuteScalar("SELECT version()");
}
```
