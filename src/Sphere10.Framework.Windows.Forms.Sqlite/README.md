<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Hydrogen.Windows.Forms.Sqlite

**SQLite database connection UI controls** for Windows Forms providing dynamic ConnectionBar and ConnectionPanel implementations.

This library provides control implementations (ConnectionBar and ConnectionPanel) for SQLite database connections. These controls are used dynamically by `Hydrogen.Windows.Forms` when SQLite is selected as the database type.

## Overview

`Hydrogen.Windows.Forms.Sqlite` contains UI control implementations specific to SQLite database connectivity. The controls are designed to be loaded dynamically by the base `Hydrogen.Windows.Forms` library based on the selected database provider.

### What's Included

- **ConnectionBar**: Database connection toolbar/bar implementation for SQLite
- **ConnectionPanel**: Database connection panel implementation for SQLite
- SQLite-specific connection configuration UI
- Dynamic loading support for database provider selection

## Usage

The controls in this library are typically loaded automatically by `Hydrogen.Windows.Forms` when SQLite is selected as the active database provider.

## Dependencies

- **Hydrogen.Data.Sqlite**: SQLite data access
- **Hydrogen.Windows.Forms**: Core Windows Forms utilities
- **System.Windows.Forms**: WinForms framework

## Related Projects

- [Hydrogen.Windows.Forms](../Hydrogen.Windows.Forms) - Core WinForms utilities
- [Hydrogen.Data.Sqlite](../Hydrogen.Data.Sqlite) - SQLite implementation
- [Hydrogen.Windows.Forms.Firebird](../Hydrogen.Windows.Forms.Firebird) - Firebird controls
- [Hydrogen.Windows.Forms.MSSQL](../Hydrogen.Windows.Forms.MSSQL) - SQL Server controls

## License & Author

**License**: [Refer to repository LICENSE](../../LICENSE)  
**Author**: Herman Schoenfeld, Sphere 10 Software (sphere10.com)  
**Copyright**: © 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved.
