<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Hydrogen.Windows.Forms.MSSQL

**SQL Server database connection UI controls** for Windows Forms providing dynamic ConnectionBar and ConnectionPanel implementations.

This library provides control implementations (ConnectionBar and ConnectionPanel) for SQL Server database connections. These controls are used dynamically by `Hydrogen.Windows.Forms` when SQL Server is selected as the database type.

## Overview

`Hydrogen.Windows.Forms.MSSQL` contains UI control implementations specific to SQL Server database connectivity. The controls are designed to be loaded dynamically by the base `Hydrogen.Windows.Forms` library based on the selected database provider.

### What's Included

- **ConnectionBar**: Database connection toolbar/bar implementation for SQL Server
- **ConnectionPanel**: Database connection panel implementation for SQL Server
- SQL Server-specific connection configuration UI
- Dynamic loading support for database provider selection

## Usage

The controls in this library are typically loaded automatically by `Hydrogen.Windows.Forms` when SQL Server is selected as the active database provider.

## Dependencies

- **Hydrogen.Data.MSSQL**: SQL Server data access
- **Hydrogen.Windows.Forms**: Core Windows Forms utilities
- **System.Windows.Forms**: WinForms framework

## Related Projects

- [Hydrogen.Windows.Forms](../Hydrogen.Windows.Forms) - Core WinForms utilities
- [Hydrogen.Data.MSSQL](../Hydrogen.Data.MSSQL) - SQL Server implementation
- [Hydrogen.Windows.Forms.Firebird](../Hydrogen.Windows.Forms.Firebird) - Firebird controls
- [Hydrogen.Windows.Forms.Sqlite](../Hydrogen.Windows.Forms.Sqlite) - SQLite controls

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld, Sphere 10 Software (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.
