<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.Firebird

**Firebird database connection UI controls** for Windows Forms providing dynamic ConnectionBar and ConnectionPanel implementations.

This library provides control implementations (ConnectionBar and ConnectionPanel) for Firebird database connections. These controls are used dynamically by `Sphere10.Framework.Windows.Forms` when Firebird is selected as the database type.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.Forms.Firebird
```

## Overview

`Sphere10.Framework.Windows.Forms.Firebird` contains UI control implementations specific to Firebird database connectivity. The controls are designed to be loaded dynamically by the base `Sphere10.Framework.Windows.Forms` library based on the selected database provider.

### What's Included

- **ConnectionBar**: Database connection toolbar/bar implementation for Firebird
- **ConnectionPanel**: Database connection panel implementation for Firebird
- Firebird-specific connection configuration UI
- Dynamic loading support for database provider selection

## Usage

The controls in this library are typically loaded automatically by `Sphere10.Framework.Windows.Forms` when Firebird is selected as the active database provider.

## Dependencies

- **Sphere10.Framework.Data.Firebird**: Firebird data access
- **Sphere10.Framework.Windows.Forms**: Core Windows Forms utilities
- **System.Windows.Forms**: WinForms framework

## Related Projects

- [Sphere10.Framework.Windows.Forms](../Sphere10.Framework.Windows.Forms) - Core WinForms utilities
- [Sphere10.Framework.Data.Firebird](../Sphere10.Framework.Data.Firebird) - Firebird implementation
- [Sphere10.Framework.Windows.Forms.MSSQL](../Sphere10.Framework.Windows.Forms.MSSQL) - SQL Server controls
- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite) - SQLite controls

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld, Sphere 10 Software (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.


