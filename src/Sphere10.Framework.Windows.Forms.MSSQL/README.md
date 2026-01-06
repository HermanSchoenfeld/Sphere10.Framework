<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.MSSQL

**SQL Server database connection UI controls** for Windows Forms providing dynamic ConnectionBar and ConnectionPanel implementations.

This library provides control implementations (ConnectionBar and ConnectionPanel) for SQL Server database connections. These controls are used dynamically by `Sphere10.Framework.Windows.Forms` when SQL Server is selected as the database type.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.Forms.MSSQL
```

## Overview

`Sphere10.Framework.Windows.Forms.MSSQL` contains UI control implementations specific to SQL Server database connectivity. The controls are designed to be loaded dynamically by the base `Sphere10.Framework.Windows.Forms` library based on the selected database provider.

### What's Included

- **ConnectionBar**: Database connection toolbar/bar implementation for SQL Server
- **ConnectionPanel**: Database connection panel implementation for SQL Server
- SQL Server-specific connection configuration UI
- Dynamic loading support for database provider selection

## Usage

The controls in this library are typically loaded automatically by `Sphere10.Framework.Windows.Forms` when SQL Server is selected as the active database provider.

## Dependencies

- **Sphere10.Framework.Data.MSSQL**: SQL Server data access
- **Sphere10.Framework.Windows.Forms**: Core Windows Forms utilities
- **System.Windows.Forms**: WinForms framework

## Related Projects

- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Core WinForms utilities
- [Sphere10.Framework.Data.MSSQL](../Sphere10.Framework.Data.MSSQL) - SQL Server implementation
- [Sphere10.Framework.Windows.Forms.Firebird](../Sphere10.Framework.Windows.Forms.Firebird) - Firebird controls
- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite) - SQLite controls

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld, Sphere 10 Software (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.


