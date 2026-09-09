# Sphere10.Framework.Windows.Forms.PostgreSQL

<!-- Copyright (c) 2018-Present Herman Schoenfeld. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

**PostgreSQL connection editors for Windows Forms**, backed by `Npgsql` and the framework's `PostgreSQLDAC`. The package provides a compact connection bar and a larger panel for collecting host, database, account, password, and port settings.

The controls implement `IDatabaseConnectionProvider` through the shared WinForms base classes. They can be hosted directly or registered for the framework's provider-selecting `DatabaseConnectionBar` and `DatabaseConnectionPanel`. The package contains working editors and registration code; database creation, querying, and schema management belong to the data-provider package.

## Installation and requirements

```bash
dotnet add package Sphere10.Framework.Windows.Forms.PostgreSQL
```

The package targets **`net10.0-windows`** and uses Windows Forms. A consuming desktop project needs the Windows .NET 10 desktop runtime and these project settings:

```xml
<PropertyGroup>
  <OutputType>WinExe</OutputType>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWindowsForms>true</UseWindowsForms>
</PropertyGroup>
```

Build with the .NET 10 SDK. Create and use controls on an STA UI thread. A reachable PostgreSQL server and suitable credentials are required only when testing or opening a database connection; constructing and editing a control does not open one.

## Package architecture

| Component | Responsibility |
|---|---|
| [`PostgreSQLConnectionBar`](PostgreSQLConnectionBar.cs) | Compact editor derived from `ConnectionBarBase`; includes its inherited artificial-key options |
| [`PostgreSQLConnectionPanel`](PostgreSQLConnectionPanel.cs) | Larger table-layout editor derived from `ConnectionPanelBase` |
| [`ModuleConfiguration`](ModuleConfiguration.cs) | Registers both controls as named transient services under `nameof(DBMSType.PostgreSQL)` |
| `Sphere10.Framework.Windows.Forms` | Base controls, `IDatabaseConnectionProvider`, provider selectors, and WinForms lifecycle integration |
| `Sphere10.Framework.Data.PostgreSQL` | `PostgreSQLDAC`, SQL-building/database support, and the `Npgsql` driver dependency |

The UI classes are in `Sphere10.Framework.Windows.Forms.PostgreSQL`. The returned `PostgreSQLDAC` is in `Sphere10.Framework.Data`. Install the data-provider package by itself for applications that do not need Windows Forms.

## Control API

Both controls expose the same connection fields:

| Member | Meaning |
|---|---|
| `Server` | PostgreSQL host name; maps to `NpgsqlConnectionStringBuilder.Host` |
| `Database` / `DatabaseName` | Database field; `DatabaseName` returns its current text |
| `Username`, `Password` | Map to the provider's `Username` and `Password` |
| `Port` | Text field; blank omits an explicit port, using the provider default of 5432 |
| `ConnectionString` | Builds or loads the five supported fields using `NpgsqlConnectionStringBuilder` |
| `GetDAC()` | Returns a new `PostgreSQLDAC` through `IDAC`, using the control's current connection string |
| `TestConnection()` | Returns `Task<Result>`; attempts to open and dispose a connection scope without executing an application query |

`ConnectionString` is hidden from designer serialization. Set credentials at runtime rather than placing them in designer-generated code. The password textbox masks its display, but `Password` and `ConnectionString` still expose the text to application code.

Assigning `ConnectionString` populates the displayed fields. A provider-default port (`5432`) is shown as blank; a nondefault port is shown explicitly.

## Complete example: host and test a connection panel

Use this as `Program.cs` in a Windows Forms application with the package installed. Enter valid credentials and click **Test connection** to contact the server. There is no automatic connection attempt on startup.

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms.PostgreSQL;
using WinFormsApplication = System.Windows.Forms.Application;

namespace ReadmeExamples.PostgreSQLDirect;

internal static class Program {
	[STAThread]
	private static void Main() {
		WinFormsApplication.EnableVisualStyles();
		WinFormsApplication.SetCompatibleTextRenderingDefault(false);
		using var window = new Form { Text = "PostgreSQL connection", ClientSize = new Size(640, 260) };
		var editor = new PostgreSQLConnectionPanel {
			Dock = DockStyle.Fill,
			Server = "localhost",
			Database = "app_data",
			Username = "app_user",
			Password = string.Empty,
			Port = string.Empty
		};
		var status = new Label { Dock = DockStyle.Bottom, Height = 44, Text = "Enter your connection details." };
		var testButton = new Button { Dock = DockStyle.Bottom, Height = 32, Text = "Test connection" };
		testButton.Click += async (sender, args) => {
			testButton.Enabled = false;
			using var restoreButton = Tools.Scope.ExecuteOnDispose(() => {
				if (!testButton.IsDisposed)
					testButton.Enabled = true;
			});
			try {
				var result = await editor.TestConnection();
				if (!window.IsDisposed)
					status.Text = result.IsSuccess ? "Connection succeeded." : result.ToString();
			} catch (Exception error) {
				if (!window.IsDisposed)
					status.Text = error.Message;
			}
		};
		window.Controls.Add(editor);
		window.Controls.Add(status);
		window.Controls.Add(testButton);
		WinFormsApplication.Run(window);
	}
}
```

The form owns and disposes the child controls. `TestConnection()` reads their values on the UI thread and performs its connection-opening work through the base implementation's background task. It reports connection-opening exceptions in `Result`; invalid connection-string construction can throw before that internal catch, which is why the example also catches exceptions at the UI boundary. The API has no cancellation-token parameter.

To use a bar instead, host `PostgreSQLConnectionBar` in your form and call the same `ConnectionString`, `GetDAC()`, and `TestConnection()` members. The bar additionally inherits `ArtificialKeysFile` and `SelectArtificialKeysFile()`; a configured artificial-key file is loaded when `GetDAC()` is called. The panel has no corresponding artificial-key file property.

## Complete example: register the provider selector

`DatabaseConnectionPanel` and `DatabaseConnectionBar` resolve providers through `Sphere10Framework.Instance.ServiceProvider`. Register this package's module and start the framework **before** constructing a selector. Installing the package alone does not perform these registrations.

This separate `Program.cs` starts only the modules needed for the example, displays the registered provider, and ends the framework after the form closes:

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;
using Sphere10.Framework.Windows.Forms;
using ProviderModule = Sphere10.Framework.Windows.Forms.PostgreSQL.ModuleConfiguration;
using WinFormsModule = Sphere10.Framework.Windows.Forms.ModuleConfiguration;
using WinFormsApplication = System.Windows.Forms.Application;

namespace ReadmeExamples.PostgreSQLSelector;

internal static class Program {
	[STAThread]
	private static void Main() {
		WinFormsApplication.EnableVisualStyles();
		WinFormsApplication.SetCompatibleTextRenderingDefault(false);
		var framework = Sphere10Framework.Instance;
		framework.Build()
			.UseModule<WinFormsModule>()
			.UseModule<ProviderModule>()
			.Start();
		using var shutdown = Tools.Scope.ExecuteOnDispose(framework.EndFramework);
		using var window = new Form { Text = "Database connection", ClientSize = new Size(660, 220) };
		var selector = new DatabaseConnectionPanel {
			Dock = DockStyle.Fill,
			SelectedDBMSType = DBMSType.PostgreSQL
		};
		window.Controls.Add(selector);
		WinFormsApplication.Run(window);
	}
}
```

In an existing framework application, add `.UseModule<ProviderModule>()` to its startup builder instead of starting a second framework. Register other provider UI modules to add choices to the selector. The module supplies both the bar and the panel as transient controls; use separate instances for separate hosts and keep them on the UI thread.

## Connection and lifecycle limits

- The editor reconstructs connection strings from its displayed fields. Options such as pooling, timeouts, TLS settings, and provider-specific advanced keys are not retained through a `ConnectionString` setter/getter round trip. Apply required advanced settings with the provider's connection-string builder **after** reading the editor, and construct a `PostgreSQLDAC` from that final string. The editor's own `GetDAC()` and `TestConnection()` use only its reconstructed string.
- A bar parses nonempty invalid port text with `Tools.Parser.Parse<int?>` and may throw. A panel uses `SafeParse<int?>`, so unparseable text becomes an omitted/default port. These are basic editors, not comprehensive connection validators; validate an application's required fields and port range before use.
- Reading `DatabaseName` describes the current UI value; it does not query the server. A successful connection test confirms that opening a connection worked, not that application tables, permissions, or migrations are correct.
- The controls do not save connection profiles, create databases, discover servers, or supply a query editor. They expose the current settings and a data-access object for the application to use. Obtain settings on the UI thread before starting database work, and dispose every connection/transaction scope you create.
- The underlying `PostgreSQLDAC.BulkInsert` currently throws `NotImplementedException`. See the data-provider documentation for supported SQL, schema, and database-management operations; adding these UI controls does not add missing provider functionality.

## Build from source

From the repository root:

```bash
dotnet build src/Sphere10.Framework.Windows.Forms.PostgreSQL/Sphere10.Framework.Windows.Forms.PostgreSQL.csproj -c Release
dotnet pack src/Sphere10.Framework.Windows.Forms.PostgreSQL/Sphere10.Framework.Windows.Forms.PostgreSQL.csproj -c Release --no-build
```

The project references the shared WinForms library and matching data provider. Driver versions are managed centrally in [`Directory.Packages.props`](../../Directory.Packages.props). The project marks this README as the NuGet package README. Building or packing does not contact a database or run the UI.

## Related documentation

- [Windows Forms framework](../Sphere10.Framework.Windows.Forms/README.md)
- [PostgreSQL data provider](../Sphere10.Framework.Data.PostgreSQL/README.md)
- [Data-access abstractions](../Sphere10.Framework.Data/README.md)
- [Application startup and modules](../Sphere10.Framework.Application/README.md)

## License and author

Distributed under the [MIT License](../../LICENSE).

**Author:** Herman Schoenfeld (sphere10.com). Copyright © 2018-Present Herman Schoenfeld. All rights reserved.
