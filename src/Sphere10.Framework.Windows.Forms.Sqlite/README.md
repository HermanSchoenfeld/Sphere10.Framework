<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.Sqlite

**SQLite database connection UI controls** for Windows Forms providing dynamic ConnectionBar and ConnectionPanel implementations.

This library provides control implementations (ConnectionBar and ConnectionPanel) for SQLite database connections. These controls are used dynamically by `Sphere10.Framework.Windows.Forms` when SQLite is selected as the database type.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.Forms.Sqlite
```

## Overview

`Sphere10.Framework.Windows.Forms.Sqlite` contains UI control implementations specific to SQLite database connectivity. The controls are designed to be loaded dynamically by the base `Sphere10.Framework.Windows.Forms` library based on the selected database provider.

### What's Included

- **ConnectionBar**: Database connection toolbar/bar implementation for SQLite
- **ConnectionPanel**: Database connection panel implementation for SQLite
- SQLite-specific connection configuration UI
- Dynamic loading support for database provider selection

## Usage

The controls in this library are typically loaded automatically by `Sphere10.Framework.Windows.Forms` when SQLite is selected as the active database provider.

## Dependencies

- **Sphere10.Framework.Data.Sqlite**: SQLite data access
- **Sphere10.Framework.Windows.Forms**: Core Windows Forms utilities
- **System.Windows.Forms**: WinForms framework

## Related Projects

- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Core WinForms utilities
- [Sphere10.Framework.Data.Sqlite](../Sphere10.Framework.Data.Sqlite) - SQLite implementation
- [Sphere10.Framework.Windows.Forms.Firebird](../Sphere10.Framework.Windows.Forms.Firebird) - Firebird controls
- [Sphere10.Framework.Windows.Forms.MSSQL](../Sphere10.Framework.Windows.Forms.MSSQL) - SQL Server controls

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld, Sphere 10 Software (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.


