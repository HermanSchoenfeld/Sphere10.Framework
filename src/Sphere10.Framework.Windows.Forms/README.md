<!-- Copyright (c) 2018-Present Herman Schoenfeld & Sphere 10 Software. All rights reserved. Author: Herman Schoenfeld (sphere10.com) -->

# 🖼️ Sphere10.Framework.Windows.Forms

**Windows Forms UI framework and component library** providing data binding controls, database connection panels, validation components, and presentation utilities for desktop applications.

Sphere10.Framework.Windows.Forms enables **rapid Windows desktop application development** with pre-built UI components for database connectivity, data display, user input validation, and common desktop patterns.

## 📦 Installation

```bash
dotnet add package Sphere10.Framework.Windows.Forms
```

## ⚡ 10-Second Example

```csharp
using Sphere10.Framework.Windows.Forms;

// Create a database connection panel that adapts to selected DBMS
var connectionPanel = new DatabaseConnectionPanel();
connectionPanel.SelectedDBMSType = DBMSType.SQLServer;

// Get connection string and DAC from the panel
string connectionString = connectionPanel.ConnectionString;
IDAC dac = connectionPanel.GetDAC();
```

## 🏗️ Core Concepts

**Database Connection Controls**: `DatabaseConnectionPanel` dynamically loads appropriate UI based on selected database type (SQLite, SQL Server, Firebird).

**Custom Controls**: Enhanced controls like `PathSelectorControl`, `ProgressBarEx`, `PropertyGridEx`, `RadioGroupBox`, and validation indicators.

**Data Binding**: Enhanced data binding with validation and change notification.

**Wizard Framework**: Multi-step wizard UI pattern implementation.

**Source Grid**: Advanced data grid component for tabular data display.

## 🔧 Core Components

### DatabaseConnectionPanel

Dynamic database connection UI that loads appropriate controls based on selected provider:

```csharp
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Data;

public class MainForm : Form {
    private DatabaseConnectionPanel _connectionPanel;
    
    public MainForm() {
        InitializeComponent();
        
        _connectionPanel = new DatabaseConnectionPanel();
        
        // Optionally hide certain database types
        _connectionPanel.IgnoreDBMS = new[] { DBMSType.FirebirdFile };
        
        // Handle DBMS type changes
        _connectionPanel.DBMSTypeChanged += OnDBMSChanged;
        
        Controls.Add(_connectionPanel);
    }
    
    private void OnDBMSChanged(DatabaseConnectionPanel sender, DBMSType newType) {
        // Panel automatically loads correct connection UI:
        // - SQLServer: MSSQLConnectionPanel
        // - Sqlite: SqliteConnectionPanel  
        // - Firebird: FirebirdConnectionPanel
        // - FirebirdFile: FirebirdEmbeddedConnectionPanel
    }
    
    private async void TestConnection() {
        var result = await _connectionPanel.TestConnection();
        if (result.IsSuccess) {
            MessageBox.Show("Connection successful!");
            
            // Get the DAC for database operations
            using var dac = _connectionPanel.GetDAC();
            // Use dac...
        } else {
            MessageBox.Show($"Connection failed: {result.ErrorMessages.First()}");
        }
    }
}
```

### PathSelectorControl

File/folder path selection with browse button:

```csharp
using Sphere10.Framework.Windows.Forms;

var pathSelector = new PathSelectorControl();
pathSelector.Mode = PathSelectionMode.File;  // or Folder
pathSelector.Path = @"C:\Data\file.txt";

// User can type path or browse
string selectedPath = pathSelector.Path;
```

### ProgressBarEx

Enhanced progress bar with text display:

```csharp
using Sphere10.Framework.Windows.Forms;

var progressBar = new ProgressBarEx();
progressBar.DisplayStyle = ProgressBarDisplayText.Percentage;
progressBar.Value = 75;  // Shows "75%"
```

### ValidationIndicator

Visual validation state indicator:

```csharp
using Sphere10.Framework.Windows.Forms;

var validator = new ValidationIndicator();
validator.State = ValidationState.Valid;    // Green checkmark
validator.State = ValidationState.Invalid;  // Red X
validator.State = ValidationState.Pending;  // Yellow
```

### RadioGroupBox

Group box with built-in radio button management:

```csharp
using Sphere10.Framework.Windows.Forms;

var radioGroup = new RadioGroupBox();
// Radio buttons inside are mutually exclusive
```

### ServiceStatusControl

Display Windows service status:

```csharp
using Sphere10.Framework.Windows.Forms;

var serviceStatus = new ServiceStatusControl();
serviceStatus.ServiceName = "MyService";
// Displays: Running, Stopped, Starting, etc.
```

## 🛠️ Tools.WinForms Namespace

```csharp
using Tools;

// Create custom cursor from bitmap
Cursor cursor = WinForms.CreateCursor(bitmap, hotspotX, hotspotY);

// Load cursor from raw bytes
Cursor rawCursor = WinForms.LoadRawCursor(cursorBytes);
```

## 📋 Available Components

### Controls
| Control | Description |
|---------|-------------|
| `CheckedGroupBox` | Group box with checkbox header |
| `PathSelectorControl` | File/folder path selection |
| `ProgressBarEx` | Progress bar with text display |
| `PropertyGridEx` | Enhanced property grid |
| `RadioGroupBox` | Radio button group container |
| `ServiceStatusControl` | Windows service status display |
| `ValidationIndicator` | Visual validation state |
| `ExpandingCircle` | Animated expanding circle |
| `PictureBoxEx` | Enhanced picture box |
| `UserControlEx` | Enhanced user control base |

### Database Components
| Component | Description |
|-----------|-------------|
| `DatabaseConnectionPanel` | Dynamic DBMS connection UI |
| `DatabaseConnectionBar` | Compact connection bar |
| `ConnectionPanelBase` | Base class for connection panels |
| `IDatabaseConnectionProvider` | Interface for connection providers |

### Other
| Component | Description |
|-----------|-------------|
| `Wizard` | Multi-step wizard framework |
| `SourceGrid` | Advanced data grid |
| `LoadingCircle` | Loading animation |
| `ExplorerBar` | Explorer-style navigation bar |
| `AppointmentBook` | Appointment/calendar UI |
| `ApplicationBlock` | Modular application sections with menus |

## 🏗️ Application Blocks

Application blocks provide a modular way to organize application features with menu structures. The builder pattern provides a fluent API for construction:

```csharp
using Sphere10.Framework.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

// Register an application block using the builder pattern
serviceCollection.AddApplicationBlock(
    new ApplicationBlockBuilder()
        .WithName("My Block")
        .WithDefaultScreen<MainScreen>()
        .AddMenu(mb => mb
            .WithText("File")
            .AddActionItem("New", () => CreateNew())
            .AddActionItem("Open", () => OpenFile())
            .AddScreenItem<SettingsScreen>("Settings")
        )
        .AddMenu(mb => mb
            .WithText("Tools")
            .AddScreenItem<ToolA>("Tool A")
            .AddScreenItem<ToolB>("Tool B")
        )
        .Build()
);

// Or create a reusable builder method
public class MyApplicationBlock {
    public static ApplicationBlock Build() {
        return new ApplicationBlockBuilder()
            .WithName("My Application")
            .WithDefaultScreen<DashboardScreen>()
            .AddMenu(mb => mb
                .WithText("Main Menu")
                .AddScreenItem<Screen1>("Option 1")
                .AddScreenItem<Screen2>("Option 2")
                .AddActionItem("Custom Action", async () => {
                    // Custom action logic
                    await DoSomethingAsync();
                })
            )
            .Build();
    }
}

// Register in ModuleConfiguration
serviceCollection.AddApplicationBlock(MyApplicationBlock.Build());
```

The builder pattern supports:
- **Screen items**: Navigate to specific forms/screens
- **Action items**: Execute custom code when clicked
- **Menu organization**: Group related features logically
- **Fluent API**: Chain methods for clean, readable code
- **Type-safe**: Compile-time checking with generics

## 📦 Dependencies

- **Sphere10.Framework**: Core framework
- **Sphere10.Framework.Data**: Database abstraction
- **System.Windows.Forms**: Windows Forms (.NET built-in)
- **System.Drawing**: Graphics support

## 📖 Related Projects

- [Sphere10.Framework.Windows.Forms.Sqlite](../Sphere10.Framework.Windows.Forms.Sqlite) - SQLite connection panel
- [Sphere10.Framework.Windows.Forms.MSSQL](../Sphere10.Framework.Windows.Forms.MSSQL) - SQL Server connection panel  
- [Sphere10.Framework.Windows.Forms.Firebird](../Sphere10.Framework.Windows.Forms.Firebird) - Firebird connection panel
- [Sphere10.Framework.Windows](../Sphere10.Framework.Windows) - Windows platform integration
- [Sphere10.Framework.Data](../Sphere10.Framework.Data) - Database abstraction layer

## ✅ Status & Compatibility

- **Maturity**: Production-tested for Windows desktop applications
- **.NET Target**: .NET 8.0+ (Windows), .NET Framework 4.7+ (legacy)
- **Platform**: Windows only (Windows Forms)

## ⚖️ License

Distributed under the **MIT NON-AI License**.

See the LICENSE file for full details. More information: [Sphere10 NON-AI-MIT License](https://sphere10.com/legal/NON-AI-MIT)

## 👤 Author

**Herman Schoenfeld** - Software Engineer
