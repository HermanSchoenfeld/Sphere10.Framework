<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.MSSQL

SQL Server connection editors for Windows Forms. `MSSQLConnectionBar` provides a compact connection bar; `MSSQLConnectionPanel` provides the same fields in a panel suitable for a settings page or dialog. Both produce `MSSQLDAC` instances through the shared `IDatabaseConnectionProvider` API.

## Installation and requirements

```powershell
dotnet add package Sphere10.Framework.Windows.Forms.MSSQL
```

Use a Windows desktop application targeting `net10.0-windows`, with the .NET 10 SDK and Windows Forms enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Create and access the controls on your application's STA UI thread with a WinForms message loop. The package references its matching data provider and `Sphere10.Framework.Windows.Forms`; install the UI package into the desktop project that hosts the controls.

## Controls and connection settings

The concrete controls are in `Sphere10.Framework.Windows.Forms.MSSQL`. Their common base types and provider interface are in `Sphere10.Framework.Windows.Forms`.

| API | Behavior |
| --- | --- |
| `Server` | SQL Server host or named instance. |
| `Port` | Optional port as text; emitted as `server,port` in `Data Source`. |
| `Database` / `DatabaseName` | Editable initial catalog / read-only catalog name. |
| `Username`, `Password` | SQL authentication fields. |
| `ConnectionString` | Reads the current fields or populates them from a SQL connection string. |
| `GetDAC()` | Returns a new `MSSQLDAC` as `IDAC`; it does not open a connection. |
| `TestConnection()` | Returns `Task<Result>` after attempting to open and dispose a DAC scope. |
| `ArtificialKeysFile` | Bar-only inherited setting; `GetDAC()` loads the selected artificial-key XML file into the DAC. |

## Add a bar or show a connection dialog

Call these methods from your existing WinForms application. Pass an initial SQL-authentication connection string, such as `Server=localhost;Database=Example;User ID=app_user`, and let the user enter the password. The package supplies the editor panel; the example composes the dialog using standard `Form.ShowDialog(owner)`.

```csharp
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.MSSQL;

public static class SqlServerConnectionUi {
	public static MSSQLConnectionBar AddBar(Control host, string connectionString) {
		var bar = new MSSQLConnectionBar {
			Dock = DockStyle.Top,
			ConnectionString = connectionString
		};
		host.Controls.Add(bar);
		return bar;
	}

	public static bool TryEditConnection(IWin32Window owner, string initialConnectionString, out string connectionString) {
		using var dialog = new Form {
			Text = "SQL Server connection",
			Width = 640,
			Height = 230,
			StartPosition = FormStartPosition.CenterParent
		};
		var panel = new MSSQLConnectionPanel { Dock = DockStyle.Fill, ConnectionString = initialConnectionString };
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

`AddBar` transfers the control's lifetime to `host`; disposing the host disposes its child controls. `TryEditConnection` disposes its dialog and panel, preserves the original string on cancellation, and returns the edited string on OK. Acceptance only collects settings. Use `bar.GetDAC()` or `new Sphere10.Framework.Data.MSSQLDAC(connectionString)` when the application is ready to access the database.

## Shared provider selection

To use the generic `DatabaseConnectionBar` or `DatabaseConnectionPanel`, add `Sphere10.Framework.Windows.Forms.MSSQL.ModuleConfiguration` with `UseModule<TModule>()` in the existing framework startup builder, before starting the application and creating those generic controls. This registers named transient bars and panels under `nameof(DBMSType.SQLServer)`.

After startup, set `SelectedDBMSType = DBMSType.SQLServer` before assigning the generic control's `ConnectionString`. Merely referencing the NuGet package does not register the services. A directly constructed `MSSQLConnectionBar` or `MSSQLConnectionPanel` does not need this provider lookup.

## Events and connection lifecycle

- Both concrete editors inherit `StateChanged` from `UserControlEx`. Subscribe with a parameterless handler, for example `bar.StateChanged += () => ...`, to react to changes; it is not a connection-success event.
- The generic `DatabaseConnectionPanel.DBMSTypeChanged` event receives the panel and selected `DBMSType`. It reports provider changes. The generic bar has no equivalent public provider-change event.
- Call `await bar.TestConnection()` from a UI event handler and inspect the returned `Result.IsSuccess`. The control captures settings on the UI thread and opens the scope on a worker task. Connection failures become result errors; settings parsing and artificial-key loading run outside the connection-error handler and can throw separately, so handle those exceptions in the host.
- No connection stays open after a successful test. Obtain a DAC for application operations and dispose the connection or DAC scope used by each operation. The controls do not create databases or manage application transactions.

## SQL Server behavior and limitations

The editors use `Microsoft.Data.SqlClient.SqlConnectionStringBuilder`, and `MSSQLDAC` creates `Microsoft.Data.SqlClient.SqlConnection` instances. Both editors explicitly emit `Integrated Security=False`: these controls edit SQL username/password authentication, even when an assigned string originally used Windows or Microsoft Entra authentication.

Only server, port, catalog, user and password survive editing. Options such as `Encrypt`, `TrustServerCertificate`, pooling and `Authentication` are not preserved by a `ConnectionString` round trip. If your application needs those options, apply them to the returned string with `SqlConnectionStringBuilder` and create the DAC from that final string, or use a custom editor. Do not feed that final string back through the limited editor expecting those settings to survive.

An omitted encryption option follows `MSSQLDAC`'s compatibility policy; see the [SQL Server provider guide](../Sphere10.Framework.Data.MSSQL/README.md) for encryption defaults and certificate validation. The bar parses its port strictly; the panel uses a safe parser and treats unparseable port text as unset. Validate port input in the host if acceptance must reject it. Connection strings can contain the entered password, so avoid displaying or logging them.

## Build and source

From the repository root on Windows:

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.MSSQL/Sphere10.Framework.Windows.Forms.MSSQL.csproj -c Release
```

- [MSSQLConnectionBar](MSSQLConnectionBar.cs) and [MSSQLConnectionPanel](MSSQLConnectionPanel.cs): field mapping and DAC construction.
- [ModuleConfiguration](ModuleConfiguration.cs): registration names.
- [Connection provider interface](../Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs), [bar base](../Sphere10.Framework.Windows.Forms/Database/ConnectionBarBase.cs), and [panel base](../Sphere10.Framework.Windows.Forms/Database/ConnectionPanelBase.cs): shared lifecycle.

## Related guides

- [Core WinForms](../Sphere10.Framework.Windows.Forms/README.md)
- [SQL Server data provider](../Sphere10.Framework.Data.MSSQL/README.md)
- [Application startup and modules](../Sphere10.Framework.Application/README.md)
- [SQLite controls](../Sphere10.Framework.Windows.Forms.Sqlite/README.md) and [Firebird controls](../Sphere10.Framework.Windows.Forms.Firebird/README.md)

## License & Author

**License**: [MIT License](../../LICENSE)

**Author**: Herman Schoenfeld (sphere10.com)

**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
