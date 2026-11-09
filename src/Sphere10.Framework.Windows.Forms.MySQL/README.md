<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.MySQL

`Sphere10.Framework.Windows.Forms.MySQL` provides Windows Forms editors for MySQL connection settings in desktop applications that use Sphere10 Framework data access. Use `MySQLConnectionBar` where a compact one-row editor fits, and `MySQLConnectionPanel` on a settings page or in a dialog. Both build a MySqlConnector connection string, test it on request and create a `MySQLDAC` from it.

## Reference the package

Add the package from your configured feed, and keep its version in step with your other Sphere10 Framework packages:

```powershell
dotnet add package Sphere10.Framework.Windows.Forms.MySQL
```

The package depends on `Sphere10.Framework.Data.MySQL`, which supplies `MySQLDAC` and the `MySqlConnector` driver, and on `Sphere10.Framework.Windows.Forms`. To build against source instead, reference `src/Sphere10.Framework.Windows.Forms.MySQL/Sphere10.Framework.Windows.Forms.MySQL.csproj` with a `ProjectReference`.

The project targets `net10.0-windows`, and a build with a .NET 11 SDK also targets `net11.0-windows`. A consuming project needs a Windows target framework with Windows Forms enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Create and use the controls on the application's UI thread. Creating or editing a control never connects, so a MySQL server is needed only to test or open a connection.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Windows.Forms.MySQL` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | Registers `MySQLConnectionBar` and `MySQLConnectionPanel` as named transient `ConnectionBarBase` and `ConnectionPanelBase` services under `DBMSType.MySQL`. |
| [`MySQLConnectionBar`](MySQLConnectionBar.cs) | class | One-row `ConnectionBarBase` editor for the MySQL server, port, database, user name and password that builds the connection string and creates a `MySQLDAC`. |
| [`MySQLConnectionPanel`](MySQLConnectionPanel.cs) | class | Larger `ConnectionPanelBase` editor with the same fields as `MySQLConnectionBar`, for a settings page or dialog. |
<!-- domains:end -->

## Controls

Both controls are in the `Sphere10.Framework.Windows.Forms.MySQL` namespace and edit the same five fields.

| Control | Base class | Use |
|---|---|---|
| `MySQLConnectionBar` | `ConnectionBarBase` | One-row editor for a toolbar or header area. Its options button can select an artificial-keys file. |
| `MySQLConnectionPanel` | `ConnectionPanelBase` | Larger editor for a settings page or a dialog. |

| Member | Behavior |
|---|---|
| `Server` | Server name, mapped to `MySqlConnectionStringBuilder.Server`. |
| `Database`, `DatabaseName` | Database name. `DatabaseName` returns the `Database` text and does not query the server. |
| `Username`, `Password` | Account name and password, mapped to `UserID` and `Password`. The password box masks its text, but both properties return plain text. |
| `Port` | Port as text. A blank port is left out of the string, so the driver's default of 3306 applies. |
| `ConnectionString` | Builds a string from the five fields, or fills the fields from an assigned string. Loading port 3306 leaves the port box blank. |
| `GetDAC()` | Returns a new `MySQLDAC` as `IDAC` without opening a connection. |
| `TestConnection()` | Opens and closes a test connection, and returns `Task<Result>`. |
| `ArtificialKeysFile` | Bar only. Path of an artificial-keys XML file. The options button sets it through `SelectArtificialKeysFile()` and can clear it again. `GetDAC()` loads the file into each new DAC, so that `GetSchema()` includes the keys it declares. |

`ConnectionString` is hidden from the Windows Forms designer, but the field properties are not. Assign credentials at runtime so that they never reach designer-generated code.

## Edit and test a connection

Place the panel on a designer-authored dialog, form or screen like any other control, and host `MySQLConnectionBar` the same way where space is limited. The code-behind below belongs to a dialog whose designer file declares `_connectionPanel` (a `MySQLConnectionPanel`), `_testButton` and `_statusLabel`, wires `_testButton.Click`, and sets the `DialogResult` of its OK and Cancel buttons.

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

The editors only collect settings. They do not save profiles, discover servers or create databases, and they never hold a connection:

- `TestConnection()` builds the connection string on the calling thread, then opens and disposes a DAC scope on a thread-pool task. A connection failure becomes an error in the returned `Result`, and no connection stays open afterwards. A successful test shows that a connection opened, not that the application's tables or permissions are in place. The method takes no cancellation token.
- Building the string can throw before the test starts. The bar throws a `FormatException` for port text that is not a number, and loading its `ArtificialKeysFile` can also fail.
- When the application needs the database, call `GetDAC()` on the editor or construct `new MySQLDAC(connectionString)`, then call `EnterScope()` for each unit of work and dispose the scope. The DAC itself is not disposable, and the scope owns the connection.
- `StateChanged`, inherited from `UserControlEx`, is a parameterless event that is raised when a field changes after the control has loaded. It reports edits, not connectivity.

For example, this unit of work deletes the stored sessions in one transaction on the accepted string:

```csharp
using Sphere10.Framework.Data;

var dac = new MySQLDAC(savedConnectionString);
using (var scope = dac.EnterScope()) {
	scope.BeginTransaction();
	dac.ExecuteNonQuery("DELETE FROM sessions");
	scope.Commit();
}
```

Disposing the scope does not commit, so call `Commit()` explicitly.

## Host an editor in code

The editors also work without the designer. Use this as `Program.cs` in a Windows Forms application that references the package. Nothing connects on startup, and **Test connection** contacts the server with the entered settings.

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.MySQL;
using Application = System.Windows.Forms.Application;

namespace ReadmeExamples.MySQLDirect;

internal static class Program {
	[STAThread]
	private static void Main() {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		using var window = new Form { Text = "MySQL connection", ClientSize = new Size(640, 260) };
		var editor = new MySQLConnectionPanel {
			Dock = DockStyle.Fill,
			Server = "localhost",
			Database = "app_data",
			Username = "app_user"
		};
		var status = new Label { Dock = DockStyle.Bottom, Height = 44, Text = "Enter your connection details." };
		var testButton = new Button { Dock = DockStyle.Bottom, Height = 32, Text = "Test connection" };
		testButton.Click += async (sender, args) => {
			testButton.Enabled = false;
			try {
				var result = await editor.TestConnection();
				if (!window.IsDisposed)
					status.Text = result.IsSuccess ? "Connection succeeded." : string.Join(Environment.NewLine, result.ErrorMessages);
			} catch (Exception error) {
				// Settings that cannot form a connection string throw instead of failing the result.
				if (!window.IsDisposed)
					status.Text = error.Message;
			} finally {
				if (!testButton.IsDisposed)
					testButton.Enabled = true;
			}
		};
		window.Controls.Add(editor);
		window.Controls.Add(status);
		window.Controls.Add(testButton);
		Application.Run(window);
	}
}
```

The form owns and disposes the child controls. To use the bar instead, host `MySQLConnectionBar` the same way and call the same `ConnectionString`, `GetDAC()` and `TestConnection()` members.

## Connection string behavior

- The editors rebuild the string from their five fields, so other settings in an assigned string are dropped, such as pooling, timeouts or TLS options. To keep such settings, read `ConnectionString`, add them with `MySqlConnectionStringBuilder`, and construct `MySQLDAC` from the result. `GetDAC()` and `TestConnection()` always use the rebuilt string.
- The bar parses the port strictly. The panel leaves out port text that it cannot parse, so validate the port in the application when invalid input must be rejected.
- The setter does not catch errors from `MySqlConnectionStringBuilder`, so assigning a string that it rejects throws.
- Connection strings contain the password, so do not log or display them.

## Use the generic provider selector

`DatabaseConnectionPanel` and `DatabaseConnectionBar` in `Sphere10.Framework.Windows.Forms` let the user choose a database engine and then show that engine's editor. They resolve the editors as named framework services, so add this package's module to the application's startup code:

```csharp
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	// ... the application's existing configuration and modules ...
	.AddModule<Sphere10.Framework.Windows.Forms.MySQL.ModuleConfiguration>()
	.StartWinFormsApplication();
```

`ModuleConfiguration` registers `MySQLConnectionBar` and `MySQLConnectionPanel` as transient services named `nameof(DBMSType.MySQL)`. Referencing the package does not register them, and a directly hosted MySQL editor needs no module.

- A selector constructed before the framework has started, for example in the Windows Forms designer, finds no service provider and lists no engines, so construct it after startup. Switching engines disposes the previous editor.
- The engine list contains only registered providers. Setting `SelectedDBMSType` to an unregistered engine leaves the current editor selected.
- Set `SelectedDBMSType = DBMSType.MySQL` before assigning `ConnectionString`, because the selector passes the string to its current editor.
- `Database` returns the engine and the string together as a `DBReference`, and the bar also accepts one. `DACFactory.CreateDAC(reference.DBMSType, reference.ConnectionString)` recreates the DAC from a saved reference.
- `DatabaseConnectionPanel` raises `DBMSTypeChanged(panel, dbmsType)` when the selected engine changes. `DatabaseConnectionBar` has no such event.

This complete `Program.cs` starts only the modules that the selector needs, shows it with MySQL selected and ends the framework after the window closes:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;
using ProviderModule = Sphere10.Framework.Windows.Forms.MySQL.ModuleConfiguration;
using WinFormsModule = Sphere10.Framework.Windows.Forms.ModuleConfiguration;
using Application = System.Windows.Forms.Application;

namespace ReadmeExamples.MySQLSelector;

internal static class Program {
	[STAThread]
	private static void Main() {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		var framework = Sphere10Framework.Instance;
		framework.Build()
			.AddModule<WinFormsModule>()
			.AddModule<ProviderModule>()
			.Start();
		using var shutdown = Tools.Scope.ExecuteOnDispose(framework.EndFramework);
		using var window = new Form { Text = "Database connection", ClientSize = new Size(660, 220) };
		var selector = new DatabaseConnectionPanel {
			Dock = DockStyle.Fill,
			SelectedDBMSType = DBMSType.MySQL
		};
		window.Controls.Add(selector);
		Application.Run(window);
	}
}
```

In an existing framework application, add the module to its startup builder instead of starting a second framework. Register the modules of other providers to add their engines to the selector.

Save the selector's `Database` to keep the engine with the string, and recreate the DAC from it later:

```csharp
using Sphere10.Framework.Data;

var saved = selector.Database;

var dac = DACFactory.CreateDAC(saved.DBMSType, saved.ConnectionString);
using (dac.EnterScope()) {
	var customers = dac.ExecuteScalar<long>("SELECT COUNT(*) FROM customers");
}
```

## Try the controls

The [WinForms Tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md) hosts both editors. Run it from the repository root on Windows:

```powershell
dotnet run --project utils/Sphere10.Framework.Utils.WinFormsTester -c Release -p:BuildRevision=0
```

- **Controls > Input > All controls by class name** opens the control catalog. Search for `MySQLConnection` to edit sample settings, which never connect on their own.
- **Integration > Connections > Database Connection Panel** and **Database Connection Bar** show the generic selectors with every provider module registered. Choose **MySQL**, enter the settings of a reachable server and click **Test connection**.

## Build from source

Build the project and pack it on its own from the repository root on Windows:

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.MySQL/Sphere10.Framework.Windows.Forms.MySQL.csproj -c Release -p:BuildRevision=0
dotnet pack src/Sphere10.Framework.Windows.Forms.MySQL/Sphere10.Framework.Windows.Forms.MySQL.csproj -c Release --no-build -p:BuildRevision=0
```

`-p:BuildRevision=0` keeps a development build from taking the next local build number. The [root README](../../README.md#building-and-publishing-packages) explains build numbering, and how `scripts/pack.ps1` packs the whole package set. The project packs this file as its NuGet readme, and [`Directory.Packages.props`](../../Directory.Packages.props) sets the driver version.

- [`MySQLConnectionBar.cs`](MySQLConnectionBar.cs) and [`MySQLConnectionPanel.cs`](MySQLConnectionPanel.cs) map the fields to `MySqlConnectionStringBuilder`.
- [`ModuleConfiguration.cs`](ModuleConfiguration.cs) registers the named services.
- [`ConnectionBarBase`](../Sphere10.Framework.Windows.Forms/Database/Controls/ConnectionBarBase.cs) and [`ConnectionPanelBase`](../Sphere10.Framework.Windows.Forms/Database/Controls/ConnectionPanelBase.cs) implement [`IDatabaseConnectionProvider`](../Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs), including the shared `GetDAC()` and `TestConnection()` logic.

## Related documentation

- [Windows Forms library](../Sphere10.Framework.Windows.Forms/README.md) for the generic selectors and the base classes.
- [MySQL data provider](../Sphere10.Framework.Data.MySQL/README.md) for `MySQLDAC`, `Tools.MySQL` and the provider's limits.
- [Data access](../Sphere10.Framework.Data/README.md) for DAC scopes, transactions and [artificial keys](../Sphere10.Framework.Data/README.md#artificial-keys).
- [Application startup and modules](../Sphere10.Framework.Application/README.md).
- [Database connection editors](../../docs/guidelines/patterns/winforms-controls.md#database-connection-editors), which compares the editors of every provider.
- The other provider editors for [SQL Server](../Sphere10.Framework.Windows.Forms.MSSQL/README.md), [SQLite](../Sphere10.Framework.Windows.Forms.Sqlite/README.md), [Firebird](../Sphere10.Framework.Windows.Forms.Firebird/README.md), [PostgreSQL](../Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) and [Oracle](../Sphere10.Framework.Windows.Forms.Oracle/README.md).

## License and author

**License**: [MIT License](../../LICENSE)

**Author**: Herman Schoenfeld (sphere10.com)

**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
