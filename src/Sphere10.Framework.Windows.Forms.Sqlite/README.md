<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.Sqlite

SQLite connection editors for Windows Forms. `SqliteConnectionBar` and `SqliteConnectionPanel` collect a database filename, password, journal mode and synchronization mode, then expose those settings through `IDatabaseConnectionProvider`. Use the bar in a tool area and the panel in a settings page or dialog.

## Installation and requirements

```powershell
dotnet add package Sphere10.Framework.Windows.Forms.Sqlite
```

Use a Windows desktop application targeting `net10.0-windows`, with the .NET 10 SDK and Windows Forms enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Create and access the controls on your application's STA UI thread with a WinForms message loop. The package references its matching data provider and `Sphere10.Framework.Windows.Forms`; install the UI package into the desktop project that hosts the controls.

The data provider uses `System.Data.SQLite` and includes `SourceGear.sqlite3` as a native dependency. Keep the native runtime assets in the deployed application's output for its selected architecture. These are Windows UI controls even though the separate data library targets `net10.0`.

## Controls and connection settings

The concrete controls are in `Sphere10.Framework.Windows.Forms.Sqlite`.

| API | Behavior |
| --- | --- |
| `ConnectionString` | Populates or reads the filename, password, journal mode and sync mode. |
| `Filename` | Read-only current file-selection path; set it through `ConnectionString` or the UI. |
| `Password`, `HasPassword` | Read-only password and whether its trimmed value is nonempty. |
| `Mode` | File-selector `PathSelectionMode`; the designer default is `File`. |
| `DatabaseName` | Filename without its extension, or null when no path is selected. |
| `GetDAC()` | Returns a new `SqliteDAC` as `IDAC` without opening it. |
| `TestConnection()` | Opens and disposes a DAC scope on a worker task, returning `Task<Result>`. |
| `ArtificialKeysFile` | Bar-only inherited setting, applied when `GetDAC()` is called. |

The initial journal choice is `SqliteJournalMode.Default`; initial synchronization is `SqliteSyncMode.Normal`. The combo boxes translate these framework enums to `System.Data.SQLite` settings.

## Add a bar or show a connection dialog

The example uses an existing database file and `PathSelectionMode.OpenFile`. Run it from the application's STA UI thread. Pass the dialog an initial string produced by `SQLiteConnectionStringBuilder` or a saved string from these controls.

```csharp
using System.Data.SQLite;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Sqlite;

public static class SqliteConnectionUi {
	public static SqliteConnectionBar AddBar(Control host, string filename) {
		var settings = new SQLiteConnectionStringBuilder { DataSource = filename, FailIfMissing = true };
		var bar = new SqliteConnectionBar {
			Dock = DockStyle.Top,
			Mode = PathSelectionMode.OpenFile,
			ConnectionString = settings.ConnectionString
		};
		host.Controls.Add(bar);
		return bar;
	}

	public static bool TryEditConnection(IWin32Window owner, string initialConnectionString, out string connectionString) {
		using var dialog = new Form {
			Text = "SQLite connection",
			Width = 640,
			Height = 230,
			StartPosition = FormStartPosition.CenterParent
		};
		var panel = new SqliteConnectionPanel {
			Dock = DockStyle.Fill,
			Mode = PathSelectionMode.OpenFile,
			ConnectionString = initialConnectionString
		};
		var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
		var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
		var accept = new Button { Text = "OK", DialogResult = DialogResult.OK };
		buttons.Controls.Add(cancel);
		buttons.Controls.Add(accept);
		dialog.Controls.Add(panel);
		dialog.Controls.Add(buttons);
		dialog.AcceptButton = accept;
		dialog.CancelButton = cancel;

		connectionString = initialConnectionString;
		if (dialog.ShowDialog(owner) != DialogResult.OK)
			return false;
		connectionString = panel.ConnectionString;
		return true;
	}
}
```

The host owns the added bar. The dialog disposes its panel and buttons after closing; cancellation returns the original connection string unchanged. Pressing OK collects settings without opening or creating a database. After acceptance, obtain `bar.GetDAC()` or construct `new Sphere10.Framework.Data.SqliteDAC(connectionString)` for application operations.

## Shared provider selection

Register `Sphere10.Framework.Windows.Forms.Sqlite.ModuleConfiguration` with `UseModule<TModule>()` in the existing framework startup builder before creating a generic `DatabaseConnectionBar` or `DatabaseConnectionPanel`. It supplies named transient implementations under `nameof(DBMSType.Sqlite)`.

After the framework has started, set the generic control's `SelectedDBMSType` to `DBMSType.Sqlite`, then assign `ConnectionString`. Referencing the package alone does not register its module. Concrete `SqliteConnectionBar` and `SqliteConnectionPanel` instances can be constructed directly without provider lookup.

## Events and connection lifecycle

- The concrete editors inherit the parameterless `StateChanged` event from `UserControlEx`. It reports edited control state, not database connectivity.
- The generic `DatabaseConnectionPanel` additionally raises `DBMSTypeChanged(panel, dbmsType)` when its provider selection changes; the generic bar has no corresponding public event.
- Await `TestConnection()` in a UI event handler and inspect `Result.IsSuccess`. The temporary scope is disposed after the test; success does not retain a live connection. Parsing settings or loading `ArtificialKeysFile` happens before the method's connection-error handling and can throw separately.
- Read settings on the UI thread, then use the resulting DAC for work. Dispose application-owned connections or DAC scopes. The editor does not manage a database's lifetime or your transactions.

## SQLite behavior and limitations

Every generated connection string includes `FailIfMissing=True`. Selecting `SaveFile` or typing a new path does not turn these controls into database-creation tools: create the database separately before testing or opening it. For in-memory databases or specialized connection options, use the data provider directly.

The editor reconstructs its string from the four visible settings. It does not preserve options such as `Read Only`, pooling or custom URI settings. Apply any additional options after reading `ConnectionString` and construct the DAC from that final string.

The password is passed to the provider; the presence of a password field does not guarantee encryption support in every native SQLite build. Journal and synchronization choices affect locking, durability and file access, so choose them for the workload and filesystem in use. See the [SQLite provider guide](../Sphere10.Framework.Data.Sqlite/README.md) for database operations and deployment details. Avoid logging connection strings containing passwords.

## Build and source

From the repository root on Windows:

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.Sqlite/Sphere10.Framework.Windows.Forms.Sqlite.csproj -c Release
```

- [SqliteConnectionBar](SqliteConnectionBar.cs) and [SqliteConnectionPanel](SqliteConnectionPanel.cs): field mapping and connection-test implementation.
- [ModuleConfiguration](ModuleConfiguration.cs): registration.
- [Connection provider interface](../Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs), [bar base](../Sphere10.Framework.Windows.Forms/Database/ConnectionBarBase.cs), and [panel base](../Sphere10.Framework.Windows.Forms/Database/ConnectionPanelBase.cs): shared API.

## Related guides

- [Core WinForms](../Sphere10.Framework.Windows.Forms/README.md)
- [SQLite data provider](../Sphere10.Framework.Data.Sqlite/README.md)
- [Application startup and modules](../Sphere10.Framework.Application/README.md)
- [SQL Server controls](../Sphere10.Framework.Windows.Forms.MSSQL/README.md) and [Firebird controls](../Sphere10.Framework.Windows.Forms.Firebird/README.md)

## License & Author

**License**: [MIT License](../../LICENSE)

**Author**: Herman Schoenfeld (sphere10.com)

**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
