<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.Firebird

Firebird connection editors for Windows Forms, covering both server connections and embedded database files. The package provides compact bars and dialog-sized panels that return `FirebirdDAC` instances through the shared `IDatabaseConnectionProvider` API.

## Installation and requirements

```powershell
dotnet add package Sphere10.Framework.Windows.Forms.Firebird
```

Use a Windows desktop application targeting `net10.0-windows`, with the .NET 10 SDK and Windows Forms enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Create and access the controls on your application's STA UI thread with a WinForms message loop. The package references its matching data provider and `Sphere10.Framework.Windows.Forms`; install the UI package into the desktop project that hosts the controls.

The matching data package uses `FirebirdSql.Data.FirebirdClient`. Server mode requires a reachable Firebird server. Embedded mode requires the Firebird native engine/client components for the deployed process architecture; a connection editor and the managed provider alone do not provision an embedded server or create its database.

## Choose the correct control

All four concrete types are in `Sphere10.Framework.Windows.Forms.Firebird`.

| Mode | Bar | Panel | Generic selector key |
| --- | --- | --- | --- |
| Server | `FirebirdConnectionBar` | `FirebirdConnectionPanel` | `DBMSType.Firebird` |
| Embedded file | `FirebirdEmbeddedConnectionBar` | `FirebirdEmbeddedConnectionPanel` | `DBMSType.FirebirdFile` |

Server controls expose editable `Server`, `Port`, `Database`, `Username` and `Password` properties. `Port` is text and is parsed when generating the string. `DatabaseName` returns the `Database` value. The generated provider string uses `FbServerType.Default`; the database path or alias is interpreted by the Firebird server.

Embedded controls expose read-only `Filename`, `Password` and `HasPassword`, plus an editable `Mode` for the file selector. Populate the filename, username and password through `ConnectionString` or the UI; there is no public embedded `Username` property. `DatabaseName` returns the filename without its extension, or null when no path is selected. These controls always emit `FbServerType.Embedded`.

All controls support `ConnectionString`, `GetDAC()` and `TestConnection()`. The bars also inherit `ArtificialKeysFile`; panels do not expose that setting.

## Add a server bar or show an embedded connection dialog

Pass a server connection string to `AddServerBar`, for example one produced by `FbConnectionStringBuilder` with the server, database alias and application credentials. For the embedded dialog, pass a string with `Database`, `User ID` and any required password. These methods belong in an existing WinForms application running on an STA UI thread.

```csharp
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.Firebird;

public static class FirebirdConnectionUi {
	public static FirebirdConnectionBar AddServerBar(Control host, string connectionString) {
		var bar = new FirebirdConnectionBar { Dock = DockStyle.Top, ConnectionString = connectionString };
		host.Controls.Add(bar);
		return bar;
	}

	public static bool TryEditEmbeddedConnection(IWin32Window owner, string initialConnectionString, out string connectionString) {
		using var dialog = new Form {
			Text = "Embedded Firebird connection",
			Width = 650,
			Height = 210,
			StartPosition = FormStartPosition.CenterParent
		};
		var panel = new FirebirdEmbeddedConnectionPanel {
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

The package supplies the panels; the dialog is composed with the standard WinForms `Form.ShowDialog(owner)` API. Use `FirebirdConnectionPanel` in the same pattern for server settings, or `FirebirdEmbeddedConnectionBar` for a compact file editor.

The host owns the added bar. The dialog and its child controls are disposed after closing, and cancellation leaves the original string unchanged. OK collects settings without connecting. Use `bar.GetDAC()` or `new Sphere10.Framework.Data.FirebirdDAC(connectionString)` afterwards; opening a connection is a separate application action.

## Shared provider selection

Register `Sphere10.Framework.Windows.Forms.Firebird.ModuleConfiguration` with `UseModule<TModule>()` in the existing framework startup builder. It registers both bar/panel pairs as named transient services under `nameof(DBMSType.Firebird)` and `nameof(DBMSType.FirebirdFile)`.

Start the framework before constructing a generic `DatabaseConnectionBar` or `DatabaseConnectionPanel`. Set `SelectedDBMSType` to the required mode before assigning its `ConnectionString`. Package installation alone does not register the module; directly constructed Firebird controls do not use this lookup.

## Events and connection lifecycle

- The concrete controls inherit the parameterless `UserControlEx.StateChanged` event. It reports changes to UI state, not successful connections.
- The generic `DatabaseConnectionPanel.DBMSTypeChanged` event supplies the panel and selected `DBMSType`; the generic bar has no corresponding public event.
- `GetDAC()` creates a new DAC and, on a bar with `ArtificialKeysFile` configured, loads that XML file. It does not connect to Firebird.
- `await control.TestConnection()` opens a temporary DAC scope on a worker task and disposes it before returning a `Result`. Check `Result.IsSuccess`. Connection exceptions become result errors, but connection-string parsing and artificial-key loading can throw before that handler runs.
- Call control methods from the UI thread. Dispose the host when finished and use disposable DAC scopes or connections for subsequent database operations. These controls do not retain a connection after testing or manage transactions.

## Provider behavior and limitations

The editors rebuild connection strings from their visible fields. Server controls retain server, port, database, username and password; embedded controls retain filename, username and password. Additional options such as character set, role, pooling and native client-library location are not preserved. Apply such options to the edited string with `FbConnectionStringBuilder` before constructing a DAC directly.

Changing the server-type entry in an input string does not change the editor's mode: server controls output `Default` and embedded controls output `Embedded`. Select the matching control or `DBMSType` instead. Invalid port text can cause connection-string generation to throw. For embedded usage, selecting a filename does not create a database or install native dependencies. Use the [Firebird data provider guide](../Sphere10.Framework.Data.Firebird/README.md) for creation and provider setup. Connection strings may contain passwords and should not be logged.

## Build and source

From the repository root on Windows:

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.Firebird/Sphere10.Framework.Windows.Forms.Firebird.csproj -c Release
```

- [Server bar](FirebirdConnectionBar.cs) and [server panel](FirebirdConnectionPanel.cs).
- [Embedded bar](FirebirdEmbeddedConnectionBar.cs) and [embedded panel](FirebirdEmbeddedConnectionPanel.cs).
- [ModuleConfiguration](ModuleConfiguration.cs): both provider-mode registrations.
- [Connection provider interface](../Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs), [bar base](../Sphere10.Framework.Windows.Forms/Database/ConnectionBarBase.cs), and [panel base](../Sphere10.Framework.Windows.Forms/Database/ConnectionPanelBase.cs).

## Related guides

- [Core WinForms](../Sphere10.Framework.Windows.Forms/README.md)
- [Firebird data provider](../Sphere10.Framework.Data.Firebird/README.md)
- [Application startup and modules](../Sphere10.Framework.Application/README.md)
- [SQL Server controls](../Sphere10.Framework.Windows.Forms.MSSQL/README.md) and [SQLite controls](../Sphere10.Framework.Windows.Forms.Sqlite/README.md)

## License & Author

**License**: [MIT License](../../LICENSE)

**Author**: Herman Schoenfeld (sphere10.com)

**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
