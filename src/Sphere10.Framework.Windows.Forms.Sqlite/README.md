<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# Sphere10.Framework.Windows.Forms.Sqlite

`Sphere10.Framework.Windows.Forms.Sqlite` provides Windows Forms editors that select a SQLite database file and its password, journal mode and synchronization mode, for desktop applications that use Sphere10 Framework data access. Use `SqliteConnectionBar` where a compact one-row editor fits, and `SqliteConnectionPanel` on a settings page or in a dialog. Both build a `System.Data.SQLite` connection string, test it on request and create a `SqliteDAC` from it.

## Reference the package

Add the package from your configured feed, and keep its version in step with your other Sphere10 Framework packages:

```powershell
dotnet add package Sphere10.Framework.Windows.Forms.Sqlite
```

The package depends on `Sphere10.Framework.Data.Sqlite`, which supplies `SqliteDAC`, `Tools.Sqlite`, the `System.Data.SQLite` driver and the `SourceGear.sqlite3` package of native SQLite libraries, and on `Sphere10.Framework.Windows.Forms`. To build against source instead, reference `src/Sphere10.Framework.Windows.Forms.Sqlite/Sphere10.Framework.Windows.Forms.Sqlite.csproj` with a `ProjectReference`.

The project targets `net10.0-windows`, and a build with a .NET 11 SDK also targets `net11.0-windows`. The SQLite data provider itself targets `net10.0`, but these editors need Windows. A consuming project needs a Windows target framework with Windows Forms enabled:

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Create and use the controls on the application's UI thread. Creating or editing a control never opens the database file. The application's output must include the native SQLite library for its runtime architecture, which `SourceGear.sqlite3` provides.

<!-- domains:begin -->
## Domains

Every folder of `Sphere10.Framework.Windows.Forms.Sqlite` is a domain with its own README, which lists all of its public types.

| Domain | What it provides | Source |
|---|---|---|

### Project root

Types declared in the project folder itself:

| Type | Kind | Purpose |
|---|---|---|
| [`ModuleConfiguration`](ModuleConfiguration.cs) | class | Registers `SqliteConnectionBar` and `SqliteConnectionPanel` as named transient `ConnectionBarBase` and `ConnectionPanelBase` services under `DBMSType.Sqlite`. |
| [`SqliteConnectionBar`](SqliteConnectionBar.cs) | class | One-row `ConnectionBarBase` editor for a database file, password, journal mode and synchronous mode that builds a `FailIfMissing` connection string and creates a `SqliteDAC`. |
| [`SqliteConnectionPanel`](SqliteConnectionPanel.cs) | class | Larger `ConnectionPanelBase` editor with the same fields as `SqliteConnectionBar`, for a settings page or dialog. |
<!-- domains:end -->

## Controls

Both controls are in the `Sphere10.Framework.Windows.Forms.Sqlite` namespace and edit the same four settings.

| Control | Base class | Use |
|---|---|---|
| `SqliteConnectionBar` | `ConnectionBarBase` | One-row editor for a toolbar or header area. Its options button can select an artificial-keys file. |
| `SqliteConnectionPanel` | `ConnectionPanelBase` | Larger editor for a settings page or a dialog. |

| Member | Behavior |
|---|---|
| `Filename` | Read-only path of the database file. Set it through `ConnectionString` or the file selector. |
| `Mode` | `PathSelectionMode` of the file selector. Both controls start in `File` mode, and `OpenFile` suits an existing database. |
| `Password`, `HasPassword` | Read-only password text, and whether it contains a non-whitespace character. |
| `DatabaseName` | File name without its extension, or `null` when no file is selected. |
| `ConnectionString` | Builds a string from the file, password, journal mode and sync mode, always with `FailIfMissing=True`, or fills the fields from an assigned string. |
| `GetDAC()` | Returns a new `SqliteDAC` as `IDAC` without opening the file. |
| `TestConnection()` | Opens and closes the database, and returns `Task<Result>`. |
| `ArtificialKeysFile` | Bar only. Path of an artificial-keys XML file. The options button sets it through `SelectArtificialKeysFile()` and can clear it again. `GetDAC()` loads the file into each new DAC, so that `GetSchema()` includes the keys it declares. |

The **Journal** list offers the `SqliteJournalMode` values and starts at `Default`, and the **Sync** list offers the `SqliteSyncMode` values and starts at `Normal`. `SqliteHelper.Convert` maps both to their `System.Data.SQLite` settings. `ConnectionString` is hidden from the Windows Forms designer.

## Edit and test a connection

Place the panel on a designer-authored dialog, form or screen like any other control, and host `SqliteConnectionBar` the same way where space is limited. Set the editor's `Mode` to `PathSelectionMode.OpenFile` in the designer when the user must choose an existing file. The code-behind below belongs to a dialog whose designer file declares `_connectionPanel` (a `SqliteConnectionPanel`), `_testButton` and `_statusLabel`, wires `_testButton.Click`, and sets the `DialogResult` of its OK and Cancel buttons.

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

The editors only collect settings. They do not save profiles or create databases, and they never hold the database open:

- `TestConnection()` builds the connection string on the calling thread, then opens and disposes a DAC scope on a thread-pool task. A missing file or another open failure becomes an error in the returned `Result`, and nothing stays open afterwards. A successful test shows that the file opened, not that the application's tables are in place. The method takes no cancellation token.
- Building the string can throw before the test starts, for example when a bar's `ArtificialKeysFile` cannot be loaded.
- When the application needs the database, call `GetDAC()` on the editor or construct `new SqliteDAC(connectionString)`, then call `EnterScope()` for each unit of work and dispose the scope. The DAC itself is not disposable, and the scope owns the connection.
- `StateChanged`, inherited from `UserControlEx`, is a parameterless event that is raised when a field changes after the control has loaded. It reports edits, not connectivity.

For example, this unit of work deletes the stored sessions in one transaction on the accepted string:

```csharp
using Sphere10.Framework.Data;

var dac = new SqliteDAC(savedConnectionString);
using (var scope = dac.EnterScope()) {
	scope.BeginTransaction();
	dac.ExecuteNonQuery("DELETE FROM sessions");
	scope.Commit();
}
```

Disposing the scope does not commit, so call `Commit()` explicitly.

## Host an editor in code

The editors also work without the designer. The helper below adds a bar to an existing form or container, and builds a connection dialog around the panel with the standard `Form.ShowDialog(owner)` API. Call it from the application's UI thread. Both editors use `PathSelectionMode.OpenFile` for an existing database file. Pass the dialog a string from `SQLiteConnectionStringBuilder` or one saved from these editors.

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

The host owns the added bar. The dialog disposes its panel and buttons after it closes, and cancelling returns the original string unchanged. Pressing **OK** collects the settings without opening or creating a database.

The editors never create the file, so create it first when it may be missing. `AlreadyExistsPolicy.Skip` leaves an existing file as it is:

```csharp
using Sphere10.Framework;

Tools.Sqlite.Create(path, existsPolicy: AlreadyExistsPolicy.Skip);
var bar = SqliteConnectionUi.AddBar(host, path);
```

## Connection string behavior

- Every generated string contains `FailIfMissing=True`, so the editors never create a database, even in `SaveFile` mode. Create the file first, for example with `Tools.Sqlite.Create(path)` from the data provider.
- The editors select database files. For an in-memory database, call `Tools.Sqlite.Open(":memory:")` instead.
- The editors rebuild the string from their four settings, so other settings in an assigned string are dropped, such as `Read Only` or pooling. To keep such settings, read `ConnectionString`, add them with `SQLiteConnectionStringBuilder`, and construct `SqliteDAC` from the result. `GetDAC()` and `TestConnection()` always use the rebuilt string.
- The editors pass the password to `System.Data.SQLite` as `Password`. Whether that encrypts the file depends on the native SQLite library in use, so do not rely on the password box alone for encryption.
- Journal and sync modes change locking and durability, so choose them for the workload and the file system.
- The setter does not catch errors from `SQLiteConnectionStringBuilder`, so assigning a string that it rejects throws.
- Connection strings contain the password, so do not log or display them.

## Use the generic provider selector

`DatabaseConnectionPanel` and `DatabaseConnectionBar` in `Sphere10.Framework.Windows.Forms` let the user choose a database engine and then show that engine's editor. They resolve the editors as named framework services, so add this package's module to the application's startup code:

```csharp
Sphere10Framework.Instance
	.BuildWinFormsApplication()
	// ... the application's existing configuration and modules ...
	.AddModule<Sphere10.Framework.Windows.Forms.Sqlite.ModuleConfiguration>()
	.StartWinFormsApplication();
```

`ModuleConfiguration` registers `SqliteConnectionBar` and `SqliteConnectionPanel` as transient services named `nameof(DBMSType.Sqlite)`. Referencing the package does not register them, and a directly hosted SQLite editor needs no module.

- A selector constructed before the framework has started, for example in the Windows Forms designer, finds no service provider and lists no engines, so construct it after startup. Switching engines disposes the previous editor.
- The engine list contains only registered providers. Setting `SelectedDBMSType` to an unregistered engine leaves the current editor selected.
- Set `SelectedDBMSType = DBMSType.Sqlite` before assigning `ConnectionString`, because the selector passes the string to its current editor.
- `Database` returns the engine and the string together as a `DBReference`, and the bar also accepts one. `DACFactory.CreateDAC(reference.DBMSType, reference.ConnectionString)` recreates the DAC from a saved reference.
- `DatabaseConnectionPanel` raises `DBMSTypeChanged(panel, dbmsType)` when the selected engine changes. `DatabaseConnectionBar` has no such event.

This complete `Program.cs` starts only the modules that the selector needs, shows it with SQLite selected and ends the framework after the window closes:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;
using ProviderModule = Sphere10.Framework.Windows.Forms.Sqlite.ModuleConfiguration;
using WinFormsModule = Sphere10.Framework.Windows.Forms.ModuleConfiguration;
using Application = System.Windows.Forms.Application;

namespace ReadmeExamples.SqliteSelector;

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
			SelectedDBMSType = DBMSType.Sqlite
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

- **Controls > Input > All controls by class name** opens the control catalog. Search for `SqliteConnection` to edit sample settings, which never open a database on their own.
- **Integration > Connections > Database Connection Panel** and **Database Connection Bar** show the generic selectors with every provider module registered. Choose **Sqlite**, select an existing database file and click **Test connection**.

## Build from source

From the repository root on Windows:

```powershell
dotnet build src/Sphere10.Framework.Windows.Forms.Sqlite/Sphere10.Framework.Windows.Forms.Sqlite.csproj -c Release -p:BuildRevision=0
```

`-p:BuildRevision=0` keeps a development build from taking the next local build number. The [root README](../../README.md#building-and-publishing-packages) explains build numbering, and how `scripts/pack.ps1` packs the whole package set. The project packs this file as its NuGet readme, and [`Directory.Packages.props`](../../Directory.Packages.props) sets the driver versions.

- [`SqliteConnectionBar.cs`](SqliteConnectionBar.cs) and [`SqliteConnectionPanel.cs`](SqliteConnectionPanel.cs) map the settings to `SQLiteConnectionStringBuilder`.
- [`ModuleConfiguration.cs`](ModuleConfiguration.cs) registers the named services.
- [`ConnectionBarBase`](../Sphere10.Framework.Windows.Forms/Database/Controls/ConnectionBarBase.cs) and [`ConnectionPanelBase`](../Sphere10.Framework.Windows.Forms/Database/Controls/ConnectionPanelBase.cs) implement [`IDatabaseConnectionProvider`](../Sphere10.Framework.Windows.Forms/Database/IDatabaseConnectionProvider.cs), including the shared `GetDAC()` logic.

## Related documentation

- [Windows Forms library](../Sphere10.Framework.Windows.Forms/README.md) for the generic selectors and the base classes.
- [SQLite data provider](../Sphere10.Framework.Data.Sqlite/README.md) for `SqliteDAC`, `Tools.Sqlite` and database creation.
- [Data access](../Sphere10.Framework.Data/README.md) for DAC scopes, transactions and [artificial keys](../Sphere10.Framework.Data/README.md#artificial-keys).
- [Application startup and modules](../Sphere10.Framework.Application/README.md).
- [Database connection editors](../../docs/guidelines/patterns/winforms-controls.md#database-connection-editors), which compares the editors of every provider.
- The other provider editors for [SQL Server](../Sphere10.Framework.Windows.Forms.MSSQL/README.md), [Firebird](../Sphere10.Framework.Windows.Forms.Firebird/README.md), [MySQL](../Sphere10.Framework.Windows.Forms.MySQL/README.md), [PostgreSQL](../Sphere10.Framework.Windows.Forms.PostgreSQL/README.md) and [Oracle](../Sphere10.Framework.Windows.Forms.Oracle/README.md).

## License and author

**License**: [MIT License](../../LICENSE)

**Author**: Herman Schoenfeld (sphere10.com)

**Copyright**: © 2018-Present Herman Schoenfeld. All rights reserved.
