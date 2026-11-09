<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.Firebird

`Sphere10.Framework.Windows.Forms.Firebird` provides Windows Forms editors for Firebird connection settings in desktop applications that use Sphere10 Framework data access. It covers a Firebird server and an embedded database file, each with a compact one-row bar and a larger panel for a settings page or a dialog. All four editors build a `FirebirdSql.Data.FirebirdClient` connection string, test it on request and create a `FirebirdDAC` from it.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Windows.Forms.Firebird` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`FirebirdConnectionBar`](FirebirdConnectionBar.cs) | class | One-row `ConnectionBarBase` editor for a Firebird server, port, database, user name and password that builds an `FbConnectionStringBuilder` connection string and creates a `FirebirdDAC`. |
| [`FirebirdConnectionPanel`](FirebirdConnectionPanel.cs) | class | Larger `ConnectionPanelBase` editor with the same fields as `FirebirdConnectionBar`, for a settings page or dialog. |
| [`FirebirdEmbeddedConnectionBar`](FirebirdEmbeddedConnectionBar.cs) | class | One-row editor for an embedded Firebird database file, user name and password that builds an embedded-server connection string and exposes `Filename`, `Password` and the file selector's `Mode`. |
| [`FirebirdEmbeddedConnectionPanel`](FirebirdEmbeddedConnectionPanel.cs) | class | Larger editor with the same fields as `FirebirdEmbeddedConnectionBar`, for a settings page or dialog. |
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | Registers the server editors under `DBMSType.Firebird` and the embedded editors under `DBMSType.FirebirdFile` as named transient `ConnectionBarBase` and `ConnectionPanelBase` services. |
<!-- domains:end -->

## Reference the package

Add the package from your configured feed, and keep its version in step with your other Sphere10 Framework packages:

```powershell
dotnet add package Sphere10.Framework.Windows.Forms.Firebird
```

The package depends on `Sphere10.Framework.Data.Firebird`, which supplies `FirebirdDAC` and the `FirebirdSql.Data.FirebirdClient` driver, and on `Sphere10.Framework.Windows.Forms`. To build against source instead, reference `src/Sphere10.Framework.Windows.Forms.Firebird/Sphere10.Framework.Windows.Forms.Firebird.csproj` with a `ProjectReference`.

The project targets `net10.0-windows`, and a build with a .NET 11 SDK also targets `net11.0-windows`. A consuming project needs a Windows target framework with Windows Forms enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Create and use the controls on the application's UI thread. Creating or editing a control never connects, so a Firebird server is needed only to test or open a server connection. Embedded mode runs the Firebird engine inside the application process, so the application must deploy Firebird's native client and engine files for its architecture. The managed driver package does not include them.

## Controls

All four controls are in the `Sphere10.Framework.Windows.Forms.Firebird` namespace. The bars derive from `ConnectionBarBase` and fit a toolbar or header area, and their options button can select an artificial-keys file. The panels derive from `ConnectionPanelBase`.

| Mode | Bar | Panel | Server type | Selector key |
|---|---|---|---|---|
| Server | `FirebirdConnectionBar` | `FirebirdConnectionPanel` | `FbServerType.Default` | `DBMSType.Firebird` |
| Embedded file | `FirebirdEmbeddedConnectionBar` | `FirebirdEmbeddedConnectionPanel` | `FbServerType.Embedded` | `DBMSType.FirebirdFile` |

| Member | Editors | Behavior |
|---|---|---|
| `Server` | Server | Server host, mapped to `FbConnectionStringBuilder.DataSource`. |
| `Port` | Server | Port as text. A blank port is left out of the string. Loading a string shows the builder's port, which is the driver default when the string has none. |
| `Database`, `DatabaseName` | Server | Database path or alias, as the server resolves it. `DatabaseName` returns the `Database` text and does not query the server. |
| `Username`, `Password` | Server | Account name and password, mapped to `UserID` and `Password`. The password box masks its text, but both properties return plain text. |
| `Filename` | Embedded | Read-only path of the database file. Set it through `ConnectionString` or the file selector. |
| `Mode` | Embedded | `PathSelectionMode` of the file selector, such as `OpenFile` for an existing file. |
| `Password`, `HasPassword` | Embedded | Read-only password text, and whether it contains a non-whitespace character. |
| `DatabaseName` | Embedded | File name without its extension, or `null` when no file is selected. |
| `ConnectionString` | All | Builds a string from the editor's fields and server type, or fills the fields from an assigned string. |
| `GetDAC()` | All | Returns a new `FirebirdDAC` as `IDAC` without opening a connection. |
| `TestConnection()` | All | Opens and closes a test connection, and returns `Task<Result>`. |
| `ArtificialKeysFile` | Bars | Path of an artificial-keys XML file. The options button sets it through `SelectArtificialKeysFile()` and can clear it again. `GetDAC()` loads the file into each new DAC, so that `GetSchema()` includes the keys it declares. |

The embedded editors also have a **Username** box but no public `Username` property, so set the user name through `ConnectionString` or the UI. `ConnectionString` is hidden from the Windows Forms designer, but the server editors' field properties are not. Assign credentials at runtime so that they never reach designer-generated code.

## Edit and test a connection

Place a panel on a designer-authored dialog, form or screen like any other control, and host a bar the same way where space is limited. The code-behind below belongs to a dialog whose designer file declares `_connectionPanel` (a `FirebirdConnectionPanel`), `_testButton` and `_statusLabel`, wires `_testButton.Click`, and sets the `DialogResult` of its OK and Cancel buttons.

```csharp
using System;
using System.ComponentModel;
using System.Windows.Forms;

public partial class DatabaseSettingsDialog : Form {
	public DatabaseSettingsDialog() {
		InitializeComponent();
	}

	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string ConnectionString {
		get => _connectionPanel.ConnectionString;
		set => _connectionPanel.ConnectionString = value;
	}

	private async void _testButton_Click(object sender, EventArgs e) {
		_testButton.Enabled = false;
		try {
			var result = await _connectionPanel.TestConnection();
			if (!IsDisposed)
				_statusLabel.Text = result.IsSuccess ? "Connection succeeded." : string.Join(Environment.NewLine, result.ErrorMessages);
		} catch (Exception error) {
			// Settings that cannot form a connection string throw instead of failing the result.
			if (!IsDisposed)
				_statusLabel.Text = error.Message;
		} finally {
			if (!IsDisposed)
				_testButton.Enabled = true;
		}
	}
}
```

Show the dialog with the saved string, and keep the edited string only when the user accepts it:

```csharp
using var dialog = new DatabaseSettingsDialog { ConnectionString = savedConnectionString };
if (dialog.ShowDialog(owner) == DialogResult.OK)
	savedConnectionString = dialog.ConnectionString;
```

A `FirebirdEmbeddedConnectionPanel` works the same way. Set its `Mode` to `PathSelectionMode.OpenFile` in the designer when the user must choose an existing file.

The editors only collect settings. They do not save profiles, discover servers or create databases, and they never hold a connection:

- `TestConnection()` builds the connection string on the calling thread, then opens and disposes a DAC scope on a thread-pool task. A connection failure becomes an error in the returned `Result`, and no connection stays open afterwards. A successful test shows that a connection opened, not that the application's tables or permissions are in place. The method takes no cancellation token.
- Building the string can throw before the test starts. Both server editors throw a `FormatException` for port text that is not a number, and loading a bar's `ArtificialKeysFile` can also fail.
- When the application needs the database, call `GetDAC()` on the editor or construct `new FirebirdDAC(connectionString)`, then call `EnterScope()` for each unit of work and dispose the scope. The DAC itself is not disposable, and the scope owns the connection.
- `StateChanged`, inherited from `UserControlEx`, is a parameterless event that is raised when a field changes after the control has loaded. It reports edits, not connectivity.

## Connection string behavior

- Each editor writes its own server type, whatever the assigned string contained. Choose the editor, or the selector key, that matches the deployment.
- The editors rebuild the string from their fields, so other settings in an assigned string are dropped, such as the character set, role, pooling or client-library location. To keep such settings, read `ConnectionString`, add them with `FbConnectionStringBuilder`, and construct `FirebirdDAC` from the result. `GetDAC()` and `TestConnection()` always use the rebuilt string.
- Both server editors parse the port strictly, so validate the port text before reading `ConnectionString` when the user should see a friendlier message than the `FormatException`.
- Choosing a file does not create a database. Create the database separately before testing a connection to it.
- The setters leave a field blank when `FbConnectionStringBuilder` cannot return its value. A string that the builder rejects outright still throws.
- Connection strings contain the password, so do not log or display them.

## Use the generic provider selector

`DatabaseConnectionPanel` and `DatabaseConnectionBar` in `Sphere10.Framework.Windows.Forms` let the user choose a database engine and then show that engine's editor. They resolve the editors as named framework services, so add this package's module to the application's startup code:

```csharp
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	// ... the application's existing configuration and modules ...
	.AddModule<Sphere10.Framework.Windows.Forms.Firebird.ModuleConfiguration>()
	.StartWinFormsApplication();
```

`ModuleConfiguration` registers the server editors under `nameof(DBMSType.Firebird)` and the embedded editors under `nameof(DBMSType.FirebirdFile)`, as transient services. The selectors list them as **Firebird** and **Firebird Embedded**, and `IgnoreDBMS = new[] { DBMSType.FirebirdFile }` hides the embedded entry. Referencing the package does not register the editors, and a directly hosted Firebird editor needs no module.

- A selector constructed before the framework has started, for example in the Windows Forms designer, finds no service provider and lists no engines, so construct it after startup. Switching engines disposes the previous editor.
- The engine list contains only registered providers. Setting `SelectedDBMSType` to an unregistered engine leaves the current editor selected.
- Set `SelectedDBMSType` to `DBMSType.Firebird` or `DBMSType.FirebirdFile` before assigning `ConnectionString`, because the selector passes the string to its current editor.
- `Database` returns the engine and the string together as a `DBReference`, and the bar also accepts one. `DACFactory.CreateDAC(reference.DBMSType, reference.ConnectionString)` recreates the DAC from a saved reference.
- `DatabaseConnectionPanel` raises `DBMSTypeChanged(panel, dbmsType)` when the selected engine changes. `DatabaseConnectionBar` has no such event.

## Try the controls

The [WinForms Tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) hosts all four editors. Run it from the repository root on Windows:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.WinFormsTester -c Release -p:BuildRevision=0
```

- **Controls > Input > All controls by class name** opens the control catalog. Search for `Firebird` to edit sample settings in each editor, which never connect on their own.
- **Integration > Connections > Database Connection Panel** and **Database Connection Bar** show the generic selectors with every provider module registered. Choose **Firebird** or **Firebird Embedded**, enter real settings and click **Test connection**.

## Build from source

From the repository root on Windows:

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.Firebird/Sphere10.Framework.Windows.Forms.Firebird.csproj -c Release -p:BuildRevision=0
```

`-p:BuildRevision=0` keeps a development build from taking the next local build number. The [root README](../../README.md#building-and-publishing-packages) explains build numbering, and how `scripts/pack.ps1` packs the whole package set. The project packs this file as its NuGet readme, and [`Directory.Packages.props`](../../Directory.Packages.props) sets the driver version.

- [`FirebirdConnectionBar.cs`](FirebirdConnectionBar.cs) and [`FirebirdConnectionPanel.cs`](FirebirdConnectionPanel.cs) are the server editors.
- [`FirebirdEmbeddedConnectionBar.cs`](FirebirdEmbeddedConnectionBar.cs) and [`FirebirdEmbeddedConnectionPanel.cs`](FirebirdEmbeddedConnectionPanel.cs) are the embedded editors.
- [`ModuleConfiguration.cs`](ModuleConfiguration.cs) registers both modes.
- [`ConnectionBarBase`](../Sphere10.Framework.Windows.Forms/Database/Controls/ConnectionBarBase.cs) and [`ConnectionPanelBase`](../Sphere10.Framework.Windows.Forms/Database/Controls/ConnectionPanelBase.cs) implement [`IDatabaseConnectionProvider`](../Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs), including the shared `GetDAC()` and `TestConnection()` logic.

## Related documentation

- [Windows Forms library](../Sphere10.Framework.Windows.Forms/README.md) for the generic selectors and the base classes.
- [Firebird data provider](../Sphere10.Framework.Data.Firebird/README.md) for `FirebirdDAC`.
- [Data access](../Sphere10.Framework.Data/README.md) for DAC scopes, transactions and [artificial keys](../Sphere10.Framework.Data/README.md#artificial-keys).
- [Application startup and modules](../Sphere10.Framework.Application/README.md).
- [Database connection editors](../../docs/guidelines/patterns/winforms-controls.md#database-connection-editors), which compares the editors of every provider.
- The other provider editors for [SQL Server](../Sphere10.Framework.Windows.Forms.MSSQL/README.md), [SQLite](../Sphere10.Framework.Windows.Forms.Sqlite/README.md), [MySQL](../Sphere10.Framework.Windows.Forms.MySQL/README.md), [PostgreSQL](../Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) and [Oracle](../Sphere10.Framework.Windows.Forms.Oracle/README.md).

## License and author

**License**: [MIT License](../../LICENSE)

**Author**: Herman Schoenfeld (sphere10.com)

**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
